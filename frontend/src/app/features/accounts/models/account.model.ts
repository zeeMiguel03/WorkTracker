export type AccountType =
  | 'cash'
  | 'bank'
  | 'digital-wallet'
  | 'savings'
  | 'credit-card'
  | 'investment';

export interface Account {
  readonly id: number;
  readonly name: string;
  readonly type: AccountType;
  readonly typeLabel: string;
  readonly initialBalance: number;
  readonly balance: number;
  readonly currency: string;
  readonly color: string;
  readonly includeInTotal: boolean;
  readonly detail?: string;
}

export interface AccountDraft {
  name: string;
  type: AccountType;
  balance: number;
  currency: string;
  color: string;
  includeInTotal: boolean;
}

export interface AccountApi {
  readonly id: number;
  readonly userId: number;
  readonly accountType: number;
  readonly name: string;
  readonly bankName: string | null;
  readonly cardBrand: string | null;
  readonly last4: string | null;
  readonly iconKey: string | null;
  readonly color: string | null;
  readonly initialBalance: number;
  readonly includeInTotal: boolean;
  readonly createdAt: string;
  readonly utCreation: number | null;
}

export interface AccountPage {
  readonly items: AccountApi[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly hasPrevious: boolean;
  readonly hasNext: boolean;
}

export interface AccountRequest {
  readonly accountType: number;
  readonly name: string;
  readonly bankName: string | null;
  readonly cardBrand: string | null;
  readonly last4: string | null;
  readonly iconKey: string | null;
  readonly color: string | null;
  readonly initialBalance: number;
  readonly includeInTotal: boolean;
}
