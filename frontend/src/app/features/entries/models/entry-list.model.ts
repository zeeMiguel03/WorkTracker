export interface EntryListItem {
  readonly id: number;
  readonly name: string;
  readonly source: string;
  readonly transactionType: string;
  readonly account: string;
  readonly quantity: number;
  readonly value: number;
  readonly date: string;
  readonly color: string;
  readonly imageUrl: string | null;
}

export interface EntryFilterOption {
  readonly value: string;
  readonly label: string;
}

export type EntrySortField = 'name' | 'source' | 'transactionType' | 'account' | 'quantity' | 'value' | 'date';

export type EntryOperationMode = 'single' | 'recurring';
export type EntryFlow = 'expense' | 'income';
export type EntryFrequency = 'daily' | 'weekly' | 'monthly' | 'yearly';
export type EntryEndMode = 'never' | 'date';

export interface EntryFormDraft {
  name: string;
  source: string;
  transactionType: string;
  account: string;
  quantity: number;
  value: number;
  date: string;
  description: string;
  operationMode: EntryOperationMode;
  flow: EntryFlow;
  frequency: EntryFrequency;
  endMode: EntryEndMode;
  endDate: string;
  imageFile: File | null;
}
