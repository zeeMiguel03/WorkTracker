import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { Source } from '../../sources/models/source.model';
import { CreateTaskRequest, Task } from '../models/task.model';
import { TaskStatus } from '../models/task-status.model';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-task-form',
  styleUrl: './task-form.component.scss',
  templateUrl: './task-form.component.html',
})
export class TaskForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly task = input<Task | null>(null);
  readonly statuses = input<readonly TaskStatus[]>([]);
  readonly sources = input<readonly Source[]>([]);
  readonly defaultStatusId = input<number | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly submitted = output<CreateTaskRequest>();
  readonly cancelled = output<void>();

  readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(500)]],
    taskStatusId: [0, [Validators.required, Validators.min(1)]],
    sourceId: [0, [Validators.min(0)]],
    priority: [1, [Validators.required, Validators.min(0)]],
    sortOrder: [0, [Validators.required, Validators.min(0)]],
    dueDate: [''],
  });

  constructor() {
    effect(() => {
      const task = this.task();
      const firstStatusId = this.statuses()[0]?.id ?? 0;

      this.form.reset({
        title: task?.title ?? '',
        description: task?.description ?? '',
        taskStatusId: task?.taskStatusId ?? this.defaultStatusId() ?? firstStatusId,
        sourceId: task?.sourceId ?? 0,
        priority: task?.priority ?? 1,
        sortOrder: task?.sortOrder ?? 0,
        dueDate: task?.dueDate ? task.dueDate.slice(0, 10) : '',
      });
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    this.submitted.emit({
      sourceId: value.sourceId > 0 ? value.sourceId : null,
      taskStatusId: value.taskStatusId,
      title: value.title.trim(),
      description: value.description.trim() || null,
      priority: value.priority,
      sortOrder: value.sortOrder,
      dueDate: value.dueDate || null,
    });
  }

  protected hasError(controlName: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }
}
