import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, Subscription, switchMap } from 'rxjs';

import { Modal } from '../../../shared/ui/modal/modal.component';
import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { ProductTransactionFields } from '../components/product-transaction-fields/product-transaction-fields.component';
import {
  ProductCondition,
  ProductDetailsApi,
  ProductStatus,
} from '../models/product.model';
import { ProductService, SellProductRequest, UpdateProductRequest } from '../services/product.service';
import { ProductRelationOptions, ProductRelationsService } from '../services/product-relations.service';

interface ProductEditDraft {
  name: string;
  description: string;
  category: string;
  brand: string;
  size: string;
  color: string;
  condition: ProductCondition;
  status: ProductStatus;
  purchasePrice: number;
  listingPrice: number | null;
  minimumPrice: number | null;
  notes: string;
}

interface ProductSellDraft {
  sourceId: number | null;
  accountId: number | null;
  transactionTypeId: number | null;
  salePrice: number;
  saleOtherCosts: number | null;
  soldAt: string;
}

@Component({
  imports: [FormsModule, RouterLink, Dropdown, Modal, ProductTransactionFields],
  selector: 'app-product-detail',
  styleUrl: './product-detail.component.scss',
  templateUrl: './product-detail.component.html',
})
export class ProductDetail implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productService = inject(ProductService);
  private readonly productRelationsService = inject(ProductRelationsService);

  protected readonly product = signal<ProductDetailsApi | null>(null);
  protected readonly imageUrls = signal<Record<number, string>>({});
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly isDeleting = signal(false);
  protected readonly isDeleteConfirmOpen = signal(false);
  protected readonly isSelling = signal(false);
  protected readonly isSellFormOpen = signal(false);
  protected readonly isEditing = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly relationsLoading = signal(false);
  protected readonly relationOptions = signal<ProductRelationOptions>({
    sourceOptions: [],
    accountOptions: [],
    transactionTypeOptions: [],
  });

  protected editDraft: ProductEditDraft = this.emptyDraft();
  protected sellDraft = this.emptySellDraft();

  protected readonly conditions: readonly DropdownOption[] = [
    { value: 'new-with-tags', label: 'Novo com etiqueta' },
    { value: 'new-without-tags', label: 'Novo sem etiqueta' },
    { value: 'very-good', label: 'Muito bom' },
    { value: 'good', label: 'Bom' },
    { value: 'satisfactory', label: 'Satisfatório' },
  ];

  protected readonly statuses: readonly DropdownOption[] = [
    { value: 'draft', label: 'Rascunho' },
    { value: 'purchased', label: 'Comprado' },
    { value: 'active', label: 'Ativo' },
    { value: 'sold', label: 'Vendido' },
    { value: 'archived', label: 'Arquivado' },
  ];

  private productId = 0;
  private openSellOnLoad = false;
  private imageSubscription = new Subscription();

  ngOnInit(): void {
    this.productId = Number(this.route.snapshot.paramMap.get('id'));
    this.openSellOnLoad = this.route.snapshot.queryParamMap.get('action') === 'sell';

    if (!Number.isInteger(this.productId) || this.productId <= 0) {
      this.errorMessage.set('Produto inválido.');
      this.isLoading.set(false);
      return;
    }

    this.loadProduct();
    this.loadRelationOptions();
  }

  ngOnDestroy(): void {
    this.imageSubscription.unsubscribe();
    this.revokeImageUrls();
  }

  protected startEditing(): void {
    const product = this.product();
    if (!product || this.isSaving() || this.isDeleting() || this.isSelling()) return;

    this.editDraft = this.toEditDraft(product);
    this.errorMessage.set(null);
    this.isEditing.set(true);
  }

  protected cancelEditing(): void {
    if (!this.isSaving()) {
      this.isEditing.set(false);
      this.errorMessage.set(null);
    }
  }

  protected chooseCondition(value: string): void {
    if (this.conditions.some((option) => option.value === value)) {
      this.editDraft = { ...this.editDraft, condition: value as ProductCondition };
    }
  }

  protected chooseStatus(value: string): void {
    if (this.statuses.some((option) => option.value === value)) {
      this.editDraft = { ...this.editDraft, status: value as ProductStatus };
    }
  }

  protected saveChanges(): void {
    if (this.isSaving() || !this.editDraft.name.trim()) return;

    const request: UpdateProductRequest = {
      ...this.editDraft,
      name: this.editDraft.name.trim(),
      description: this.editDraft.description.trim(),
      category: this.editDraft.category.trim(),
      brand: this.editDraft.brand.trim(),
      size: this.editDraft.size.trim(),
      color: this.editDraft.color.trim(),
      notes: this.editDraft.notes.trim(),
    };

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.productService.update(this.productId, request)
      .pipe(
        switchMap(() => this.productService.getById(this.productId)),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: (product) => {
          this.applyProduct(product);
          this.isEditing.set(false);
        },
        error: () => this.errorMessage.set('Não foi possível guardar as alterações.'),
      });
  }

  protected openSellForm(): void {
    const product = this.product();
    if (!product || this.isSelling() || this.toStatus(product.status) === 'sold') {
      return;
    }

    const saleTransaction = this.relationOptions().transactionTypeOptions.find((option) => option.label.toLowerCase().includes('venda'))
      ?? this.relationOptions().transactionTypeOptions[0];

    this.sellDraft = {
      sourceId: this.relationOptions().sourceOptions[0] ? Number(this.relationOptions().sourceOptions[0].value) : null,
      accountId: this.relationOptions().accountOptions[0] ? Number(this.relationOptions().accountOptions[0].value) : null,
      transactionTypeId: saleTransaction ? Number(saleTransaction.value) : null,
      salePrice: product.listingPrice ?? product.purchasePrice,
      saleOtherCosts: null,
      soldAt: this.todayInputValue(),
    };
    this.errorMessage.set(null);
    this.isSellFormOpen.set(true);
  }

  protected chooseSaleSource(value: string): void {
    this.sellDraft = { ...this.sellDraft, sourceId: Number(value) };
  }

  protected chooseSaleAccount(value: string): void {
    this.sellDraft = { ...this.sellDraft, accountId: Number(value) };
  }

  protected cancelSell(): void {
    if (!this.isSelling()) {
      this.isSellFormOpen.set(false);
      this.errorMessage.set(null);
    }
  }

  protected confirmSale(): void {
    if (
      this.isSelling()
      || this.sellDraft.salePrice < 0
      || !this.sellDraft.soldAt
      || !this.sellDraft.sourceId
      || !this.sellDraft.accountId
      || !this.sellDraft.transactionTypeId
    ) {
      if (!this.sellDraft.sourceId || !this.sellDraft.accountId || !this.sellDraft.transactionTypeId) {
        this.errorMessage.set('Seleciona o canal e a conta da venda.');
      }
      return;
    }

    const request: SellProductRequest = {
      productName: this.product()?.name ?? 'Produto',
      sourceId: this.sellDraft.sourceId,
      accountId: this.sellDraft.accountId,
      transactionTypeId: this.sellDraft.transactionTypeId,
      salePrice: Number(this.sellDraft.salePrice) || 0,
      saleOtherCosts: this.sellDraft.saleOtherCosts === null || this.sellDraft.saleOtherCosts === undefined
        ? null
        : Number(this.sellDraft.saleOtherCosts),
      soldAt: new Date(`${this.sellDraft.soldAt}T12:00:00`).toISOString(),
    };

    this.isSelling.set(true);
    this.errorMessage.set(null);

    this.productService.sell(this.productId, request)
      .pipe(
        switchMap(() => this.productService.getById(this.productId)),
        finalize(() => this.isSelling.set(false)),
      )
      .subscribe({
        next: (product) => {
          this.applyProduct(product);
          this.isSellFormOpen.set(false);
        },
        error: () => this.errorMessage.set('Não foi possível registar a venda.'),
      });
  }

  protected openDeleteConfirmation(): void {
    if (this.isDeleting() || this.isSaving() || this.isSelling()) return;
    this.errorMessage.set(null);
    this.isDeleteConfirmOpen.set(true);
  }

  protected cancelDelete(): void {
    if (!this.isDeleting()) {
      this.isDeleteConfirmOpen.set(false);
      this.errorMessage.set(null);
    }
  }

  protected confirmDelete(): void {
    if (this.isDeleting() || this.isSaving() || this.isSelling()) return;

    this.isDeleting.set(true);
    this.errorMessage.set(null);

    this.productService.remove(this.productId)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl('/products'),
        error: () => this.errorMessage.set('Não foi possível apagar o produto.'),
      });
  }

  protected formatPrice(value: number | null): string {
    if (value === null || value === undefined) return '—';
    return new Intl.NumberFormat('pt-PT', {
      style: 'currency',
      currency: 'EUR',
      minimumFractionDigits: 2,
    }).format(value);
  }

  protected formatDate(value: string | null): string {
    if (!value) return '—';
    return new Intl.DateTimeFormat('pt-PT', { dateStyle: 'medium' }).format(new Date(value));
  }

  protected statusLabel(value: number | string): string {
    return this.statuses.find((option) => option.value === this.toStatus(value))?.label ?? 'Desconhecido';
  }

  protected conditionLabel(value: number | string): string {
    const normalized = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    const values: Record<string, ProductCondition> = {
      '1': 'new-with-tags', '2': 'new-without-tags', '3': 'very-good', '4': 'good', '5': 'satisfactory',
      newwithtags: 'new-with-tags', newwithouttags: 'new-without-tags', verygood: 'very-good', good: 'good', satisfactory: 'satisfactory',
    };
    return this.conditions.find((option) => option.value === values[normalized])?.label ?? '—';
  }

  protected sellingPrice(product: ProductDetailsApi): number | null {
    return this.toStatus(product.status) === 'sold'
      ? product.salePrice
      : product.listingPrice;
  }

  protected profit(product: ProductDetailsApi): number | null {
    const sellingPrice = this.sellingPrice(product);
    if (sellingPrice === null) return null;

    const acquisitionCost = product.purchasePrice + product.allocatedShippingCost + product.allocatedOtherCosts;
    const saleCosts = this.toStatus(product.status) === 'sold' ? (product.saleOtherCosts ?? 0) : 0;
    return sellingPrice - acquisitionCost - saleCosts;
  }

  private loadProduct(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.productService.getById(this.productId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (product) => {
          this.applyProduct(product);
          if (this.openSellOnLoad) {
            this.openSellOnLoad = false;
            this.openSellForm();
            void this.router.navigate([], {
              relativeTo: this.route,
              queryParams: { action: null },
              queryParamsHandling: 'merge',
              replaceUrl: true,
            });
          }
        },
        error: () => this.errorMessage.set('Não foi possível carregar o produto.'),
      });
  }

  private loadRelationOptions(): void {
    this.relationsLoading.set(true);
    this.productRelationsService.loadOptions()
      .pipe(finalize(() => this.relationsLoading.set(false)))
      .subscribe({
        next: (options) => this.relationOptions.set(options),
        error: () => this.errorMessage.set('Não foi possível carregar as fontes e contas.'),
      });
  }

  private applyProduct(product: ProductDetailsApi): void {
    this.product.set(product);
    this.editDraft = this.toEditDraft(product);
    this.loadImages(product);
  }

  private loadImages(product: ProductDetailsApi): void {
    this.imageSubscription.unsubscribe();
    this.imageSubscription = new Subscription();
    this.revokeImageUrls();

    const urls: Record<number, string> = {};
    this.imageUrls.set(urls);

    for (const image of product.images) {
      this.imageSubscription.add(this.productService.getImage(product.id, image.id).subscribe({
        next: (blob) => {
          urls[image.id] = URL.createObjectURL(blob);
          this.imageUrls.set({ ...urls });
        },
      }));
    }
  }

  private revokeImageUrls(): void {
    for (const url of Object.values(this.imageUrls())) {
      URL.revokeObjectURL(url);
    }
    this.imageUrls.set({});
  }

  private toEditDraft(product: ProductDetailsApi): ProductEditDraft {
    return {
      name: product.name,
      description: product.description ?? '',
      category: product.category ?? '',
      brand: product.brand ?? '',
      size: product.size ?? '',
      color: product.color ?? '',
      condition: this.toCondition(product.condition),
      status: this.toStatus(product.status),
      purchasePrice: product.purchasePrice,
      listingPrice: product.listingPrice,
      minimumPrice: product.minimumPrice,
      notes: product.notes ?? '',
    };
  }

  private toCondition(value: number | string): ProductCondition {
    const normalized = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    const values: Record<string, ProductCondition> = {
      '1': 'new-with-tags', '2': 'new-without-tags', '3': 'very-good', '4': 'good', '5': 'satisfactory',
      newwithtags: 'new-with-tags', newwithouttags: 'new-without-tags', verygood: 'very-good', good: 'good', satisfactory: 'satisfactory',
    };
    return values[normalized] ?? 'good';
  }

  protected toStatus(value: number | string): ProductStatus {
    const normalized = String(value).replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    const values: Record<string, ProductStatus> = {
      '1': 'draft', '2': 'purchased', '3': 'active', '4': 'sold', '5': 'archived',
      draft: 'draft', purchased: 'purchased', active: 'active', sold: 'sold', archived: 'archived',
    };
    return values[normalized] ?? 'draft';
  }

  private emptyDraft(): ProductEditDraft {
    return {
      name: '', description: '', category: '', brand: '', size: '', color: '',
      condition: 'good', status: 'active', purchasePrice: 0,
      listingPrice: null, minimumPrice: null, notes: '',
    };
  }

  private emptySellDraft(): ProductSellDraft {
    return {
      sourceId: null,
      accountId: null,
      transactionTypeId: null,
      salePrice: 0,
      saleOtherCosts: null,
      soldAt: this.todayInputValue(),
    };
  }

  private todayInputValue(): string {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }
}
