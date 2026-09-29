import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export interface DashboardDaily {
  readonly date: string;
  readonly revenue: number;
  readonly profit: number;
}

export interface DashboardChannel {
  readonly name: string;
  readonly profit: number;
}

export interface DashboardSale {
  readonly id: number;
  readonly productId: number | null;
  readonly name: string;
  readonly saleDate: string;
  readonly sourceName: string;
  readonly salePrice: number;
  readonly profit: number;
}

export interface DashboardStock {
  readonly id: number;
  readonly name: string;
  readonly listingPrice: number;
  readonly acquisitionCost: number;
  readonly potentialProfit: number;
}

export interface DashboardData {
  readonly revenue: number;
  readonly profit: number;
  readonly soldCount: number;
  readonly invested: number;
  readonly stockCount: number;
  readonly daily: readonly DashboardDaily[];
  readonly sourceProfits: readonly DashboardChannel[];
  readonly sales: readonly DashboardSale[];
  readonly stock: readonly DashboardStock[];
}

@Injectable({ providedIn: 'root' })
export class DashboardDataService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/dashboard`;

  load(from: string, toExclusive: string): Observable<DashboardData> {
    const params = new HttpParams()
      .set('from', from)
      .set('toExclusive', toExclusive);

    return this.http.get<DashboardData>(this.endpoint, {
      params,
      withCredentials: true,
    });
  }
}
