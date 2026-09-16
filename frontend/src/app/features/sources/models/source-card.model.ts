export interface SourceCardModel {
  readonly id: number;
  readonly name: string;
  readonly createdAt: string;
  readonly imageUrl: string | null;
  readonly websiteUrl: string | null;
  readonly initials: string;
  readonly color: string;
  readonly transactions: number;
  readonly isActive: boolean;
}
