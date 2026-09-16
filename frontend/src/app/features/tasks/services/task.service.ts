import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { CreateTaskRequest, Task, TaskPage, UpdateTaskRequest } from '../models/task.model';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/tasks`;

  list(page = 1, pageSize = 8, taskStatusId?: number, search = ''): Observable<TaskPage> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (taskStatusId !== undefined) {
      params = params.set('taskStatusId', taskStatusId);
    }

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<TaskPage>(this.endpoint, {
      params,
      withCredentials: true,
    });
  }

  getById(id: number): Observable<Task> {
    return this.http.get<Task>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }

  create(data: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(this.endpoint, data, {
      withCredentials: true,
    });
  }

  update(id: number, data: UpdateTaskRequest): Observable<void> {
    return this.http.put<void>(`${this.endpoint}/${id}`, data, {
      withCredentials: true,
    });
  }

  updateStatus(taskId: number, statusId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.endpoint}/${taskId}/status/${statusId}`,
      {},
      { withCredentials: true },
    );
  }

  updateSortOrder(taskId: number, sortOrder: number): Observable<void> {
    return this.http.patch<void>(
      `${this.endpoint}/${taskId}/sort-order/${sortOrder}`,
      {},
      { withCredentials: true },
    );
  }

  complete(id: number): Observable<void> {
    return this.http.post<void>(
      `${this.endpoint}/${id}/complete`,
      {},
      { withCredentials: true },
    );
  }

  reopen(id: number): Observable<void> {
    return this.http.post<void>(
      `${this.endpoint}/${id}/reopen`,
      {},
      { withCredentials: true },
    );
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.endpoint}/${id}`, {
      withCredentials: true,
    });
  }
}
