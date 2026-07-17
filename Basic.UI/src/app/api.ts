export const STS_URL = 'http://localhost:5143';
export const API_URL = 'http://localhost:5216';

export type TaskStatus = 'Pending' | 'InProgress' | 'Done';

export interface TaskItem {
  id: number;
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string | null;
  userId: number;
}
