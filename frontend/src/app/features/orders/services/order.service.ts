import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, concatMap, forkJoin, from, map, Observable, of, reduce, switchMap, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ProductDetailsApi, ProductListItemApi, ProductPage } from '../../products/models/product.model';
import { SourceService } from '../../sources/services/source.service';
import {
  ORDER_STATUS_API,
  OrderDetailsApi,
  OrderDraft,
  OrderListApi,
  OrderPage,
  OrderPageApi,
  OrderProductDraft,
  OrderProductView,
  OrderSourceOption,
  OrderStatus,
  OrderView,
  orderStatusFromApi,
} from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly sourceService = inject(SourceService);
  private readonly endpoint = `${environment.apiUrl}/purchase-orders`;
  private readonly productEndpoint = `${environment.apiUrl}/products`;

  list(page = 1, pageSize = 10, status: OrderStatus | null = null, search = ''): Observable<OrderPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', ORDER_STATUS_API[status]);
    if (search.trim()) params = params.set('search', search.trim());

    return this.http.get<OrderPageApi>(this.endpoint, { params, withCredentials: true }).pipe(
      switchMap((response) => forkJoin({
        details: response.items.length
          ? forkJoin(response.items.map((order) => this.getRawById(order.id)))
          : of([] as OrderDetailsApi[]),
        products: this.loadProductDetails(),
      }).pipe(map(({ details, products }) => this.toPage(response, details, products)))),
    );
  }

  listSources(): Observable<readonly OrderSourceOption[]> {
    return this.sourceService.listAll().pipe(
      map((sources) => sources.filter((source) => source.isActive).map((source) => ({ id: source.id, name: source.name }))),
    );
  }

  create(draft: OrderDraft): Observable<number> {
    return this.http.post<OrderDetailsApi>(this.endpoint, this.orderPayload(draft), { withCredentials: true }).pipe(
      switchMap((order) => this.syncProducts(order.id, [], draft.products, draft).pipe(
        switchMap(() => this.markAsOrdered(order.id, draft.orderedDate)),
        map(() => order.id),
        catchError((error) => this.cleanupCreatedOrder(order.id).pipe(
          switchMap(() => throwError(() => error)),
          catchError(() => throwError(() => error)),
        )),
      )),
    );
  }

  update(order: OrderView, draft: OrderDraft): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${order.id}`, this.orderPayload(draft), { withCredentials: true }).pipe(
      switchMap(() => this.syncProducts(order.id, order.products, draft.products, draft)),
    );
  }

  remove(order: OrderView): Observable<void> {
    const productIds = order.products.flatMap((product) => product.ids);
    return from(productIds).pipe(
      concatMap((id) => this.http.delete<void>(`${this.productEndpoint}/${id}`, { withCredentials: true })),
      reduce(() => void 0, void 0),
      switchMap(() => this.http.delete<void>(`${this.endpoint}/${order.id}`, { withCredentials: true })),
    );
  }

  changeStatus(id: number, status: OrderStatus): Observable<void> {
    return this.http.patch<void>(`${this.endpoint}/${id}/status`, { Status: ORDER_STATUS_API[status] }, { withCredentials: true });
  }

  private markAsOrdered(id: number, date: string): Observable<void> {
    return this.http.post<void>(`${this.endpoint}/${id}/ordered`, { Date: new Date(`${date}T12:00:00`).toISOString() }, { withCredentials: true });
  }

  private cleanupCreatedOrder(orderId: number): Observable<void> {
    return this.loadProductDetails().pipe(
      map((products) => products.filter((product) => product.purchaseOrderId === orderId).map((product) => product.id)),
      switchMap((productIds) => from(productIds).pipe(
        concatMap((id) => this.http.delete<void>(`${this.productEndpoint}/${id}`, { withCredentials: true }).pipe(catchError(() => of(void 0)))),
        reduce(() => void 0, void 0),
      )),
      switchMap(() => this.http.delete<void>(`${this.endpoint}/${orderId}`, { withCredentials: true })),
    );
  }

  private getRawById(id: number): Observable<OrderDetailsApi> {
    return this.http.get<OrderDetailsApi>(`${this.endpoint}/${id}`, { withCredentials: true });
  }

  private loadProductDetails(): Observable<readonly ProductDetailsApi[]> {
    return this.loadAllProductListItems().pipe(
      switchMap((products) => products.length
        ? forkJoin(products.map((product) => this.http.get<ProductDetailsApi>(`${this.productEndpoint}/${product.id}`, { withCredentials: true })))
        : of([] as ProductDetailsApi[])),
    );
  }

  private loadAllProductListItems(page = 1, collected: readonly ProductListItemApi[] = []): Observable<readonly ProductListItemApi[]> {
    return this.http.get<ProductPage>(this.productEndpoint, {
      params: new HttpParams().set('page', page).set('pageSize', 50),
      withCredentials: true,
    }).pipe(
      switchMap((response) => {
        const products = [...collected, ...response.items];
        return response.hasNext ? this.loadAllProductListItems(page + 1, products) : of(products);
      }),
    );
  }

  private syncProducts(
    orderId: number,
    existing: readonly OrderProductView[],
    requested: readonly OrderProductDraft[],
    order: OrderDraft,
  ): Observable<void> {
    const totalUnits = Math.max(1, requested.reduce((total, product) => total + product.quantity, 0));
    const shippingShare = order.shippingCost / totalUnits;
    const otherShare = order.otherCosts / totalUnits;
    const requestedIds = new Set(requested.flatMap((product) => product.ids));
    const removedIds = existing.flatMap((product) => product.ids).filter((id) => !requestedIds.has(id));
    const operations: Observable<unknown>[] = removedIds.map((id) => this.http.delete(`${this.productEndpoint}/${id}`, { withCredentials: true }));

    requested.forEach((product) => {
      const desiredIds = product.ids.slice(0, product.quantity);
      desiredIds.forEach((id, index) => operations.push(this.updateProduct(id, product, shippingShare, otherShare, product.coverImageIds[index] ?? null)));
      product.ids.slice(product.quantity).forEach((id) => operations.push(this.http.delete(`${this.productEndpoint}/${id}`, { withCredentials: true })));
      for (let index = desiredIds.length; index < product.quantity; index++) {
        operations.push(this.createProduct(orderId, product, shippingShare, otherShare));
      }
    });

    return operations.length ? forkJoin(operations).pipe(map(() => void 0)) : of(void 0);
  }

  private createProduct(orderId: number, product: OrderProductDraft, shipping: number, otherCosts: number): Observable<unknown> {
    const formData = new FormData();
    formData.append('PurchaseOrderId', String(orderId));
    this.appendProductFields(formData, product, shipping, otherCosts);
    formData.append('CoverImageIndex', '0');
    if (product.image) formData.append('Images', product.image, product.image.name);
    return this.http.post(this.productEndpoint, formData, { withCredentials: true });
  }

  private updateProduct(id: number, product: OrderProductDraft, shipping: number, otherCosts: number, coverImageId: number | null): Observable<void> {
    const update = this.http.put<void>(`${this.productEndpoint}/${id}`, {
      Name: product.name.trim(), Description: product.description.trim() || null, Category: product.category || null,
      Brand: product.brand || null, Size: product.size || null, Color: product.color || null,
      Condition: product.condition, Status: product.status, PurchasePrice: product.unitPrice,
      AllocatedShippingCost: shipping, AllocatedOtherCosts: otherCosts, ListingPrice: product.listingPrice,
      MinimumPrice: product.minimumPrice, Notes: product.notes || null,
    }, { withCredentials: true });
    return product.image ? update.pipe(switchMap(() => this.saveProductImage(id, coverImageId, product.image!))) : update;
  }

  private saveProductImage(productId: number, imageId: number | null, image: File): Observable<void> {
    const formData = new FormData();
    formData.append('Image', image, image.name);
    formData.append('DisplayOrder', '0');
    formData.append('IsCover', 'true');
    if (imageId) {
      return this.http.put<void>(`${this.productEndpoint}/${productId}/images/${imageId}`, formData, { withCredentials: true });
    }
    return this.http.post<void>(`${this.productEndpoint}/${productId}/images`, formData, { withCredentials: true });
  }

  private appendProductFields(formData: FormData, product: OrderProductDraft, shipping: number, otherCosts: number): void {
    formData.append('Name', product.name.trim());
    formData.append('Description', product.description.trim());
    formData.append('Category', product.category);
    formData.append('Brand', product.brand);
    formData.append('Size', product.size);
    formData.append('Color', product.color);
    formData.append('Condition', String(product.condition));
    formData.append('Status', String(product.status));
    formData.append('PurchasePrice', String(product.unitPrice));
    formData.append('AllocatedShippingCost', String(shipping));
    formData.append('AllocatedOtherCosts', String(otherCosts));
    formData.append('Notes', product.notes);
    if (product.listingPrice !== null) formData.append('ListingPrice', String(product.listingPrice));
    if (product.minimumPrice !== null) formData.append('MinimumPrice', String(product.minimumPrice));
  }

  private orderPayload(draft: OrderDraft): object {
    return {
      EntryId: null,
      SourceId: draft.sourceId,
      TrackingNumber: draft.trackingNumber.trim() || null,
      ShippingCost: draft.shippingCost,
      OtherCosts: draft.otherCosts,
      Notes: draft.notes.trim() || null,
    };
  }

  private toPage(response: OrderPageApi, details: readonly OrderDetailsApi[], products: readonly ProductDetailsApi[]): OrderPage {
    const detailsById = new Map(details.map((detail) => [detail.id, detail]));
    return {
      ...response,
      items: response.items.map((item) => {
        const detail = detailsById.get(item.id);
        const orderProducts = products.filter((product) => product.purchaseOrderId === item.id);
        return this.toView(detail ?? item, orderProducts, detail?.notes ?? '');
      }),
    };
  }

  private toView(order: OrderListApi, products: readonly ProductDetailsApi[], notes: string): OrderView {
    const grouped = new Map<string, ProductDetailsApi[]>();
    products.forEach((product) => {
      const key = `${product.name}|${product.purchasePrice}|${product.description ?? ''}`;
      grouped.set(key, [...(grouped.get(key) ?? []), product]);
    });
    return {
      id: order.id,
      sourceId: order.sourceId,
      sourceName: order.sourceName || 'Sem origem',
      sourceInitials: this.initials(order.sourceName || 'SO'),
      trackingNumber: order.trackingNumber || 'Sem rastreio',
      status: orderStatusFromApi(order.status),
      shippingCost: Number(order.shippingCost) || 0,
      otherCosts: Number(order.otherCosts) || 0,
      orderedAt: order.orderedAt,
      deliveredAt: order.deliveredAt,
      createdAt: order.createdAt,
      notes,
      products: [...grouped.values()].map((group, index) => this.toProductView(group, index)),
    };
  }

  private toProductView(group: readonly ProductDetailsApi[], index: number): OrderProductView {
    const product = group[0];
    const coverImage = product.images.find((image) => image.isCover) ?? product.images[0] ?? null;
    return {
      ids: group.map((item) => item.id),
      coverImageIds: group.map((item) => (item.images.find((image) => image.isCover) ?? item.images[0])?.id ?? null),
      existingImageUrl: coverImage ? this.productImageUrl(product.id, coverImage.id) : null,
      image: null,
      imagePreviewUrl: null,
      name: product.name,
      description: product.description ?? '',
      quantity: group.length,
      unitPrice: Number(product.purchasePrice) || 0,
      condition: this.productConditionToApi(product.condition),
      status: this.productStatusToApi(product.status),
      category: product.category ?? '', brand: product.brand ?? '', size: product.size ?? '', color: product.color ?? '',
      listingPrice: product.listingPrice, minimumPrice: product.minimumPrice, notes: product.notes ?? '',
      accent: ['#667eea', '#a88760', '#4d7898', '#5f9786', '#9b6ead'][index % 5],
      initials: this.initials(product.name),
      coverImageUrl: coverImage ? this.productImageUrl(product.id, coverImage.id) : null,
    };
  }

  private productImageUrl(productId: number, imageId: number): string {
    return `${this.productEndpoint}/${productId}/images/${imageId}`;
  }

  private initials(value: string): string {
    const words = value.trim().split(/\s+/).filter(Boolean);
    return (words.length > 1 ? `${words[0][0]}${words[1][0]}` : words[0]?.slice(0, 2) || 'PR').toUpperCase();
  }

  private productConditionToApi(value: number | string): number {
    const normalized = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    return { '1': 1, '2': 2, '3': 3, '4': 4, '5': 5, newwithtags: 1, newwithouttags: 2, verygood: 3, good: 4, satisfactory: 5 }[normalized] ?? 3;
  }

  private productStatusToApi(value: number | string): number {
    const normalized = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    return { '1': 1, '2': 2, '3': 3, '4': 4, '5': 5, draft: 1, purchased: 2, active: 3, sold: 4, archived: 5 }[normalized] ?? 2;
  }
}
