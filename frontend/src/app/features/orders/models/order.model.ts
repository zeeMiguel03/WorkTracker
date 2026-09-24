export type OrderStatus = 'draft' | 'ordered' | 'transit' | 'partial' | 'received' | 'cancelled';

export interface OrderProductDraft {
  readonly ids: readonly number[];
  readonly coverImageIds: readonly (number | null)[];
  readonly existingImageUrl: string | null;
  readonly image: File | null;
  readonly imagePreviewUrl: string | null;
  readonly name: string;
  readonly description: string;
  readonly quantity: number;
  readonly unitPrice: number;
  readonly condition: number;
  readonly status: number;
  readonly category: string;
  readonly brand: string;
  readonly size: string;
  readonly color: string;
  readonly listingPrice: number | null;
  readonly minimumPrice: number | null;
  readonly notes: string;
}

export interface OrderDraft {
  readonly sourceId: number | null;
  readonly orderedDate: string;
  readonly trackingNumber: string;
  readonly shippingCost: number;
  readonly otherCosts: number;
  readonly notes: string;
  readonly products: readonly OrderProductDraft[];
}

export interface OrderProductView extends OrderProductDraft {
  readonly accent: string;
  readonly initials: string;
  readonly coverImageUrl: string | null;
}

export interface OrderView {
  readonly id: number;
  readonly sourceId: number | null;
  readonly sourceName: string;
  readonly sourceInitials: string;
  readonly trackingNumber: string;
  readonly status: OrderStatus;
  readonly shippingCost: number;
  readonly otherCosts: number;
  readonly orderedAt: string | null;
  readonly deliveredAt: string | null;
  readonly createdAt: string;
  readonly notes: string;
  readonly products: readonly OrderProductView[];
}

export interface OrderPage {
  readonly items: readonly OrderView[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly hasPrevious: boolean;
  readonly hasNext: boolean;
}

export interface OrderSourceOption {
  readonly id: number;
  readonly name: string;
}

export interface OrderListApi {
  readonly id: number;
  readonly sourceId: number | null;
  readonly sourceName: string | null;
  readonly trackingNumber: string | null;
  readonly status: number | string;
  readonly shippingCost: number;
  readonly otherCosts: number | null;
  readonly orderedAt: string | null;
  readonly deliveredAt: string | null;
  readonly createdAt: string;
}

export interface OrderDetailsApi extends OrderListApi {
  readonly entryId: number | null;
  readonly notes: string | null;
  readonly updatedAt: string;
}

export interface OrderPageApi {
  readonly items: readonly OrderListApi[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly hasPrevious: boolean;
  readonly hasNext: boolean;
}

export const ORDER_STATUS_API: Readonly<Record<OrderStatus, number>> = {
  draft: 1,
  ordered: 2,
  transit: 3,
  partial: 4,
  received: 5,
  cancelled: 6,
};

export function orderStatusFromApi(value: number | string): OrderStatus {
  const normalized = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
  const statuses: Record<string, OrderStatus> = {
    '1': 'draft', '2': 'ordered', '3': 'transit', '4': 'partial', '5': 'received', '6': 'cancelled',
    draft: 'draft', ordered: 'ordered', intransit: 'transit', partiallyreceived: 'partial', received: 'received', cancelled: 'cancelled',
  };
  return statuses[normalized] ?? 'draft';
}
