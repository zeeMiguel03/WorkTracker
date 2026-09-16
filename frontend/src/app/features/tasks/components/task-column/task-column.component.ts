import { Component, input, output } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList } from '@angular/cdk/drag-drop';

import { TaskCard } from '../task-card/task-card.component';
import { TaskCardModel } from '../../models/task-card.model';

export interface TaskDropEvent {
  readonly taskId: number;
  readonly previousStatusId: number;
  readonly statusId: number;
  readonly newIndex: number;
}

@Component({
  imports: [CdkDrag, CdkDragHandle, CdkDropList, TaskCard],
  selector: 'app-task-column',
  templateUrl: './task-column.component.html',
  styleUrl: './task-column.component.scss',
})
export class TaskColumn {
  readonly columnId = input.required<number>();
  readonly title = input.required<string>();
  readonly count = input.required<number>();
  readonly accent = input.required<string>();
  readonly tasks = input.required<readonly TaskCardModel[]>();
  readonly hasMoreTasks = input(false);
  readonly loadingMoreTasks = input(false);
  readonly taskDropListIds = input<readonly string[]>([]);
  readonly addTaskRequested = output<number>();
  readonly editColumnRequested = output<number>();
  readonly deleteColumnRequested = output<number>();
  readonly editTaskRequested = output<number>();
  readonly deleteTaskRequested = output<number>();
  readonly completeTaskRequested = output<number>();
  readonly reopenTaskRequested = output<number>();
  readonly loadMoreRequested = output<number>();
  readonly taskDropped = output<TaskDropEvent>();

  protected canDropTask(drag: CdkDrag<unknown>): boolean {
    const data = drag.data as Partial<TaskCardModel> | undefined;

    return (
      typeof data?.id === 'number' &&
      typeof data.statusId === 'number' &&
      typeof data.title === 'string'
    );
  }

  protected drop(event: CdkDragDrop<readonly TaskCardModel[]>): void {
    const task = event.item.data as TaskCardModel | undefined;

    if (!task) {
      return;
    }

    this.taskDropped.emit({
      taskId: task.id,
      previousStatusId: task.statusId,
      statusId: this.columnId(),
      newIndex: event.currentIndex,
    });
  }
}
