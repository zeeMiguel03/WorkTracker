import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ProductCondition, ProductPage, ProductStatus } from '../models/product.model';

export interface CreateProductRequest {
  readonly name: string;
  readonly description: string;
  readonly category: string;
  readonly brand: string;
  readonly size: string;
  readonly color: string;
  readonly condition: ProductCondition;
  readonly purchasePrice: number;
  readonly listingPrice: number | null;
  readonly minimumPrice: number | null;
  readonly images: File[];
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

  create(data: CreateProductRequest): Observable<unknown> {
    const formData = new FormData();
    formData.append('Name', data.name.trim());
    formData.append('Description', data.description.trim());
    formData.append('Category', data.category.trim());
    formData.append('Brand', data.brand.trim());
    formData.append('Size', data.size.trim());
    formData.append('Color', data.color.trim());
    formData.append('Condition', String(this.conditionToApi(data.condition)));
    formData.append('PurchasePrice', String(data.purchasePrice || 0));
    formData.append('AllocatedShippingCost', '0');
    formData.append('AllocatedOtherCosts', '0');

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
