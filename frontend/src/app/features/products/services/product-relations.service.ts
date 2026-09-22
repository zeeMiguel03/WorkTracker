import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { forkJoin, map, Observable, of, switchMap } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AccountService } from '../../accounts/services/account.service';
import { DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { SourceService } from '../../sources/services/source.service';

interface TransactionTypeApi {
  readonly id: number;
  readonly name: string;
  readonly color: string;
}

const DEFAULT_TRANSACTION_TYPES = [
  { name: 'Compra', color: '#F97066' },
  { name: 'Venda', color: '#32D583' },
] as const;

export interface ProductRelationOptions {
  readonly sourceOptions: readonly DropdownOption[];
  readonly accountOptions: readonly DropdownOption[];
  readonly transactionTypeOptions: readonly DropdownOption[];
}

@Injectable({ providedIn: 'root' })
export class ProductRelationsService {
  private readonly http = inject(HttpClient);
  private readonly accountService = inject(AccountService);
  private readonly sourceService = inject(SourceService);
  private readonly endpoint = `${environment.apiUrl}/transaction-types`;

  loadOptions(): Observable<ProductRelationOptions> {
    return forkJoin({
      sources: this.sourceService.listAll(),
      accounts: this.accountService.listAll(),
      transactionTypes: this.loadTransactionTypes(),
    }).pipe(
      map(({ sources, accounts, transactionTypes }) => ({
        sourceOptions: sources
          .filter((source) => source.isActive)
          .map((source) => ({
            value: String(source.id),
            label: source.name,
            imageUrl: source.imageUrl ? `${environment.apiUrl}/sources/${source.id}/image` : null,
          })),
        accountOptions: accounts.map((account) => ({
          value: String(account.id),
          label: account.name,
          color: account.color,
          meta: [account.bankName, account.last4 ? `•••• ${account.last4}` : null]
            .filter(Boolean)
            .join(' · ') || null,
        })),
        transactionTypeOptions: transactionTypes.map((type) => ({
          value: String(type.id),
          label: type.name,
          color: type.color,
        })),
      })),
    );
  }

  private loadTransactionTypes(): Observable<TransactionTypeApi[]> {
    return this.http.get<TransactionTypeApi[]>(this.endpoint, { withCredentials: true }).pipe(
      switchMap((transactionTypes) => {
        const missingTypes = DEFAULT_TRANSACTION_TYPES.filter((defaultType) =>
          !transactionTypes.some((type) => type.name.trim().toLowerCase() === defaultType.name.toLowerCase()),
        );

        if (!missingTypes.length) {
          return of(transactionTypes);
        }

        return forkJoin(
          missingTypes.map((type) => this.http.post<TransactionTypeApi>(this.endpoint, {
            Name: type.name,
            Color: type.color,
          }, { withCredentials: true })),
        ).pipe(map((createdTypes) => [...transactionTypes, ...createdTypes]));
      }),
    );
  }

}
