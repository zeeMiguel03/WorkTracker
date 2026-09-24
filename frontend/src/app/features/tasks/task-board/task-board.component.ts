import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, effect, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import {
  debounceTime,
  distinctUntilChanged,
  forkJoin,
  map,
  Observable,
  of,
  Subject,
  Subscription,
  switchMap,
} from 'rxjs';

import { Modal } from '../../../shared/ui/modal/modal.component';
import { SuccessModal } from '../../../shared/ui/success-modal/success-modal.component';
import { Auth } from '../../auth/auth.service';
import { Source } from '../../sources/models/source.model';
import { SourceService } from '../../sources/services/source.service';
import { TaskColumn, TaskDropEvent } from '../components/task-column/task-column.component';
import { TaskStatusForm } from '../components/task-status-form/task-status-form.component';
import { TaskCardModel, TaskPriorityLabel } from '../models/task-card.model';
import { TaskColumnModel } from '../models/task-column.model';
import { CreateTaskRequest, Task, TaskPage } from '../models/task.model';
import { CreateTaskStatusRequest, TaskStatus } from '../models/task-status.model';
import { TaskStatusService } from '../services/task-status.service';
import { TaskService } from '../services/task.service';
import { TaskForm } from '../task-form/task-form.component';

type DeleteTarget =
  | { readonly type: 'task'; readonly id: number; readonly label: string }
  | { readonly type: 'status'; readonly id: number; readonly label: string };

