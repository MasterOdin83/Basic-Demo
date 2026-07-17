import { Component, ElementRef, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { TaskItem, TaskStatus } from '../api';
import { TaskPayload, TasksService } from '../tasks.service';

const buildTaskForm = (fb: FormBuilder) =>
  fb.nonNullable.group({
    title: ['', [Validators.required, Validators.pattern(/\S/)]],
    description: [''],
    status: ['Pending' as TaskStatus],
    dueDate: [null as string | null],
  });

type TaskForm = ReturnType<typeof buildTaskForm>;

@Component({
  selector: 'app-tasks',
  templateUrl: './tasks.html',
  standalone: false,
})
export class Tasks implements OnInit {
  private api = inject(TasksService);
  private el = inject(ElementRef);
  private fb = inject(FormBuilder);

  readonly statuses: TaskStatus[] = ['Pending', 'InProgress', 'Done'];
  // Zoneless app: state written in subscribe callbacks must be signals or it never renders.
  readonly tasks = signal<TaskItem[]>([]);
  readonly error = signal('');
  readonly editError = signal('');
  readonly busy = signal(false);
  readonly editingId = signal(0);
  readonly submitted = signal(false);
  readonly editSubmitted = signal(false);

  // Two independent forms: editing a row never touches an add in progress.
  readonly form = buildTaskForm(this.fb);
  readonly editForm = buildTaskForm(this.fb);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.getAll().subscribe({
      next: (tasks) => this.tasks.set(tasks),
      error: () => this.error.set('Could not load tasks.'),
    });
  }

  titleInvalid(): boolean {
    const title = this.form.controls.title;
    return title.invalid && (title.touched || this.submitted());
  }

  editTitleInvalid(): boolean {
    const title = this.editForm.controls.title;
    return title.invalid && (title.touched || this.editSubmitted());
  }

  save(): void {
    this.submitted.set(true);
    this.error.set('');
    if (this.form.invalid) {
      this.nudgeInvalid('.task-form');
      return;
    }

    this.busy.set(true);
    this.api.create(this.toPayload(this.form)).subscribe({
      next: () => {
        this.busy.set(false);
        this.submitted.set(false);
        this.form.reset();
        this.load();
      },
      error: (e) => {
        this.busy.set(false);
        this.error.set(e.error?.error ?? 'Could not save the task.');
      },
    });
  }

  edit(task: TaskItem): void {
    this.editingId.set(task.id);
    this.editSubmitted.set(false);
    this.editError.set('');
    this.editForm.reset({
      title: task.title,
      description: task.description,
      status: task.status,
      dueDate: task.dueDate ? task.dueDate.slice(0, 10) : null,
    });
  }

  saveEdit(): void {
    this.editSubmitted.set(true);
    this.editError.set('');
    if (this.editForm.invalid) {
      this.nudgeInvalid('.task-edit');
      return;
    }

    this.busy.set(true);
    this.api.update(this.editingId(), this.toPayload(this.editForm)).subscribe({
      next: () => {
        this.busy.set(false);
        this.cancelEdit();
        this.load();
      },
      error: (e) => {
        this.busy.set(false);
        this.editError.set(e.error?.error ?? 'Could not save the task.');
      },
    });
  }

  cancelEdit(): void {
    this.editingId.set(0);
    this.editSubmitted.set(false);
    this.editError.set('');
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
        next: (updated) =>
          this.tasks.update((list) => list.map((t) => (t.id === updated.id ? updated : t))),
        error: () => this.error.set('Could not update the status.'),
      });
  }

  remove(task: TaskItem): void {
    if (!confirm(`Delete "${task.title}"?`)) return;
    this.api.delete(task.id).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Could not delete the task.'),
    });
  }

  private toPayload(form: TaskForm): TaskPayload {
    const value = form.getRawValue();
    return {
      title: value.title.trim(),
      description: value.description,
      status: value.status,
      dueDate: value.dueDate || null,
    };
  }

  // Focus the offending field and give it a small shake so the rejection is felt, not just read.
  private nudgeInvalid(scope: string): void {
    const invalid = (this.el.nativeElement as HTMLElement).querySelector<HTMLElement>(
      `${scope} .ng-invalid`,
    );
    if (!invalid) return;
    invalid.focus();
    if (!matchMedia('(prefers-reduced-motion: reduce)').matches) {
      invalid.animate(
        {
          transform: [
            'translateX(0)',
            'translateX(-5px)',
            'translateX(4px)',
            'translateX(-2px)',
            'translateX(0)',
          ],
        },
        { duration: 240, easing: 'ease-out' },
      );
    }
  }
}
