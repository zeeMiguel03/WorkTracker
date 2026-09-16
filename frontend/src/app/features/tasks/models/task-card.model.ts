export type TaskPriorityLabel = 'Baixa' | 'Média' | 'Alta';

export interface TaskCardModel {
  readonly id: number;
  readonly statusId: number;
  readonly title: string;
  readonly description: string;
  readonly priority: TaskPriorityLabel;
  readonly dueDate: string;
  readonly source: string;
  readonly assignee: string;
  readonly initials: string;
  readonly avatarUrl: string | null;
  readonly completed: boolean;
}
