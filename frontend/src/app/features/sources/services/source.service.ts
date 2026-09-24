import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  CreateSourceRequest,
  PaginatedResponse,
  Source,
  UpdateSourceRequest,
} from '../models/source.model';

@Injectable({
  providedIn: 'root',
})
export class SourceService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/sources`;

  list(page = 1, pageSize = 6, search = ''): Observable<PaginatedResponse<Source>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<PaginatedResponse<Source>>(this.endpoint, {
      params,
      withCredentials: true,
    });
  }

  listAll(): Observable<Source[]> {
    return this.list(1, 50).pipe(map((response) => response.items));
  }

  getById(id: number): Observable<Source> {
    return this.http.get<Source>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }

  getImage(id: number): Observable<Blob> {
    return this.http.get(`${this.endpoint}/${id}/image`, {
      withCredentials: true,
      responseType: 'blob',
    });
  }

  create(data: CreateSourceRequest): Observable<Source> {
    return this.http.post<Source>(this.endpoint, this.toFormData(data), { withCredentials: true });
  }

  update(id: number, data: UpdateSourceRequest): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${id}`, this.toFormData(data), {
      withCredentials: true,
    });
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }

  private toFormData(data: CreateSourceRequest | UpdateSourceRequest): FormData {
    const formData = new FormData();

    formData.append('Name', data.name.trim());

    if (data.link.trim()) {
      formData.append('Link', data.link.trim());
    }

    if (data.imageFile) {
      formData.append('ImageUrl', data.imageFile, data.imageFile.name);
    }

    if ('isActive' in data) {
      formData.append('IsActive', String(data.isActive));
    }

    return formData;
  }
}
