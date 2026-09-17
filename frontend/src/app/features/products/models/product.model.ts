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

export interface ProductDraft {
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

export interface ProductListItem {
  readonly id: number;
  readonly name: string;
  readonly category: string;
  readonly price: number;
  readonly status: ProductStatus;
  readonly imageId: number | null;
  readonly imageUrl: string | null;
}
