import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_URL, TaskItem } from './api';

export type TaskPayload = Omit<TaskItem, 'id' | 'userId'>;

@Injectable({ providedIn: 'root' })
export class TasksService {
  private http = inject(HttpClient);

  getAll() {
    return this.http.get<TaskItem[]>(`${API_URL}/api/tasks`);
  }

  create(task: TaskPayload) {
    return this.http.post<TaskItem>(`${API_URL}/api/tasks`, task);
  }

  update(id: number, task: TaskPayload) {
    return this.http.put<TaskItem>(`${API_URL}/api/tasks/${id}`, task);
  }

  delete(id: number) {
    return this.http.delete(`${API_URL}/api/tasks/${id}`);
  }
}
