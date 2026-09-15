import { Component, input } from '@angular/core';

export interface TaskCardModel {
  readonly id: number;
  readonly title: string;
  readonly description: string;
  readonly priority: 'Baixa' | 'Média' | 'Alta';
  readonly dueDate: string;
  readonly source: string;
  readonly assignee: string;
  readonly initials: string;
}

@Component({
  selector: 'app-task-card',
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.scss',
})
export class TaskCard {
  readonly task = input.required<TaskCardModel>();
}
