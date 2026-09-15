import { Component, input } from '@angular/core';
import { TaskCard, TaskCardModel } from '../task-card/task-card.component';

@Component({
  imports: [TaskCard],
  selector: 'app-task-column',
  templateUrl: './task-column.component.html',
  styleUrl: './task-column.component.scss',
})
export class TaskColumn {
  readonly title = input.required<string>();
  readonly count = input.required<number>();
  readonly accent = input.required<string>();
  readonly tasks = input.required<readonly TaskCardModel[]>();
}
