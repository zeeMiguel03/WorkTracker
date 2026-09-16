export interface Task {
  readonly id: number;
  readonly userId: number;
  readonly sourceId: number | null;
  readonly taskStatusId: number;
  readonly title: string;
  readonly description: string | null;
  readonly priority: number;
  readonly sortOrder: number;
  readonly dueDate: string | null;
  readonly completedAt: string | null;
  readonly createdAt: string;
  readonly utCreation: number | null;
}

export interface TaskPage {
  readonly items: Task[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly hasPrevious: boolean;
  readonly hasNext: boolean;
}

export interface CreateTaskRequest {
  sourceId: number | null;
  taskStatusId: number;
  title: string;
  description: string | null;
  priority: number;
  sortOrder: number;
  dueDate: string | null;
}

export type UpdateTaskRequest = CreateTaskRequest;
