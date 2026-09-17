import { Component, effect, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Dropdown } from '../../../shared/ui/dropdown/dropdown.component';
import { Account, AccountDraft, AccountType } from '../models/account.model';

@Component({
  imports: [Dropdown, FormsModule],
  selector: 'app-account-form',
  styleUrl: './account-form.component.scss',
  templateUrl: './account-form.component.html',
})
export class AccountForm {
  readonly account = input<Account | null>(null);
  readonly submitted = output<AccountDraft>();
  readonly cancelled = output<void>();

  protected readonly accountTypes: ReadonlyArray<{ value: AccountType; label: string }> = [
    { value: 'cash', label: 'Dinheiro físico' },
    { value: 'bank', label: 'Conta bancária' },
    { value: 'digital-wallet', label: 'Carteira digital' },
    { value: 'savings', label: 'Poupança' },
    { value: 'credit-card', label: 'Cartão de crédito' },
    { value: 'investment', label: 'Investimentos' },
  ];

  protected readonly colors = ['#7592FF', '#32D583', '#F79009', '#F97066', '#B692F6'];

  protected draft: AccountDraft = this.createEmptyDraft();

  private readonly accountEffect = effect(() => {
    const account = this.account();

    this.draft = account
      ? {
          name: account.name,
          type: account.type,
          balance: account.balance,
          currency: account.currency,
          color: account.color,
          includeInTotal: account.includeInTotal,
        }
      : this.createEmptyDraft();
  });

  protected submit(): void {
    this.submitted.emit({ ...this.draft, balance: Number(this.draft.balance) || 0 });
  }

  protected selectColor(color: string): void {
    this.draft = { ...this.draft, color };
  }

  protected selectType(value: string): void {
    if (this.accountTypes.some((type) => type.value === value)) {
      this.draft = { ...this.draft, type: value as AccountType };
    }
  }

  private createEmptyDraft(): AccountDraft {
    return {
      name: '',
      type: 'bank',
      balance: 0,
      currency: 'EUR',
      color: '#7592FF',
      includeInTotal: true,
    };
  }
}
