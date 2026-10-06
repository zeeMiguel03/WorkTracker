import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CreateTaskStatusRequest, TaskStatus } from '../../models/task-status.model';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';

@Component({
  imports: [ReactiveFormsModule, TranslatePipe],
  selector: 'app-task-status-form',
  templateUrl: './task-status-form.component.html',
  styleUrl: './task-status-form.component.scss',
})
export class TaskStatusForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly status = input<TaskStatus | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly submitted = output<CreateTaskStatusRequest>();
  readonly cancelled = output<void>();

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    color: ['#465FFF', [Validators.required, Validators.pattern(/^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/)]],
    sortOrder: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    effect(() => {
      const status = this.status();

      this.form.reset({
        name: status?.name ?? '',
        color: status?.color ?? '#465FFF',
        sortOrder: status?.sortOrder ?? 0,
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
      name: value.name.trim(),
      color: value.color.toUpperCase(),
      sortOrder: value.sortOrder,
    });
  }

  protected setColor(event: Event): void {
    this.form.controls.color.setValue((event.target as HTMLInputElement).value);
  }
}
