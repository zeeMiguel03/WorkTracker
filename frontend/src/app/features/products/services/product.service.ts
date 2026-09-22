import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, catchError, of, switchMap, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ProductCondition, ProductDetailsApi, ProductPage, ProductStatus } from '../models/product.model';

export interface CreateProductRequest {
  readonly name: string;
  readonly description: string;
  readonly notes: string;
  readonly category: string;
  readonly brand: string;
  readonly size: string;
  readonly color: string;
  readonly condition: ProductCondition;
  readonly status: ProductStatus;
  readonly purchasePrice: number;
  readonly listingPrice: number | null;
  readonly minimumPrice: number | null;
  readonly purchaseSourceId: number | null;
  readonly purchaseAccountId: number | null;
  readonly purchaseTransactionTypeId: number | null;
  readonly purchaseDate: string;
  readonly purchaseTrackingNumber: string;
  readonly purchaseShippingCost: number;
  readonly purchaseOtherCosts: number;
  readonly purchaseOrderNotes: string;
  readonly images: File[];
}

export interface UpdateProductRequest {
  readonly name: string;
  readonly description: string;
  readonly category: string;
  readonly brand: string;
  readonly size: string;
  readonly color: string;
  readonly condition: ProductCondition;
  readonly status: ProductStatus;
  readonly purchasePrice: number;
  readonly listingPrice: number | null;
  readonly minimumPrice: number | null;
  readonly notes: string;
}

