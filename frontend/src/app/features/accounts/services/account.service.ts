import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AccountPage, AccountRequest, AccountApi } from '../models/account.model';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/accounts`;

  list(page = 1, pageSize = 8, search = ''): Observable<AccountPage> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<AccountPage>(this.endpoint, {
      params,
      withCredentials: true,
    });
  }

  listAll(): Observable<AccountApi[]> {
    return this.list(1, 50).pipe(map((response) => response.items));
  }

  create(data: AccountRequest): Observable<AccountApi> {
    return this.http.post<AccountApi>(this.endpoint, data, { withCredentials: true });
  }

  update(id: number, data: AccountRequest): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${id}`, data, { withCredentials: true });
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`, { withCredentials: true });
  }
}
