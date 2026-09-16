export interface TaskStatus {
  readonly id: number;
  readonly userId: number;
  readonly name: string;
  readonly color: string;
  readonly sortOrder: number;
  readonly createdAt: string;
  readonly utCreation: number | null;
}

export interface CreateTaskStatusRequest {
  name: string;
  color: string;
  sortOrder: number;
}

export type UpdateTaskStatusRequest = CreateTaskStatusRequest;