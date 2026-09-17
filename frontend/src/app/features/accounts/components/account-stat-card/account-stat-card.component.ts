import { Component, input } from '@angular/core';

@Component({
  selector: 'app-account-stat-card',
  templateUrl: './account-stat-card.component.html',
  styleUrl: './account-stat-card.component.scss',
})
export class AccountStatCard {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly detail = input<string | null>(null);
  readonly icon = input<'balance' | 'accounts' | 'income' | 'expense'>('balance');
  readonly tone = input<'blue' | 'green' | 'orange' | 'red'>('blue');
}
