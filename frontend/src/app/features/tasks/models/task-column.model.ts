import { TaskCardModel } from './task-card.model';

export interface TaskColumnModel {
  readonly id: number;
  readonly title: string;
  readonly accent: string;
  readonly tasks: readonly TaskCardModel[];
}