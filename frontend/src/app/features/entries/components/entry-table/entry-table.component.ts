import { Component, input, output } from '@angular/core';

import { EntryListItem, EntrySortField } from '../../models/entry-list.model';

@Component({
  selector: 'app-entry-table',
  styleUrl: './entry-table.component.scss',
  templateUrl: './entry-table.component.html',
})
export class EntryTable {
  readonly entries = input.required<readonly EntryListItem[]>();
  readonly sortField = input<EntrySortField>('date');
  readonly sortDirection = input<'asc' | 'desc'>('desc');
  readonly sortRequested = output<EntrySortField>();
  readonly editRequested = output<number>();
  readonly deleteRequested = output<number>();

  protected requestSort(field: EntrySortField): void {
    this.sortRequested.emit(field);
  }

  protected formatValue(value: number): string {
    return new Intl.NumberFormat('pt-PT', {
      style: 'currency',
      currency: 'EUR',
      minimumFractionDigits: 2,
    }).format(value);
  }

  protected formatDate(date: string): string {
    return new Intl.DateTimeFormat('pt-PT', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(new Date(date));
  }

  protected getInitials(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((word) => word.charAt(0))
      .join('')
      .toUpperCase();
  }

  protected isSorted(field: EntrySortField): boolean {
    return this.sortField() === field;
  }
}
