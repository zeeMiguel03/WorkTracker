import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, Subject } from 'rxjs';

import { Modal } from '../../../shared/ui/modal/modal.component';
import { ProductCard } from '../components/product-card/product-card.component';
import { ProductForm } from '../product-form/product-form.component';
import { ProductDraft, ProductListItem, ProductListItemApi, ProductStatus } from '../models/product.model';
import { CreateProductRequest, ProductService } from '../services/product.service';

type ProductFilter = 'all' | ProductStatus;

@Component({
  imports: [ProductCard, ProductForm, Modal, RouterLink],
  selector: 'app-product-list',
  styleUrl: './product-list.component.scss',
  templateUrl: './product-list.component.html',
})
export class ProductList {
  private readonly productService = inject(ProductService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanges = new Subject<string>();
  private requestId = 0;

  protected readonly products = signal<readonly ProductListItem[]>([]);
  protected readonly selectedStatus = signal<ProductFilter>('all');
  protected readonly searchTerm = signal('');
  protected readonly productModalOpen = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly totalItems = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly pageSize = 20;

  protected readonly statusFilters: readonly { value: ProductStatus; label: string }[] = [
    { value: 'active', label: 'Ativos' },
    { value: 'purchased', label: 'Comprados' },
    { value: 'sold', label: 'Vendidos' },
    { value: 'draft', label: 'Rascunho' },
  ];

  protected readonly pageNumbers = computed(() =>
    Array.from({ length: this.totalPages() }, (_, index) => index + 1).slice(0, 5),
  );

  constructor() {
    this.searchChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.searchTerm.set(search);
        this.loadProducts(1);
      });

    this.loadProducts();
  }

  protected selectStatus(status: ProductStatus): void {
    this.selectedStatus.update((current) => current === status ? 'all' : status);
    this.loadProducts(1);
  }

  protected updateSearch(event: Event): void {
    this.searchChanges.next((event.target as HTMLInputElement).value);
  }

  protected openCreateProduct(): void {
    this.errorMessage.set(null);
    this.productModalOpen.set(true);
  }

  protected closeProductModal(): void {
    if (!this.isSaving()) {
      this.productModalOpen.set(false);
    }
  }

  protected saveProduct(draft: ProductDraft): void {
    this.isSaving.set(true);
    this.errorMessage.set(null);

    const request: CreateProductRequest = { ...draft };

    this.productService.create(request)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: () => {
          this.productModalOpen.set(false);
          this.loadProducts(1);
        },
        error: () => this.errorMessage.set('Não foi possível adicionar o produto.'),
      });
  }

  protected loadProducts(page = this.page()): void {
    const requestId = ++this.requestId;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const selectedStatus = this.selectedStatus();
    const status: ProductStatus | null = selectedStatus === 'all' ? null : selectedStatus;

    this.productService.list(page, this.pageSize, status, this.searchTerm())
      .pipe(finalize(() => {
        if (requestId === this.requestId) this.isLoading.set(false);
      }))
      .subscribe({
        next: (response) => {
          if (requestId !== this.requestId) return;

          this.products.set(response.items.map((item) => this.toCardModel(item)));
          this.page.set(response.page);
          this.totalItems.set(response.totalItems);
          this.totalPages.set(response.totalPages);
        },
        error: () => {
          if (requestId === this.requestId) {
            this.errorMessage.set('Não foi possível carregar os produtos.');
          }
        },
      });
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.loadProducts(page);
  }

  private toCardModel(product: ProductListItemApi): ProductListItem {
    return {
      id: product.id,
      name: product.name,
      category: product.category || 'Sem categoria',
      price: Number(product.listingPrice) || 0,
      status: this.toStatus(product.status),
      imageId: product.coverImageId,
      imageUrl: product.coverImageUrl,
    };
  }

  private toStatus(value: number | string): ProductStatus {
    const normalized = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    const values: Record<string, ProductStatus> = {
      '1': 'draft', '2': 'purchased', '3': 'active', '4': 'sold', '5': 'archived',
      draft: 'draft', purchased: 'purchased', active: 'active', sold: 'sold', archived: 'archived',
    };
    return values[normalized] ?? 'draft';
  }
}
