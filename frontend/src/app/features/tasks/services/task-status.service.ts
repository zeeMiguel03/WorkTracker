import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { CreateTaskStatusRequest, TaskStatus, UpdateTaskStatusRequest,} from '../models/task-status.model';

@Injectable({
  providedIn: 'root',
})
export class TaskStatusService {
  readonly http = inject(HttpClient);
  readonly endpoint = `${environment.apiUrl}/task-statuses`;

  list(): Observable<TaskStatus[]> {
    return this.http.get<TaskStatus[]>(this.endpoint, {
      withCredentials: true,
    });
  }

  getById(id: number): Observable<TaskStatus> {
    return this.http.get<TaskStatus>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }

  create(data: CreateTaskStatusRequest): Observable<TaskStatus> {
    return this.http.post<TaskStatus>(this.endpoint, data, {
      withCredentials: true,
    });
  }

  update(id: number, data: UpdateTaskStatusRequest): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${id}`, data, {
      withCredentials: true,
    });
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }
}
