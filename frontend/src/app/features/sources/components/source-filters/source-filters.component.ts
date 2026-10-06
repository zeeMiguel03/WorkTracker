import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';

@Component({
  imports: [TranslatePipe],
  selector: 'app-source-filters',
  styleUrl: './source-filters.component.scss',
  templateUrl: './source-filters.component.html',
})
export class SourceFilters {
  readonly searchTerm = input('');
  readonly searchTermChange = output<string>();

  protected updateSearch(event: Event): void {
    this.searchTermChange.emit((event.target as HTMLInputElement).value);
  }
}
