import { Component, input, output, signal } from '@angular/core';

import { Account } from '../../models/account.model';

@Component({
  selector: 'app-account-card',
  templateUrl: './account-card.component.html',
  styleUrl: './account-card.component.scss',
})
export class AccountCard {
  readonly account = input.required<Account>();
  readonly editRequested = output<number>();
  readonly deleteRequested = output<number>();
  readonly menuOpen = signal(false);

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected formatBalance(balance: number, currency: string): string {
    return new Intl.NumberFormat('pt-PT', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(balance) + ` ${currency}`;
  }
}
