import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, Subject } from 'rxjs';

import { Modal } from '../../../shared/ui/modal/modal.component';
import { SuccessModal } from '../../../shared/ui/success-modal/success-modal.component';
import { SourceCard } from '../components/source-card/source-card.component';
import { SourceFilters } from '../components/source-filters/source-filters.component';
import { SourceForm } from '../components/source-form/source-form.component';
import { SourceCardModel } from '../models/source-card.model';
import {
  CreateSourceRequest,
  Source,
  UpdateSourceRequest,
} from '../models/source.model';
import { SourceService } from '../services/source.service';

@Component({
  imports: [Modal, RouterLink, SourceCard, SourceFilters, SourceForm, SuccessModal],
  selector: 'app-source-list',
  styleUrl: './source-list.component.scss',
  templateUrl: './source-list.component.html',
})
export class SourceList {
  private readonly sourceService = inject(SourceService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanges = new Subject<string>();

  protected readonly sourceModalOpen = signal(false);
  protected readonly deleteModalOpen = signal(false);
  protected readonly selectedSource = signal<SourceCardModel | null>(null);
  protected readonly sources = signal<SourceCardModel[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successModal = signal<{ title: string; message: string } | null>(null);
  protected readonly searchTerm = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = 8;
  protected readonly totalItems = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly visibleSources = computed(() => this.sources());
  protected readonly pageNumbers = computed(() => {
    const totalPages = this.totalPages();
    const currentPage = this.page();
    const maxButtons = 5;

    if (totalPages <= maxButtons) {
      return Array.from({ length: totalPages }, (_, index) => index + 1);
    }

    const start = Math.max(
      1,
      Math.min(currentPage - 2, totalPages - maxButtons + 1),
    );

    return Array.from({ length: maxButtons }, (_, index) => start + index);
  });
  protected readonly mobilePageNumbers = computed(() => {
    const totalPages = this.totalPages();
    const currentPage = this.page();
    const visiblePages = Math.min(totalPages, 3);
    const start = Math.max(1, Math.min(currentPage - 1, totalPages - visiblePages + 1));

    return Array.from({ length: visiblePages }, (_, index) => start + index);
  });

  protected readonly hasSearch = computed(() => this.searchTerm().trim().length > 0);

  constructor() {
    this.searchChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((search) => {
        this.searchTerm.set(search);
        this.page.set(1);
        this.loadSources(1);
      });

    this.loadSources(1);
  }

  protected updateSearch(search: string): void {
    this.searchChanges.next(search);
  }

  protected loadSources(page = this.page()): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.sourceService.list(page, this.pageSize, this.searchTerm()).subscribe({
      next: (response) => {
        if (response.totalPages > 0 && page > response.totalPages) {
          this.loadSources(response.totalPages);
          return;
        }

        this.sources.set(response.items.map((source) => this.toCardModel(source)));
        this.page.set(response.page);
        this.totalItems.set(response.totalItems);
        this.totalPages.set(response.totalPages);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Não foi possível carregar as fontes.');
        this.isLoading.set(false);
      },
    });
  }

  protected clearSearch(): void {
    this.updateSearch('');
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.loadSources(page);
  }

  protected openCreateSource(): void {
    this.selectedSource.set(null);
    this.errorMessage.set(null);
    this.sourceModalOpen.set(true);
  }

  protected openEditSource(sourceId: number): void {
    this.selectedSource.set(this.sources().find((source) => source.id === sourceId) ?? null);
    this.errorMessage.set(null);
    this.sourceModalOpen.set(true);
  }

  protected openDeleteSource(sourceId: number): void {
    this.selectedSource.set(this.sources().find((source) => source.id === sourceId) ?? null);
    this.errorMessage.set(null);
    this.deleteModalOpen.set(true);
  }

  protected saveSource(
    data: CreateSourceRequest | UpdateSourceRequest,
  ): void {
    const selectedSource = this.selectedSource();

    if (selectedSource) {
      if (!('isActive' in data)) {
        return;
      }

      this.isSaving.set(true);

      this.sourceService
        .update(selectedSource.id, data)
        .pipe(finalize(() => this.isSaving.set(false)))
        .subscribe({
          next: () => this.finishMutation(
            'Fonte atualizada!',
            'A fonte foi atualizada com sucesso.',
          ),
          error: () => this.errorMessage.set('Não foi possível atualizar a fonte.'),
        });

      return;
    }

    if ('isActive' in data) {
      return;
    }

    this.isSaving.set(true);

    this.sourceService
      .create(data)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => this.finishMutation(
          'Fonte criada!',
          'A fonte foi criada com sucesso.',
        ),
        error: () => this.errorMessage.set('Não foi possível criar a fonte.'),
      });
  }

  protected deleteSelectedSource(): void {
    const selectedSource = this.selectedSource();

    if (!selectedSource) {
      return;
    }

    this.isSaving.set(true);

    this.sourceService
      .remove(selectedSource.id)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => this.finishMutation(
          'Fonte apagada!',
          'A fonte foi removida com sucesso.',
        ),
        error: () => this.errorMessage.set('Não foi possível apagar a fonte.'),
      });
  }

  protected closeSourceModal(): void {
    this.sourceModalOpen.set(false);
  }

  protected closeDeleteModal(): void {
    this.deleteModalOpen.set(false);
  }

  protected closeSuccessModal(): void {
    this.successModal.set(null);
  }

  private finishMutation(title: string, message: string): void {
    this.sourceModalOpen.set(false);
    this.deleteModalOpen.set(false);
    this.selectedSource.set(null);
    this.loadSources();
    this.successModal.set({ title, message });
  }

  private toCardModel(source: Source): SourceCardModel {
    return {
      id: source.id,
      name: source.name,
      createdAt: source.createdAt,
      imageUrl: source.imageUrl,
      websiteUrl: source.link ?? null,
      initials: this.getInitials(source.name),
      color: this.getSourceColor(source.id),
      transactions: 0,
      isActive: source.isActive,
    };
  }

  private getInitials(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((word) => word.charAt(0))
      .join('')
      .toUpperCase() || '??';
  }

  private getSourceColor(id: number): string {
    const colors = ['#7c3aed', '#f04438', '#f79009', '#475467', '#12b76a'];
    return colors[id % colors.length];
  }
}
