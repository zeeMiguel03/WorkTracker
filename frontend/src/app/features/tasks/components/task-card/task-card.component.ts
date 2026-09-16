import { Component, input, output, signal } from '@angular/core';

import { TaskCardModel } from '../../models/task-card.model';

@Component({
  selector: 'app-task-card',
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.scss',
})
export class TaskCard {
  readonly task = input.required<TaskCardModel>();
  readonly editRequested = output<number>();
  readonly deleteRequested = output<number>();
  readonly completionRequested = output<number>();
  readonly reopenRequested = output<number>();

  protected readonly menuOpen = signal(false);

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected edit(): void {
    this.menuOpen.set(false);
    this.editRequested.emit(this.task().id);
  }

  protected remove(): void {
    this.menuOpen.set(false);
    this.deleteRequested.emit(this.task().id);
  }

  protected toggleCompletion(): void {
    this.menuOpen.set(false);

    if (this.task().completed) {
      this.reopenRequested.emit(this.task().id);
      return;
    }

    this.completionRequested.emit(this.task().id);
  }
}
