import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, Subject } from 'rxjs';

import { Modal } from '../../../shared/ui/modal/modal.component';
import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { ProductCard } from '../components/product-card/product-card.component';
import { ProductForm } from '../product-form/product-form.component';
import { ProductDraft, ProductListItem, ProductListItemApi, ProductStatus } from '../models/product.model';
import { CreateProductRequest, ProductService, SellProductRequest } from '../services/product.service';
import { ProductRelationsService } from '../services/product-relations.service';
import { portugalSaleDateInstant } from '../../../shared/util/portugal-calendar';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

type ProductFilter = 'all' | ProductStatus;

@Component({
  imports: [ProductCard, ProductForm, Modal, RouterLink, FormsModule, Dropdown, TranslatePipe],
  selector: 'app-product-list',
  styleUrl: './product-list.component.scss',
  templateUrl: './product-list.component.html',
})
export class ProductList {
  private readonly productService = inject(ProductService);
  private readonly productRelationsService = inject(ProductRelationsService);
  private readonly router = inject(Router);
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
  protected readonly deleteProductId = signal<number | null>(null);
  protected readonly saleProduct = signal<ProductListItem | null>(null);
  protected readonly saleOptions = signal<readonly DropdownOption[]>([]);
  protected readonly isLoadingSaleOptions = signal(false);
  protected readonly isSelling = signal(false);
  protected readonly saleError = signal<string | null>(null);
  protected saleDraft = { sourceId: null as number | null, salePrice: 0, saleOtherCosts: null as number | null, soldAt: this.todayInputValue() };
  protected readonly isDeleting = signal(false);
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

  protected openProduct(productId: number): void {
    void this.router.navigate(['/products', productId]);
  }

  protected openProductForSale(productId: number): void {
    const product = this.products().find((item) => item.id === productId);
    if (!product || this.isSelling() || product.status === 'sold') return;

    this.saleProduct.set(product);
    this.saleDraft = {
      sourceId: null,
      salePrice: product.price,
      saleOtherCosts: null,
      soldAt: this.todayInputValue(),
    };
    this.saleError.set(null);
    this.isLoadingSaleOptions.set(true);
    this.productRelationsService.loadOptions()
      .pipe(finalize(() => this.isLoadingSaleOptions.set(false)))
      .subscribe({
        next: (options) => {
          this.saleOptions.set(options.sourceOptions);
          this.saleDraft = { ...this.saleDraft, sourceId: options.sourceOptions[0] ? Number(options.sourceOptions[0].value) : null };
        },
        error: () => this.saleError.set('Não foi possível carregar os canais de venda.'),
      });
  }

  protected closeSaleModal(): void {
    if (!this.isSelling()) {
      this.saleProduct.set(null);
      this.saleError.set(null);
    }
  }

  protected chooseSaleSource(value: string): void {
    this.saleDraft = { ...this.saleDraft, sourceId: value ? Number(value) : null };
  }

  protected confirmSale(): void {
    const product = this.saleProduct();
    if (!product || this.isSelling() || this.saleDraft.salePrice < 0 || !this.saleDraft.soldAt) {
      if (!this.saleDraft.soldAt) this.saleError.set('Indica a data da venda.');
      return;
    }

    const request: SellProductRequest = {
      sourceId: this.saleDraft.sourceId,
      salePrice: Number(this.saleDraft.salePrice) || 0,
      saleOtherCosts: this.saleDraft.saleOtherCosts === null ? null : Number(this.saleDraft.saleOtherCosts),
      soldAt: portugalSaleDateInstant(this.saleDraft.soldAt),
    };
    this.isSelling.set(true);
    this.saleError.set(null);
    this.productService.sell(product.id, request)
      .pipe(finalize(() => this.isSelling.set(false)))
      .subscribe({
        next: () => {
          this.saleProduct.set(null);
          this.loadProducts();
        },
        error: () => this.saleError.set('Não foi possível registar a venda.'),
      });
  }

  protected openDeleteConfirmation(productId: number): void {
    if (this.isDeleting() || this.isSaving()) return;
    this.errorMessage.set(null);
    this.deleteProductId.set(productId);
  }

  protected cancelDelete(): void {
    if (!this.isDeleting()) {
      this.deleteProductId.set(null);
      this.errorMessage.set(null);
    }
  }

  protected confirmDelete(): void {
    const productId = this.deleteProductId();
    if (!productId || this.isDeleting()) return;

    this.isDeleting.set(true);
    this.errorMessage.set(null);

    this.productService.remove(productId)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: () => {
          this.deleteProductId.set(null);
          this.loadProducts();
        },
        error: () => this.errorMessage.set('Não foi possível apagar o produto.'),
      });
  }

  protected closeProductModal(): void {
    if (!this.isSaving()) {
      this.productModalOpen.set(false);
    }
  }

  protected saveProduct(draft: ProductDraft): void {
    if (this.isSaving()) {
      return;
    }

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
        error: (error: { error?: { detail?: string; title?: string } }) => {
          this.errorMessage.set(
            error.error?.detail
              ?? error.error?.title
              ?? 'Não foi possível adicionar o produto.',
          );
        },
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
      salePrice: product.salePrice === null || product.salePrice === undefined ? null : Number(product.salePrice),
      realizedProfit: product.realizedProfit === null || product.realizedProfit === undefined
        ? null
        : Number(product.realizedProfit),
      soldAt: product.soldAt,
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

  private todayInputValue(): string {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }
}
