export interface Source {
  readonly id: number;
  readonly userId: number;
  readonly name: string;
  readonly imageUrl: string | null;
  readonly link: string | null;
  readonly createdAt: string;
  readonly isActive: boolean;
  readonly utCreation: number | null;
}

export interface PaginatedResponse<T> {
  readonly items: T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly hasPrevious: boolean;
  readonly hasNext: boolean;
}

export interface CreateSourceRequest {
  name: string;
  imageFile: File | null;
  link: string;
}

export interface UpdateSourceRequest {
  name: string;
  imageFile: File | null;
  link: string;
  isActive: boolean;
}
