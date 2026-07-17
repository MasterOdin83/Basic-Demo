import { Component, OnInit, inject } from '@angular/core';
import { TaskItem, TaskStatus } from '../api';
import { TaskPayload, TasksService } from '../tasks.service';

const emptyForm = (): TaskPayload & { id: number } => ({
  id: 0,
  title: '',
  description: '',
  status: 'Pending',
  dueDate: null,
});

@Component({
  selector: 'app-tasks',
  templateUrl: './tasks.html',
  standalone: false,
})
export class Tasks implements OnInit {
  private api = inject(TasksService);

  readonly statuses: TaskStatus[] = ['Pending', 'InProgress', 'Done'];
  tasks: TaskItem[] = [];
  form = emptyForm();
  error = '';
  busy = false;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.getAll().subscribe({
      next: (tasks) => (this.tasks = tasks),
      error: () => (this.error = 'Could not load tasks.'),
    });
  }

  edit(task: TaskItem): void {
    this.form = {
      id: task.id,
      title: task.title,
      description: task.description,
      status: task.status,
      dueDate: task.dueDate ? task.dueDate.slice(0, 10) : null,
    };
  }

  cancel(): void {
    this.form = emptyForm();
    this.error = '';
  }

  save(): void {
    this.error = '';
    this.busy = true;
    const payload: TaskPayload = {
      title: this.form.title,
      description: this.form.description,
      status: this.form.status,
      dueDate: this.form.dueDate || null,
    };
    const request =
      this.form.id === 0 ? this.api.create(payload) : this.api.update(this.form.id, payload);

    request.subscribe({
      next: () => {
        this.busy = false;
        this.cancel();
        this.load();
      },
      error: (e) => {
        this.busy = false;
        this.error = e.error?.error ?? 'Could not save the task.';
      },
    });
  }

  setStatus(task: TaskItem, status: TaskStatus): void {
    this.api
      .update(task.id, {
        title: task.title,
        description: task.description,
        status,
        dueDate: task.dueDate,
      })
      .subscribe({
        next: (updated) => (task.status = updated.status),
        error: () => (this.error = 'Could not update the status.'),
      });
  }

  remove(task: TaskItem): void {
    if (!confirm(`Delete "${task.title}"?`)) return;
    this.api.delete(task.id).subscribe({
      next: () => this.load(),
      error: () => (this.error = 'Could not delete the task.'),
    });
  }
}