export interface SellProductRequest {
  readonly productName: string;
  readonly sourceId: number;
  readonly accountId: number;
  readonly transactionTypeId: number;
  readonly salePrice: number;
  readonly saleOtherCosts: number | null;
  readonly soldAt: string;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/products`;

  list(page = 1, pageSize = 20, status: ProductStatus | null = null, search = ''): Observable<ProductPage> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (status) {
      params = params.set('status', this.statusToApi(status));
    }

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<ProductPage>(this.endpoint, {
      params,
      withCredentials: true,
    });
  }

  getById(id: number): Observable<ProductDetailsApi> {
    return this.http.get<ProductDetailsApi>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }

  create(data: CreateProductRequest): Observable<unknown> {
    if (data.purchaseSourceId && data.purchaseAccountId && data.purchaseTransactionTypeId && data.purchaseDate) {
      return this.createPurchaseEntry(data).pipe(
        switchMap((entry) => this.createPurchaseOrder(data, entry.id).pipe(
          switchMap((purchaseOrder) => this.createProduct(data, purchaseOrder.id).pipe(
            catchError((error) => this.removePurchaseOrder(purchaseOrder.id).pipe(
              catchError(() => of(void 0)),
              switchMap(() => throwError(() => error)),
            )),
          )),
          catchError((error) => this.removeEntry(entry.id).pipe(
            catchError(() => of(void 0)),
            switchMap(() => throwError(() => error)),
          )),
        )),
      );
    }

    return this.createProduct(data);
  }

  private createProduct(data: CreateProductRequest, purchaseOrderId: number | null = null): Observable<unknown> {
    const formData = new FormData();
    if (purchaseOrderId) {
      formData.append('PurchaseOrderId', String(purchaseOrderId));
    }
    formData.append('Name', data.name.trim());
    formData.append('Description', data.description.trim());
    formData.append('Category', data.category.trim());
    formData.append('Brand', data.brand.trim());
    formData.append('Size', data.size.trim());
    formData.append('Color', data.color.trim());
    formData.append('Condition', String(this.conditionToApi(data.condition)));
    formData.append('Status', String(this.statusToApi(data.status)));
    formData.append('PurchasePrice', String(data.purchasePrice || 0));
    formData.append('AllocatedShippingCost', String(data.purchaseShippingCost || 0));
    formData.append('AllocatedOtherCosts', String(data.purchaseOtherCosts || 0));
    formData.append('Notes', data.notes.trim());

    if (data.listingPrice !== null) {
      formData.append('ListingPrice', String(data.listingPrice));
    }

    if (data.minimumPrice !== null) {
      formData.append('MinimumPrice', String(data.minimumPrice));
    }

    data.images.forEach((file) => formData.append('Images', file, file.name));
    formData.append('CoverImageIndex', '0');

    return this.http.post(this.endpoint, formData, { withCredentials: true });
  }

  update(id: number, data: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${id}`, {
      Name: data.name.trim(),
      Description: data.description.trim() || null,
      Category: data.category.trim() || null,
      Brand: data.brand.trim() || null,
      Size: data.size.trim() || null,
      Color: data.color.trim() || null,
      Condition: this.conditionToApi(data.condition),
      Status: this.statusToApi(data.status),
      PurchasePrice: data.purchasePrice || 0,
      AllocatedShippingCost: 0,
      AllocatedOtherCosts: 0,
      ListingPrice: data.listingPrice,
      MinimumPrice: data.minimumPrice,
      Notes: data.notes.trim() || null,
    }, { withCredentials: true });
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }

  changeStatus(id: number, status: ProductStatus): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${id}/status/${this.statusToApi(status)}`, null, {
      withCredentials: true,
    });
  }

  sell(id: number, data: SellProductRequest): Observable<void> {
    return this.createSaleEntry(data).pipe(
      switchMap((entry) => this.http.post<void>(`${this.endpoint}/${id}/sell`, {
        SaleEntryId: entry.id,
        SaleSourceId: data.sourceId,
        SalePrice: data.salePrice,
        SaleOtherCosts: data.saleOtherCosts,
        SoldAt: data.soldAt,
      }, { withCredentials: true }).pipe(
        catchError((error) => this.removeEntry(entry.id).pipe(
          catchError(() => of(void 0)),
          switchMap(() => throwError(() => error)),
        )),
      )),
    );
  }

  private createPurchaseEntry(data: CreateProductRequest): Observable<EntryApi> {
    return this.http.post<EntryApi>(`${environment.apiUrl}/entries`, {
      SourceId: data.purchaseSourceId,
      TransactionTypeId: data.purchaseTransactionTypeId,
      AccountId: data.purchaseAccountId,
      Name: `Compra: ${data.name.trim()}`,
      Description: data.description.trim() || null,
      Quantity: 1,
      Value: data.purchasePrice || 0,
      Date: new Date(`${data.purchaseDate}T12:00:00`).toISOString(),
    }, { withCredentials: true });
  }

  private createPurchaseOrder(data: CreateProductRequest, entryId: number): Observable<PurchaseOrderApi> {
    return this.http.post<PurchaseOrderApi>(`${environment.apiUrl}/purchase-orders`, {
      EntryId: entryId,
      SourceId: data.purchaseSourceId,
      TrackingNumber: data.purchaseTrackingNumber.trim() || null,
      ShippingCost: data.purchaseShippingCost || 0,
      OtherCosts: data.purchaseOtherCosts || 0,
      Notes: data.purchaseOrderNotes.trim() || null,
    }, { withCredentials: true });
  }

  private createSaleEntry(data: SellProductRequest): Observable<EntryApi> {
    return this.http.post<EntryApi>(`${environment.apiUrl}/entries`, {
      SourceId: data.sourceId,
      TransactionTypeId: data.transactionTypeId,
      AccountId: data.accountId,
      Name: `Venda: ${data.productName.trim()}`,
      Description: `Venda do produto ${data.productName.trim()}`,
      Quantity: 1,
      Value: data.salePrice,
      Date: data.soldAt,
    }, { withCredentials: true });
  }

  private removeEntry(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/entries/${id}`, { withCredentials: true });
  }

  private removePurchaseOrder(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/purchase-orders/${id}`, { withCredentials: true });
  }

  getImage(productId: number, imageId: number): Observable<Blob> {
    return this.http.get(`${this.endpoint}/${productId}/images/${imageId}`, {
      withCredentials: true,
      responseType: 'blob',
    });
  }

  private statusToApi(status: ProductStatus): string {
    return String({ draft: 1, purchased: 2, active: 3, sold: 4, archived: 5 }[status]);
  }

  private conditionToApi(condition: ProductCondition): number {
    return { 'new-with-tags': 1, 'new-without-tags': 2, 'very-good': 3, good: 4, satisfactory: 5 }[condition];
  }
}

interface EntryApi {
  readonly id: number;
}

interface PurchaseOrderApi {
  readonly id: number;
}
