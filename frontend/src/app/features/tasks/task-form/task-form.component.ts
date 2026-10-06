import { Component, computed, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { Source } from '../../sources/models/source.model';
import { CreateTaskRequest, Task } from '../models/task.model';
import { TaskStatus } from '../models/task-status.model';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  imports: [Dropdown, ReactiveFormsModule, TranslatePipe],
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

  protected readonly priorityOptions: readonly DropdownOption[] = [
    { value: '0', label: 'Baixa' },
    { value: '1', label: 'Média' },
    { value: '2', label: 'Alta' },
  ];

  protected readonly statusOptions = computed<readonly DropdownOption[]>(() =>
    this.statuses().map((status) => ({ value: `${status.id}`, label: status.name })),
  );

  protected readonly sourceOptions = computed<readonly DropdownOption[]>(() => [
    { value: '0', label: 'Sem fonte' },
    ...this.sources()
      .filter((source) => source.isActive)
      .map((source) => ({ value: `${source.id}`, label: source.name })),
  ]);

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

  protected chooseStatus(value: string): void {
    const statusId = Number(value);

    if (this.statuses().some((status) => status.id === statusId)) {
      this.form.controls.taskStatusId.setValue(statusId);
    }
  }

  protected chooseSource(value: string): void {
    const sourceId = Number(value);

    if (value === '0' || this.sources().some((source) => source.id === sourceId && source.isActive)) {
      this.form.controls.sourceId.setValue(sourceId);
    }
  }

  protected choosePriority(value: string): void {
    const priority = Number(value);

    if (this.priorityOptions.some((option) => option.value === value)) {
      this.form.controls.priority.setValue(priority);
    }
  }
}