@Component({
  imports: [
    CdkDrag,
    CdkDropList,
    Modal,
    RouterLink,
    SuccessModal,
    TaskColumn,
    TaskForm,
    TaskStatusForm,
  ],
  selector: 'app-task-board',
  styleUrl: './task-board.component.scss',
  templateUrl: './task-board.component.html',
})
export class TaskBoard implements OnDestroy, OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(Auth);
  private readonly taskService = inject(TaskService);
  private readonly taskStatusService = inject(TaskStatusService);
  private readonly sourceService = inject(SourceService);
  private readonly searchChanges = new Subject<string>();
  private readonly taskPageSize = 8;

  protected readonly tasks = signal<readonly Task[]>([]);
  protected readonly statuses = signal<readonly TaskStatus[]>([]);
  protected readonly sources = signal<readonly Source[]>([]);
  protected readonly taskTotals = signal<Readonly<Record<number, number>>>({});
  protected readonly taskPages = signal<Readonly<Record<number, number>>>({});
  protected readonly taskHasMore = signal<Readonly<Record<number, boolean>>>({});
  protected readonly loadingMoreTasks = signal<Readonly<Record<number, boolean>>>({});
  protected readonly searchTerm = signal('');
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly taskModalOpen = signal(false);
  protected readonly statusModalOpen = signal(false);
  protected readonly selectedTask = signal<Task | null>(null);
  protected readonly selectedStatus = signal<TaskStatus | null>(null);
  protected readonly defaultStatusId = signal<number | null>(null);
  protected readonly deleteTarget = signal<DeleteTarget | null>(null);
  protected readonly successModal = signal<{
    readonly title: string;
    readonly message: string;
  } | null>(null);
  private profileImageObjectUrl: string | null = null;
  private profileImageRequest: Subscription | null = null;

  private readonly profileImageEffect = effect(() => {
    this.auth.profileImageRevision();

    if (this.auth.isAuthenticated()) {
      this.loadProfileImage();
    }
  });

  protected readonly columns = computed<readonly TaskColumnModel[]>(() => {
    const query = this.normalize(this.searchTerm());

    return [...this.statuses()]
      .sort((first, second) => first.sortOrder - second.sortOrder)
      .map((status) => ({
        id: status.id,
        title: status.name,
        accent: status.color,
        tasks: this.tasks()
          .filter((task) => task.taskStatusId === status.id)
          .sort((first, second) => first.sortOrder - second.sortOrder)
          .filter((task) => {
            if (!query) {
              return true;
            }

            return this.normalize(`${task.title} ${task.description ?? ''}`).includes(query);
          })
          .map((task) => this.toCardModel(task)),
      }));
  });

  protected readonly taskDropListIds = computed<readonly string[]>(() =>
    this.statuses().map((status) => `task-column-${status.id}`),
  );

  ngOnInit(): void {
    this.searchChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadBoard());
    this.loadBoard();
  }

  ngOnDestroy(): void {
    this.profileImageRequest?.unsubscribe();
    this.revokeProfileImageUrl();
  }

  protected loadBoard(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    const search = this.searchTerm().trim();

    forkJoin({
      statuses: this.taskStatusService.list(),
      sources: this.sourceService.listAll(),
    })
      .pipe(
        switchMap(({ statuses, sources }) => {
          const pageRequests = statuses.map((status) =>
            this.taskService.list(1, this.taskPageSize, status.id, search),
          );
          const pages$ = pageRequests.length ? forkJoin(pageRequests) : of([] as TaskPage[]);

          return pages$.pipe(map((pages) => ({ pages, sources, statuses })));
        }),
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ pages, statuses, sources }) => {
          const totals: Record<number, number> = {};
          const loadedPages: Record<number, number> = {};
          const hasMore: Record<number, boolean> = {};

          statuses.forEach((status, index) => {
            const page = pages[index];
            totals[status.id] = page?.totalItems ?? 0;
            loadedPages[status.id] = page?.page ?? 1;
            hasMore[status.id] = page?.hasNext ?? false;
          });

          this.tasks.set(pages.flatMap((page) => page.items));
          this.statuses.set(statuses);
          this.sources.set(sources);
          this.taskTotals.set(totals);
          this.taskPages.set(loadedPages);
          this.taskHasMore.set(hasMore);
          this.loadingMoreTasks.set({});
          this.isLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(
            this.getErrorMessage(error, 'Não foi possível carregar as tarefas.'),
          );
          this.isLoading.set(false);
        },
      });
  }

  protected updateSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
    this.searchChanges.next(value);
  }

  protected loadMoreTasks(statusId: number): void {
    if (this.loadingMoreTasks()[statusId] || !this.taskHasMore()[statusId]) {
      return;
    }

    const nextPage = (this.taskPages()[statusId] ?? 1) + 1;
    this.loadingMoreTasks.update((current) => ({ ...current, [statusId]: true }));

    this.taskService
      .list(nextPage, this.taskPageSize, statusId, this.searchTerm().trim())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page) => {
          this.tasks.update((current) => {
            const existingIds = new Set(current.map((task) => task.id));
            const newTasks = page.items.filter((task) => !existingIds.has(task.id));

            return [...current, ...newTasks];
          });
          this.taskPages.update((current) => ({ ...current, [statusId]: page.page }));
          this.taskTotals.update((current) => ({ ...current, [statusId]: page.totalItems }));
          this.taskHasMore.update((current) => ({ ...current, [statusId]: page.hasNext }));
          this.loadingMoreTasks.update((current) => ({ ...current, [statusId]: false }));
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(
            this.getErrorMessage(error, 'Não foi possível carregar mais tarefas.'),
          );
          this.loadingMoreTasks.update((current) => ({ ...current, [statusId]: false }));
        },
      });
  }

  protected openCreateTask(statusId?: number): void {
    this.errorMessage.set(null);
    this.selectedTask.set(null);
    this.defaultStatusId.set(statusId ?? this.statuses()[0]?.id ?? null);
    this.taskModalOpen.set(true);
  }

  protected openEditTask(taskId: number): void {
    const task = this.tasks().find((item) => item.id === taskId);

    if (!task) {
      return;
    }

    this.errorMessage.set(null);
    this.selectedTask.set(task);
    this.taskModalOpen.set(true);
  }

  protected closeTaskModal(): void {
    if (!this.isSaving()) {
      this.taskModalOpen.set(false);
    }
  }

  protected saveTask(data: CreateTaskRequest): void {
    this.isSaving.set(true);
    this.errorMessage.set(null);

    const selectedTask = this.selectedTask();
    const request: Observable<unknown> = selectedTask
      ? this.taskService.update(selectedTask.id, data)
      : this.taskService.create({ ...data, sortOrder: this.nextSortOrder(data.taskStatusId) });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.taskModalOpen.set(false);
        this.showSuccess(
          selectedTask ? 'Tarefa atualizada!' : 'Tarefa criada!',
          selectedTask
            ? 'As alterações da tarefa foram guardadas com sucesso.'
            : 'A nova tarefa foi criada com sucesso.',
        );
        this.loadBoard();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.getErrorMessage(error, 'Não foi possível guardar a tarefa.'));
        this.isSaving.set(false);
      },
    });
  }

  protected openCreateStatus(): void {
    this.errorMessage.set(null);
    this.selectedStatus.set(null);
    this.statusModalOpen.set(true);
  }

  protected openEditStatus(statusId: number): void {
    const status = this.statuses().find((item) => item.id === statusId);

    if (!status) {
      return;
    }

    this.errorMessage.set(null);
    this.selectedStatus.set(status);
    this.statusModalOpen.set(true);
  }

  protected closeStatusModal(): void {
    if (!this.isSaving()) {
      this.statusModalOpen.set(false);
    }
  }

  protected saveStatus(data: CreateTaskStatusRequest): void {
    this.isSaving.set(true);
    this.errorMessage.set(null);

    const selectedStatus = this.selectedStatus();
    const request: Observable<number> = selectedStatus
      ? this.taskStatusService
          .update(selectedStatus.id, data)
          .pipe(map(() => selectedStatus.id))
      : this.taskStatusService.create(data).pipe(map((createdStatus) => createdStatus.id));

    request
      .pipe(
        switchMap((statusId) => this.normalizeStatusOrder(statusId, data.sortOrder)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.statusModalOpen.set(false);
          this.showSuccess(
            selectedStatus ? 'Coluna atualizada!' : 'Coluna criada!',
            selectedStatus
              ? 'As alterações da coluna foram guardadas com sucesso.'
              : 'A nova coluna foi criada com sucesso.',
          );
          this.loadBoard();
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(this.getErrorMessage(error, 'Não foi possível guardar a coluna.'));
          this.isSaving.set(false);
        },
      });
  }

  protected confirmDeleteTask(taskId: number): void {
    const task = this.tasks().find((item) => item.id === taskId);

    if (task) {
      this.deleteTarget.set({ type: 'task', id: task.id, label: task.title });
    }
  }

  protected confirmDeleteStatus(statusId: number): void {
    const status = this.statuses().find((item) => item.id === statusId);

    if (status) {
      this.deleteTarget.set({ type: 'status', id: status.id, label: status.name });
    }
  }

  protected closeDeleteModal(): void {
    if (!this.isSaving()) {
      this.deleteTarget.set(null);
    }
  }

  protected deleteSelected(): void {
    const target = this.deleteTarget();

    if (!target) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const request =
      target.type === 'task'
        ? this.taskService.remove(target.id)
        : this.taskStatusService.remove(target.id);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.deleteTarget.set(null);
        this.showSuccess(
          target.type === 'task' ? 'Tarefa apagada!' : 'Coluna apagada!',
          target.type === 'task'
            ? 'A tarefa foi removida com sucesso.'
            : 'A coluna foi removida com sucesso.',
        );
        this.loadBoard();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.getErrorMessage(error, 'Não foi possível concluir a operação.'));
        this.isSaving.set(false);
      },
    });
  }

  protected completeTask(taskId: number): void {
    this.updateCompletion(taskId, false);
  }

  protected reopenTask(taskId: number): void {
    this.updateCompletion(taskId, true);
  }

  protected handleTaskDrop(event: TaskDropEvent): void {
    if (event.previousStatusId === event.statusId && event.newIndex < 0) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const statusRequest =
      event.previousStatusId === event.statusId
        ? of(void 0)
        : this.taskService.updateStatus(event.taskId, event.statusId);

    statusRequest
      .pipe(
        switchMap(() => this.taskService.updateSortOrder(event.taskId, event.newIndex)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.loadBoard();
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(this.getErrorMessage(error, 'Não foi possível mover a tarefa.'));
          this.isSaving.set(false);
          this.loadBoard();
        },
      });
  }

  protected handleColumnDrop(event: CdkDragDrop<readonly TaskColumnModel[]>): void {
    if (
      event.previousIndex === event.currentIndex ||
      this.isSaving() ||
      !this.isColumnData(event.item.data)
    ) {
      return;
    }

    const reorderedColumns = [...this.columns()];
    const [movedColumn] = reorderedColumns.splice(event.previousIndex, 1);

    if (!movedColumn) {
      return;
    }

    reorderedColumns.splice(event.currentIndex, 0, movedColumn);
    this.isSaving.set(true);
    this.errorMessage.set(null);

    const requests = reorderedColumns.map((column, index) => {
      const status = this.statuses().find((item) => item.id === column.id);

      return status
        ? this.taskStatusService.update(status.id, {
            name: status.name,
            color: status.color,
            sortOrder: index,
          })
        : of(void 0);
    });

    forkJoin(requests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.showSuccess(
            'Colunas reordenadas!',
            'A nova ordem das colunas foi guardada com sucesso.',
          );
          this.loadBoard();
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(
            this.getErrorMessage(error, 'Não foi possível reordenar as colunas.'),
          );
          this.isSaving.set(false);
          this.loadBoard();
        },
      });
  }

  protected canDropColumn(drag: CdkDrag<unknown>): boolean {
    return this.isColumnData(drag.data);
  }

  protected closeSuccessModal(): void {
    this.successModal.set(null);
  }

  private updateCompletion(taskId: number, reopen: boolean): void {
    this.isSaving.set(true);
    this.errorMessage.set(null);

    const request = reopen ? this.taskService.reopen(taskId) : this.taskService.complete(taskId);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.showSuccess(
          reopen ? 'Tarefa reaberta!' : 'Tarefa concluída!',
          reopen
            ? 'A tarefa voltou a estar disponível no quadro.'
            : 'A tarefa foi marcada como concluída.',
        );
        this.loadBoard();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.getErrorMessage(error, 'Não foi possível atualizar a tarefa.'));
        this.isSaving.set(false);
      },
    });
  }

  private nextSortOrder(statusId: number): number {
    const totalTasks = this.taskTotals()[statusId];

    if (totalTasks !== undefined) {
      return totalTasks;
    }

    return (
      this.tasks()
        .filter((task) => task.taskStatusId === statusId)
        .reduce((highest, task) => Math.max(highest, task.sortOrder), -1) + 1
    );
  }

  private isColumnData(data: unknown): data is TaskColumnModel {
    if (!data || typeof data !== 'object') {
      return false;
    }

    const column = data as Partial<TaskColumnModel>;

    return (
      typeof column.id === 'number' &&
      typeof column.title === 'string' &&
      Array.isArray(column.tasks)
    );
  }

  private toCardModel(task: Task): TaskCardModel {
    const currentUser = this.auth.currentUser();
    const isCurrentUserTask = currentUser?.id === task.userId;
    const assignee = isCurrentUserTask ? currentUser.name : `Utilizador #${task.userId}`;

    return {
      id: task.id,
      statusId: task.taskStatusId,
      title: task.title,
      description: task.description ?? 'Sem descrição.',
      priority: this.toPriorityLabel(task.priority),
      dueDate: this.formatDueDate(task.dueDate),
      source:
        task.sourceId === null
          ? 'Sem fonte'
          : (this.sources().find((source) => source.id === task.sourceId)?.name ??
            `Fonte #${task.sourceId}`),
      assignee,
      initials: isCurrentUserTask ? this.getInitials(assignee) : `U${task.userId}`,
      avatarUrl: this.profileImageObjectUrl,
      completed: task.completedAt !== null,
    };
  }

  private toPriorityLabel(priority: number): TaskPriorityLabel {
    if (priority <= 0) {
      return 'Baixa';
    }

    return priority === 1 ? 'Média' : 'Alta';
  }

  private formatDueDate(value: string | null): string {
    if (value === null) {
      return 'Sem prazo';
    }

    return new Intl.DateTimeFormat('pt-PT', { day: '2-digit', month: 'short' }).format(
      new Date(value),
    );
  }

  private loadProfileImage(): void {
    this.profileImageRequest?.unsubscribe();
    this.revokeProfileImageUrl();

    if (!this.auth.currentUser()?.profileImageUrl) {
      return;
    }

    this.profileImageRequest = this.auth
      .getProfileImage()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (image) => {
          this.profileImageObjectUrl = URL.createObjectURL(image);
          this.tasks.update((tasks) => [...tasks]);
        },
        error: () => {
          this.profileImageObjectUrl = null;
        },
      });
  }

  private normalizeStatusOrder(statusId: number, requestedOrder: number): Observable<unknown> {
    return this.taskStatusService.list().pipe(
      switchMap((statuses) => {
        const selectedStatus = statuses.find((status) => status.id === statusId);

        if (!selectedStatus) {
          return of(void 0);
        }

        const orderedStatuses = statuses
          .filter((status) => status.id !== statusId)
          .sort((first, second) => first.sortOrder - second.sortOrder || first.id - second.id);
        const targetIndex = Math.max(
          0,
          Math.min(Math.trunc(requestedOrder), orderedStatuses.length),
        );

        orderedStatuses.splice(targetIndex, 0, selectedStatus);

        const requests = orderedStatuses.map((status, index) =>
          status.sortOrder === index
            ? of(void 0)
            : this.taskStatusService.update(status.id, {
                name: status.name,
                color: status.color,
                sortOrder: index,
              }),
        );

        return requests.length ? forkJoin(requests) : of(void 0);
      }),
    );
  }

  private revokeProfileImageUrl(): void {
    if (this.profileImageObjectUrl) {
      URL.revokeObjectURL(this.profileImageObjectUrl);
      this.profileImageObjectUrl = null;
    }
  }

  private normalize(value: string): string {
    return value
      .toLocaleLowerCase('pt-PT')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  private getInitials(name: string): string {
    const initials = name
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');

    return initials || 'U';
  }

  private showSuccess(title: string, message: string): void {
    this.successModal.set({ title, message });
  }

  private getErrorMessage(error: HttpErrorResponse, fallback: string): string {
    return error.error?.detail ?? error.error?.message ?? error.error?.title ?? fallback;
  }
}
