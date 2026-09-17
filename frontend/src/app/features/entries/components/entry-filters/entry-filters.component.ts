import { Component, input, output } from '@angular/core';

import { Dropdown } from '../../../../shared/ui/dropdown/dropdown.component';
import { EntryFilterOption } from '../../models/entry-list.model';

@Component({
  imports: [Dropdown],
  selector: 'app-entry-filters',
  styleUrl: './entry-filters.component.scss',
  templateUrl: './entry-filters.component.html',
})
export class EntryFilters {
  readonly searchTerm = input('');
  readonly filtersOpen = input(false);
  readonly accountOptions = input<readonly EntryFilterOption[]>([]);
  readonly sourceOptions = input<readonly EntryFilterOption[]>([]);
  readonly transactionTypeOptions = input<readonly EntryFilterOption[]>([]);
  readonly accountValue = input('all');
  readonly sourceValue = input('all');
  readonly transactionTypeValue = input('all');

  readonly searchTermChange = output<string>();
  readonly filtersOpenChange = output<boolean>();
  readonly accountValueChange = output<string>();
  readonly sourceValueChange = output<string>();
  readonly transactionTypeValueChange = output<string>();

  protected updateSearch(event: Event): void {
    this.searchTermChange.emit((event.target as HTMLInputElement).value);
  }

  protected toggleFilters(): void {
    this.filtersOpenChange.emit(!this.filtersOpen());
  }
}
