import { Component, input, output } from '@angular/core';

import { Dropdown, DropdownOption } from '../../../../shared/ui/dropdown/dropdown.component';

@Component({
  imports: [Dropdown],
  selector: 'app-product-transaction-fields',
  styleUrl: './product-transaction-fields.component.scss',
  templateUrl: './product-transaction-fields.component.html',
})
export class ProductTransactionFields {
  readonly sourceOptions = input<readonly DropdownOption[]>([]);
  readonly accountOptions = input<readonly DropdownOption[]>([]);
  readonly sourceValue = input<number | null>(null);
  readonly accountValue = input<number | null>(null);
  readonly sourceLabel = input('Fonte');
  readonly accountLabel = input('Conta');

  readonly sourceValueChange = output<string>();
  readonly accountValueChange = output<string>();

  protected optionValue(value: number | null): string {
    return value === null ? '' : String(value);
  }
}
