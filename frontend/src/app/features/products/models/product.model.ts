export type ProductStatus = 'draft' | 'purchased' | 'active' | 'sold' | 'archived';

export type ProductCondition =
  | 'new-with-tags'
  | 'new-without-tags'
  | 'very-good'
  | 'good'
  | 'satisfactory';

export interface ProductListItemApi {
  readonly id: number;
  readonly name: string;
  readonly category: string | null;
  readonly brand: string | null;
  readonly size: string | null;
  readonly color: string | null;
  readonly condition: number | string;
  readonly status: number | string;
  readonly listingPrice: number | null;
  readonly createdAt: string;
  readonly coverImageId: number | null;
  readonly coverImageUrl: string | null;
}

export interface ProductPage {
  readonly items: ProductListItemApi[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly hasPrevious: boolean;
  readonly hasNext: boolean;
}

export interface ProductImageApi {
  readonly id: number;
  readonly productId: number;
  readonly imageUrl: string;
  readonly displayOrder: number;
  readonly isCover: boolean;
  readonly createdAt: string;
}

export interface ProductDetailsApi {
  readonly id: number;
  readonly purchaseOrderId: number | null;
  readonly name: string;
  readonly description: string | null;
  readonly category: string | null;
  readonly brand: string | null;
  readonly size: string | null;
  readonly color: string | null;
  readonly condition: number | string;
  readonly purchasePrice: number;
  readonly allocatedShippingCost: number;
  readonly allocatedOtherCosts: number;
  readonly listingPrice: number | null;
  readonly minimumPrice: number | null;
  readonly status: number | string;
  readonly saleEntryId: number | null;
  readonly saleSourceId: number | null;
  readonly salePrice: number | null;
  readonly saleOtherCosts: number | null;
  readonly soldAt: string | null;
  readonly notes: string | null;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly images: readonly ProductImageApi[];
}

export interface ProductDraft {
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

export interface ProductListItem {
  readonly id: number;
  readonly name: string;
  readonly category: string;
  readonly price: number;
  readonly status: ProductStatus;
  readonly imageId: number | null;
  readonly imageUrl: string | null;
}
