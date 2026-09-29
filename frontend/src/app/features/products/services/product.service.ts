import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

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
  readonly allocatedShippingCost: number;
  readonly allocatedOtherCosts: number;
  readonly listingPrice: number | null;
  readonly minimumPrice: number | null;
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
  readonly allocatedShippingCost: number;
  readonly allocatedOtherCosts: number;
  readonly listingPrice: number | null;
  readonly minimumPrice: number | null;
  readonly notes: string;
}

export interface SellProductRequest {
  readonly sourceId: number | null;
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
    return this.createProduct(data);
  }

  private createProduct(data: CreateProductRequest): Observable<unknown> {
    const formData = new FormData();
    formData.append('Name', data.name.trim());
    formData.append('Description', data.description.trim());
    formData.append('Category', data.category.trim());
    formData.append('Brand', data.brand.trim());
    formData.append('Size', data.size.trim());
    formData.append('Color', data.color.trim());
    formData.append('Condition', String(this.conditionToApi(data.condition)));
    formData.append('Status', String(this.statusToApi(data.status)));
    formData.append('PurchasePrice', String(data.purchasePrice || 0));
    formData.append('AllocatedShippingCost', String(data.allocatedShippingCost || 0));
    formData.append('AllocatedOtherCosts', String(data.allocatedOtherCosts || 0));
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
      Status: Number(this.statusToApi(data.status)),
      PurchasePrice: data.purchasePrice || 0,
      AllocatedShippingCost: data.allocatedShippingCost || 0,
      AllocatedOtherCosts: data.allocatedOtherCosts || 0,
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
    return this.http.post<void>(`${this.endpoint}/${id}/sell`, {
      SaleSourceId: data.sourceId,
      SalePrice: data.salePrice,
      SaleOtherCosts: data.saleOtherCosts,
      SoldAt: data.soldAt,
    }, { withCredentials: true });
  }

  getImage(productId: number, imageId: number): Observable<Blob> {
    return this.http.get(`${this.endpoint}/${productId}/images/${imageId}`, {
      withCredentials: true,
      responseType: 'blob',
    });
  }

  addImage(productId: number, image: File, displayOrder: number, isCover: boolean): Observable<void> {
    const formData = new FormData();
    formData.append('Image', image, image.name);
    formData.append('DisplayOrder', String(displayOrder));
    formData.append('IsCover', String(isCover));

    return this.http.post<void>(`${this.endpoint}/${productId}/images`, formData, { withCredentials: true });
  }

  replaceImage(productId: number, imageId: number, image: File, displayOrder: number, isCover: boolean): Observable<void> {
    const formData = new FormData();
    formData.append('Image', image, image.name);
    formData.append('DisplayOrder', String(displayOrder));
    formData.append('IsCover', String(isCover));

    return this.http.put<void>(`${this.endpoint}/${productId}/images/${imageId}`, formData, { withCredentials: true });
  }

  removeImage(productId: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${productId}/images/${imageId}`, { withCredentials: true });
  }

  setCoverImage(productId: number, imageId: number): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${productId}/images/${imageId}/cover`, null, { withCredentials: true });
  }

  reorderImages(productId: number, imageIds: readonly number[]): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${productId}/images/order`, {
      ImageIds: imageIds,
    }, { withCredentials: true });
  }

  private statusToApi(status: ProductStatus): string {
    return String({ draft: 1, purchased: 2, active: 3, sold: 4, archived: 5 }[status]);
  }

  private conditionToApi(condition: ProductCondition): number {
    return { 'new-with-tags': 1, 'new-without-tags': 2, 'very-good': 3, good: 4, satisfactory: 5 }[condition];
  }
}
