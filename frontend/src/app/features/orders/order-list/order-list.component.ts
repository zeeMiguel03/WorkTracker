import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, Observable, Subject } from 'rxjs';

import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { AuthenticatedImage } from '../../../shared/ui/authenticated-image/authenticated-image.component';
import { FormStepper } from '../../../shared/ui/form-stepper/form-stepper.component';
import { Modal } from '../../../shared/ui/modal/modal.component';
import { OrderDraft, OrderProductDraft, OrderSourceOption, OrderStatus, OrderView } from '../models/order.model';
import { OrderService } from '../services/order.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { LanguageService } from '../../../core/i18n/language.service';

type OrderSort = 'recent' | 'highest' | 'lowest';

@Component({
  imports: [RouterLink, Modal, Dropdown, AuthenticatedImage, FormStepper, TranslatePipe],
  selector: 'app-order-list',
  styleUrl: './order-list.component.scss',
  templateUrl: './order-list.component.html',
})
export class OrderList {
  private readonly orderService = inject(OrderService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly language = inject(LanguageService);
  private readonly searchChanges = new Subject<string>();
  private requestId = 0;

  protected readonly createPanelOpen = signal(false);
  protected readonly activeFormStep = signal(0);
  protected readonly activeProductIndex = signal(0);
  protected readonly filtersOpen = signal(false);
  protected readonly expandedOrderId = signal<number | null>(null);
  protected readonly editingOrder = signal<OrderView | null>(null);
  protected readonly selectedStatus = signal<OrderStatus | 'all'>('all');
  protected readonly selectedSource = signal('all');
  protected readonly sortOrder = signal<OrderSort>('recent');
  protected readonly searchTerm = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly totalItems = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly orders = signal<readonly OrderView[]>([]);
  protected readonly sources = signal<readonly OrderSourceOption[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isDeleting = signal(false);
  protected readonly orderPendingDelete = signal<OrderView | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly draft = signal<OrderDraft>(this.emptyDraft());

  protected readonly statusOptions: readonly { value: OrderStatus | 'all'; label: string }[] = [
    { value: 'all', label: 'Todas' }, { value: 'ordered', label: 'Encomendadas' },
    { value: 'transit', label: 'Em trânsito' }, { value: 'partial', label: 'Parciais' }, { value: 'received', label: 'Recebidas' },
  ];
  protected readonly formSteps = [
    { id: 0, label: 'Encomenda', description: 'Origem e envio' },
    { id: 1, label: 'Produtos', description: 'Artigos e características' },
    { id: 2, label: 'Custos', description: 'Valores e confirmação' },
  ] as const;
  protected readonly conditionOptions: readonly DropdownOption[] = [
    { value: '1', label: 'Novo com etiqueta' },
    { value: '2', label: 'Novo sem etiqueta' },
    { value: '3', label: 'Muito bom' },
    { value: '4', label: 'Bom' },
    { value: '5', label: 'Satisfatório' },
  ];
  protected readonly orderStatusDropdownOptions: readonly DropdownOption[] = [
    { value: 'draft', label: 'Rascunho' },
    { value: 'ordered', label: 'Encomendada' },
    { value: 'transit', label: 'Em trânsito' },
    { value: 'partial', label: 'Receção parcial' },
    { value: 'received', label: 'Recebida' },
    { value: 'cancelled', label: 'Cancelada' },
  ];
  protected readonly sortOptions: readonly DropdownOption[] = [
    { value: 'recent', label: 'Mais recentes' },
    { value: 'highest', label: 'Maior valor' },
    { value: 'lowest', label: 'Menor valor' },
  ];
  protected readonly sourceDropdownOptions = computed<readonly DropdownOption[]>(() => [
    { value: '', label: 'Selecionar origem' },
    ...this.sources().map((source) => ({ value: String(source.id), label: source.name })),
  ]);
  protected readonly sourceFilterOptions = computed<readonly DropdownOption[]>(() => [
    { value: 'all', label: 'Todas as origens' },
    ...this.sources().map((source) => ({ value: String(source.id), label: source.name })),
  ]);

  protected readonly visibleOrders = computed(() => {
    const source = this.selectedSource();
    const filtered = source === 'all' ? this.orders() : this.orders().filter((order) => String(order.sourceId) === source);
    return [...filtered].sort((left, right) => {
      if (this.sortOrder() === 'highest') return this.orderTotal(right) - this.orderTotal(left);
      if (this.sortOrder() === 'lowest') return this.orderTotal(left) - this.orderTotal(right);
      return (right.orderedAt ?? right.createdAt).localeCompare(left.orderedAt ?? left.createdAt);
    });
  });
  protected readonly pageNumbers = computed(() => Array.from({ length: this.totalPages() }, (_, index) => index + 1).slice(0, 7));
  protected readonly activeOrders = computed(() => this.orders().filter((order) => !['received', 'cancelled'].includes(order.status)).length);
  protected readonly transitOrders = computed(() => this.orders().filter((order) => order.status === 'transit').length);
  protected readonly receivedOrders = computed(() => this.orders().filter((order) => order.status === 'received').length);
  protected readonly totalUnits = computed(() => this.orders().reduce((total, order) => total + this.productUnits(order), 0));
  protected readonly totalInvestment = computed(() => this.orders().reduce((total, order) => total + this.orderTotal(order), 0));
  protected readonly draftSubtotal = computed(() => this.draft().products.reduce((total, product) => total + product.quantity * product.unitPrice, 0));
  protected readonly draftTotal = computed(() => this.draftSubtotal() + this.draft().shippingCost + this.draft().otherCosts);
  protected readonly draftUnits = computed(() => this.draft().products.reduce((total, product) => total + product.quantity, 0));
  protected readonly canSave = computed(() => this.draft().sourceId !== null && this.draft().orderedDate.length > 0
    && this.draft().products.length > 0 && this.draft().products.every((product) => product.name.trim() && product.quantity > 0 && product.unitPrice >= 0));

  constructor() {
    this.searchChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe((search) => {
      this.searchTerm.set(search);
      this.loadOrders(1);
    });
    this.loadSources();
    this.loadOrders();
  }

  protected selectStatus(status: OrderStatus | 'all'): void { this.selectedStatus.set(status); this.loadOrders(1); }
  protected updateSearch(event: Event): void { this.searchChanges.next((event.target as HTMLInputElement).value); }
  protected updateSourceFilter(value: string): void { this.selectedSource.set(value); }
  protected updateSort(value: string): void { this.sortOrder.set(value as OrderSort); }
  protected resetFilters(): void { this.selectedSource.set('all'); this.sortOrder.set('recent'); this.selectedStatus.set('all'); this.searchTerm.set(''); this.loadOrders(1); }
  protected goToPage(page: number): void { if (page < 1 || page > this.totalPages() || page === this.page()) return; this.expandedOrderId.set(null); this.loadOrders(page); }
  protected toggleOrder(orderId: number): void { this.expandedOrderId.update((current) => current === orderId ? null : orderId); }

  protected openCreateOrder(): void { this.editingOrder.set(null); this.draft.set(this.emptyDraft()); this.activeFormStep.set(0); this.activeProductIndex.set(0); this.errorMessage.set(null); this.createPanelOpen.set(true); }
  protected openEditOrder(order: OrderView, addProduct = false): void {
    this.editingOrder.set(order);
    this.draft.set({
      sourceId: order.sourceId,
      orderedDate: this.toDateInput(order.orderedAt ?? order.createdAt),
      trackingNumber: order.trackingNumber === 'Sem rastreio' ? '' : order.trackingNumber,
      shippingCost: order.shippingCost,
      otherCosts: order.otherCosts,
      notes: order.notes,
      products: [...order.products.map((product) => ({ ...product })), ...(addProduct ? [this.emptyProduct()] : [])],
    });
    this.activeFormStep.set(addProduct ? 1 : 0);
    this.activeProductIndex.set(addProduct ? Math.max(0, order.products.length - 1) : 0);
    this.errorMessage.set(null);
    this.createPanelOpen.set(true);
  }
  protected closeOrderModal(): void {
    if (this.isSaving()) return;
    this.revokeDraftImagePreviews();
    this.createPanelOpen.set(false);
    this.editingOrder.set(null);
    this.activeFormStep.set(0);
  }
  protected selectFormStep(step: number): void { if (!this.isSaving() && step >= 0 && step < this.formSteps.length) this.activeFormStep.set(step); }
  protected nextFormStep(): void {
    if (this.activeFormStep() === 0 && (this.draft().sourceId === null || !this.draft().orderedDate)) {
      this.errorMessage.set('Seleciona a origem e a data da encomenda.');
      return;
    }
    if (this.activeFormStep() === 1 && (!this.draft().products.length || this.draft().products.some((product) => !product.name.trim()))) {
      this.errorMessage.set('Adiciona pelo menos um produto e indica o respetivo nome.');
      return;
    }
    this.errorMessage.set(null);
    this.activeFormStep.update((step) => Math.min(step + 1, this.formSteps.length - 1));
  }
  protected previousFormStep(): void { this.errorMessage.set(null); this.activeFormStep.update((step) => Math.max(0, step - 1)); }
  protected addDraftProduct(): void {
    this.draft.update((draft) => ({ ...draft, products: [...draft.products, this.emptyProduct()] }));
    this.activeProductIndex.set(this.draft().products.length - 1);
  }
  protected toggleDraftProduct(index: number): void { this.activeProductIndex.update((current) => current === index ? -1 : index); }
  protected removeDraftProduct(index: number): void {
    const product = this.draft().products[index];
    if (product?.imagePreviewUrl) URL.revokeObjectURL(product.imagePreviewUrl);
    this.draft.update((draft) => ({ ...draft, products: draft.products.filter((_, productIndex) => productIndex !== index) }));
    this.activeProductIndex.update((current) => current > index ? current - 1 : current === index ? Math.max(0, index - 1) : current);
  }

  protected updateDraftField(field: Exclude<keyof OrderDraft, 'products'>, event: Event): void {
    const element = event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
    const value = field === 'sourceId' ? (element.value ? Number(element.value) : null)
      : field === 'shippingCost' || field === 'otherCosts' ? Math.max(0, Number(element.value) || 0) : element.value;
    this.draft.update((draft) => ({ ...draft, [field]: value }));
  }

  protected chooseDraftSource(value: string): void {
    this.draft.update((draft) => ({ ...draft, sourceId: value ? Number(value) : null }));
    this.errorMessage.set(null);
  }

  protected updateDraftProduct(index: number, field: keyof OrderProductDraft, event: Event): void {
    const input = event.target as HTMLInputElement;
    const numericFields: readonly (keyof OrderProductDraft)[] = ['quantity', 'unitPrice', 'listingPrice', 'minimumPrice'];
    let value: string | number | null = input.value;
    if (field === 'quantity') value = Math.max(1, Math.floor(Number(input.value) || 1));
    else if (field === 'unitPrice') value = Math.max(0, Number(input.value) || 0);
    else if (numericFields.includes(field)) value = input.value === '' ? null : Math.max(0, Number(input.value) || 0);
    this.draft.update((draft) => ({ ...draft, products: draft.products.map((product, productIndex) => productIndex === index ? { ...product, [field]: value } : product) }));
  }

  protected chooseProductOption(index: number, field: 'condition', value: string): void {
    this.draft.update((draft) => ({
      ...draft,
      products: draft.products.map((product, productIndex) => productIndex === index ? { ...product, [field]: Number(value) } : product),
    }));
  }

  protected selectProductImage(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      this.errorMessage.set('A imagem deve ser JPG, PNG ou WebP.');
      return;
    }
    const previewUrl = URL.createObjectURL(file);
    this.draft.update((draft) => ({
      ...draft,
      products: draft.products.map((product, productIndex) => {
        if (productIndex !== index) return product;
        if (product.imagePreviewUrl) URL.revokeObjectURL(product.imagePreviewUrl);
        return { ...product, image: file, imagePreviewUrl: previewUrl };
      }),
    }));
    this.errorMessage.set(null);
  }

  protected removeProductImage(index: number): void {
    this.draft.update((draft) => ({
      ...draft,
      products: draft.products.map((product, productIndex) => {
        if (productIndex !== index) return product;
        if (product.imagePreviewUrl) URL.revokeObjectURL(product.imagePreviewUrl);
        return { ...product, image: null, imagePreviewUrl: null };
      }),
    }));
  }

  protected saveOrder(): void {
    if (!this.canSave() || this.isSaving()) return;
    this.isSaving.set(true);
    this.errorMessage.set(null);
    const current = this.editingOrder();
    const request: Observable<number | void> = current
      ? this.orderService.update(current, this.draft())
      : this.orderService.create(this.draft());
    request.pipe(finalize(() => this.isSaving.set(false))).subscribe({
      next: () => { this.isSaving.set(false); this.closeOrderModal(); this.loadOrders(current ? this.page() : 1); },
      error: (error: { error?: { detail?: string; title?: string } }) => this.errorMessage.set(error.error?.detail ?? error.error?.title ?? 'Não foi possível guardar a encomenda.'),
    });
  }

  protected changeStatus(orderId: number, value: string): void {
    const status = value as OrderStatus;
    this.orderService.changeStatus(orderId, status).subscribe({ next: () => this.loadOrders(this.page()), error: () => this.errorMessage.set('Não foi possível atualizar o estado.') });
  }

  protected requestDeleteOrder(order: OrderView): void { this.orderPendingDelete.set(order); }
  protected cancelDeleteOrder(): void { if (!this.isDeleting()) this.orderPendingDelete.set(null); }
  protected confirmDeleteOrder(): void {
    const order = this.orderPendingDelete();
    if (!order || this.isDeleting()) return;
    this.isDeleting.set(true);
    this.orderService.remove(order).pipe(finalize(() => this.isDeleting.set(false))).subscribe({
      next: () => { this.orderPendingDelete.set(null); this.expandedOrderId.set(null); this.loadOrders(this.page()); },
      error: () => this.errorMessage.set('Não foi possível apagar a encomenda.'),
    });
  }

  protected loadOrders(page = this.page()): void {
    const requestId = ++this.requestId;
    this.isLoading.set(true);
    this.errorMessage.set(null);
    const selected = this.selectedStatus();
    this.orderService.list(page, this.pageSize, selected === 'all' ? null : selected, this.searchTerm()).pipe(
      finalize(() => { if (requestId === this.requestId) this.isLoading.set(false); }),
    ).subscribe({
      next: (response) => {
        if (requestId !== this.requestId) return;
        this.orders.set(response.items); this.page.set(response.page); this.totalItems.set(response.totalItems); this.totalPages.set(response.totalPages);
      },
      error: () => { if (requestId === this.requestId) this.errorMessage.set('Não foi possível carregar as encomendas.'); },
    });
  }

  protected orderSubtotal(order: OrderView): number { return order.products.reduce((total, product) => total + product.quantity * product.unitPrice, 0); }
  protected orderTotal(order: OrderView): number { return this.orderSubtotal(order) + order.shippingCost + order.otherCosts; }
  protected productUnits(order: OrderView): number { return order.products.reduce((total, product) => total + product.quantity, 0); }
  protected statusLabel(status: OrderStatus): string { return ({ draft: 'Rascunho', ordered: 'Encomendada', transit: 'Em trânsito', partial: 'Receção parcial', received: 'Recebida', cancelled: 'Cancelada' } as const)[status]; }
  protected formatCurrency(value: number): string { return new Intl.NumberFormat(this.language.locale(), { style: 'currency', currency: 'EUR' }).format(value); }
  protected formatDate(value: string | null): string { return value ? new Intl.DateTimeFormat(this.language.locale(), { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(value)).replace('.', '') : this.language.translate('Sem data'); }
  protected dropdownValue(value: number | null): string { return value === null ? '' : `${value}`; }
  protected formatIndex(index: number): string { return String(index + 1).padStart(2, '0'); }
  protected orderReference(id: number): string { return `ENC-${String(id).padStart(4, '0')}`; }
  protected productInitials(name: string): string { const words = name.trim().split(/\s+/); return (words.length > 1 ? words[0][0] + words[1][0] : name.slice(0, 2) || 'PR').toUpperCase(); }

  private loadSources(): void { this.orderService.listSources().subscribe({ next: (sources) => this.sources.set(sources), error: () => this.errorMessage.set('Não foi possível carregar as origens.') }); }
  private emptyDraft(): OrderDraft { return { sourceId: null, orderedDate: new Date().toISOString().slice(0, 10), trackingNumber: '', shippingCost: 0, otherCosts: 0, notes: '', products: [this.emptyProduct()] }; }
  private emptyProduct(): OrderProductDraft { return { ids: [], coverImageIds: [], existingImageUrl: null, image: null, imagePreviewUrl: null, name: '', description: '', quantity: 1, unitPrice: 0, condition: 3, status: 2, category: '', brand: '', size: '', color: '', listingPrice: null, minimumPrice: null, notes: '' }; }
  private toDateInput(value: string): string { const date = new Date(value); return Number.isNaN(date.getTime()) ? new Date().toISOString().slice(0, 10) : date.toISOString().slice(0, 10); }
  private revokeDraftImagePreviews(): void {
    this.draft().products.forEach((product) => {
      if (product.imagePreviewUrl) URL.revokeObjectURL(product.imagePreviewUrl);
    });
  }
}
