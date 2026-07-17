import { environment } from '../environments/environment';

export const STS_URL = environment.stsUrl;
export const API_URL = environment.apiUrl;

export type TaskStatus = 'Pending' | 'InProgress' | 'Done';

export interface TaskItem {
  id: number;
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string | null;
  userId: number;
}
