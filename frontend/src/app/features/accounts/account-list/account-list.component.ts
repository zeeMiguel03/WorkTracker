import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, map, Subject } from 'rxjs';

import { Modal } from '../../../shared/ui/modal/modal.component';
import { AccountCard } from '../components/account-card/account-card.component';
import { AccountForm } from '../account-form/account-form.component';
import { Account, AccountApi, AccountDraft, AccountRequest, AccountType } from '../models/account.model';
import { AccountService } from '../services/account.service';

const ACCOUNT_TYPE_LABELS: Readonly<Record<AccountType, string>> = {
  cash: 'Dinheiro físico',
  bank: 'Conta bancária',
  'digital-wallet': 'Carteira digital',
  savings: 'Poupança',
  'credit-card': 'Cartão de crédito',
  investment: 'Investimentos',
};

const ACCOUNT_TYPE_TO_API: Readonly<Record<AccountType, number>> = {
  cash: 1,
  bank: 4,
  'digital-wallet': 7,
  savings: 5,
  'credit-card': 3,
  investment: 6,
};

const ACCOUNT_TYPE_FROM_API: Readonly<Record<string, AccountType>> = {
  '1': 'cash',
  '2': 'bank',
  '3': 'credit-card',
  '4': 'bank',
  '5': 'savings',
  '6': 'investment',
  '7': 'digital-wallet',
  cash: 'cash',
  debitcard: 'bank',
  creditcard: 'credit-card',
  bankaccount: 'bank',
  savings: 'savings',
  investment: 'investment',
  digitalwallet: 'digital-wallet',
};

@Component({
  imports: [AccountCard, AccountForm, Modal, RouterLink],
  selector: 'app-account-list',
  styleUrl: './account-list.component.scss',
  templateUrl: './account-list.component.html',
})
export class AccountList {
  private readonly accountService = inject(AccountService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanges = new Subject<string>();
  private loadRequestId = 0;

  protected readonly accounts = signal<readonly Account[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly accountModalOpen = signal(false);
  protected readonly selectedAccount = signal<Account | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = 8;
  protected readonly totalItems = signal(0);
  protected readonly totalPages = signal(0);

  protected readonly hasSearch = computed(() => this.searchTerm().trim().length > 0);
  protected readonly pageNumbers = computed(() => {
    const totalPages = this.totalPages();
    const currentPage = this.page();
    const maxButtons = 5;

    if (totalPages <= maxButtons) {
      return Array.from({ length: totalPages }, (_, index) => index + 1);
    }

    const start = Math.max(1, Math.min(currentPage - 2, totalPages - maxButtons + 1));
    return Array.from({ length: maxButtons }, (_, index) => start + index);
  });
  protected readonly mobilePageNumbers = computed(() => {
    const totalPages = this.totalPages();
    const currentPage = this.page();
    const visiblePages = Math.min(totalPages, 3);
    const start = Math.max(1, Math.min(currentPage - 1, totalPages - visiblePages + 1));

    return Array.from({ length: visiblePages }, (_, index) => start + index);
  });

  constructor() {
    this.searchChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((search) => {
        this.searchTerm.set(search);
        this.loadAccounts(1);
      });

    this.loadAccounts(1);
  }

  protected updateSearch(event: Event): void {
    this.searchChanges.next((event.target as HTMLInputElement).value);
  }

  protected clearSearch(): void {
    this.searchChanges.next('');
  }

  protected loadAccounts(page = this.page()): void {
    const requestId = ++this.loadRequestId;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.accountService
      .list(page, this.pageSize, this.searchTerm())
      .pipe(finalize(() => {
        if (requestId === this.loadRequestId) {
          this.isLoading.set(false);
        }
      }))
      .subscribe({
        next: (response) => {
          if (requestId !== this.loadRequestId) {
            return;
          }

          if (response.totalPages > 0 && page > response.totalPages) {
            this.isLoading.set(false);
            this.loadAccounts(response.totalPages);
            return;
          }

          this.accounts.set(response.items.map((account) => this.toCardModel(account)));
          this.page.set(response.totalPages === 0 ? 1 : response.page);
          this.totalItems.set(response.totalItems);
          this.totalPages.set(response.totalPages);
        },
        error: () => {
          if (requestId === this.loadRequestId) {
            this.errorMessage.set('Não foi possível carregar as contas.');
          }
        },
      });
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.loadAccounts(page);
  }

  protected openCreateAccount(): void {
    this.selectedAccount.set(null);
    this.errorMessage.set(null);
    this.accountModalOpen.set(true);
  }

  protected openEditAccount(accountId: number): void {
    this.selectedAccount.set(this.accounts().find((account) => account.id === accountId) ?? null);
    this.errorMessage.set(null);
    this.accountModalOpen.set(true);
  }

  protected closeAccountModal(): void {
    if (this.isSaving()) {
      return;
    }

    this.accountModalOpen.set(false);
    this.selectedAccount.set(null);
  }

  protected saveAccount(draft: AccountDraft): void {
    const selected = this.selectedAccount();
    const request = this.toRequest(draft);

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const request$ = selected
      ? this.accountService.update(selected.id, request)
      : this.accountService.create(request).pipe(map(() => undefined));

    request$
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.accountModalOpen.set(false);
          this.selectedAccount.set(null);
          this.loadAccounts(selected ? this.page() : 1);
        },
        error: () => {
          this.errorMessage.set('Não foi possível guardar a conta.');
        },
      });
  }

  protected deleteAccount(accountId: number): void {
    this.accountService.remove(accountId).subscribe({
      next: () => this.loadAccounts(this.page()),
      error: () => this.errorMessage.set('Não foi possível apagar a conta.'),
    });
  }

  private toRequest(draft: AccountDraft): AccountRequest {
    return {
      accountType: ACCOUNT_TYPE_TO_API[draft.type],
      name: draft.name.trim(),
      bankName: null,
      cardBrand: null,
      last4: null,
      iconKey: draft.type,
      color: draft.color,
      initialBalance: Number(draft.balance) || 0,
      includeInTotal: draft.includeInTotal,
    };
  }

  private toCardModel(account: AccountApi): Account {
    const type = this.toAccountType(account.accountType);
    const detail = account.bankName
      ?? (account.last4 ? `Cartão ···· ${account.last4}` : 'Saldo inicial');

    return {
      id: account.id,
      name: account.name,
      type,
      typeLabel: ACCOUNT_TYPE_LABELS[type],
      balance: Number(account.initialBalance) || 0,
      currency: 'EUR',
      color: account.color || this.getAccountColor(account.id),
      includeInTotal: account.includeInTotal,
      detail,
    };
  }

  private toAccountType(value: number | string): AccountType {
    const key = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    return ACCOUNT_TYPE_FROM_API[key] ?? 'bank';
  }

  private getAccountColor(id: number): string {
    const colors = ['#7592FF', '#32D583', '#F79009', '#F97066', '#B692F6'];
    return colors[id % colors.length];
  }
}
