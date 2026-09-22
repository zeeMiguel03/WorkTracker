import { Component, effect, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { ProductTransactionFields } from '../components/product-transaction-fields/product-transaction-fields.component';
import { ProductCondition, ProductDraft, ProductStatus } from '../models/product.model';

@Component({
  imports: [FormsModule, Dropdown, ProductTransactionFields],
  selector: 'app-product-form',
  styleUrl: './product-form.component.scss',
  templateUrl: './product-form.component.html',
})
export class ProductForm {
  private static readonly maxImages = 5;

  protected readonly steps = [
    { id: 0, label: 'Dados básicos', description: 'Imagem e identificação' },
    { id: 1, label: 'Características', description: 'Detalhes do produto' },
    { id: 2, label: 'Compra e preço', description: 'Custos e valores' },
  ] as const;

  readonly submitted = output<ProductDraft>();
  readonly cancelled = output<void>();
  readonly saving = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly sourceOptions = input<readonly DropdownOption[]>([]);
  readonly accountOptions = input<readonly DropdownOption[]>([]);
  readonly transactionTypeOptions = input<readonly DropdownOption[]>([]);
  readonly relationsLoading = input(false);

  protected draft: ProductDraft = this.emptyDraft();
  protected selectedFiles: File[] = [];
  protected draggedImageIndex: number | null = null;
  protected dragOverImageIndex: number | null = null;
  protected readonly validationError = signal<string | null>(null);
  protected readonly activeStep = signal(0);

  private readonly relationDefaultsEffect = effect(() => {
    const source = this.sourceOptions()[0];
    const account = this.accountOptions()[0];
    const transaction = this.transactionTypeOptions().find((option) => option.label.toLowerCase().includes('compra'))
      ?? this.transactionTypeOptions()[0];

    this.draft = {
      ...this.draft,
      purchaseSourceId: this.draft.purchaseSourceId ?? (source ? Number(source.value) : null),
      purchaseAccountId: this.draft.purchaseAccountId ?? (account ? Number(account.value) : null),
      purchaseTransactionTypeId: this.draft.purchaseTransactionTypeId ?? (transaction ? Number(transaction.value) : null),
    };
  });

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

  protected chooseCondition(value: string): void {
    if (this.conditions.some((option) => option.value === value)) {
      this.draft = { ...this.draft, condition: value as ProductCondition };
    }
  }

  protected choosePurchaseSource(value: string): void {
    this.draft = { ...this.draft, purchaseSourceId: Number(value) };
    this.validationError.set(null);
  }

  protected choosePurchaseAccount(value: string): void {
    this.draft = { ...this.draft, purchaseAccountId: Number(value) };
    this.validationError.set(null);
  }

  protected chooseStatus(value: string): void {
    if (this.statuses.some((option) => option.value === value)) {
      this.draft = { ...this.draft, status: value as ProductStatus };
    }
  }

  protected selectStep(step: number): void {
    if (this.saving() || step < 0 || step >= this.steps.length) {
      return;
    }

    this.validationError.set(null);
    this.activeStep.set(step);
  }

  protected nextStep(): void {
    if (this.activeStep() === 0 && !this.draft.name.trim()) {
      this.validationError.set('Indica o nome do produto antes de continuar.');
      return;
    }

    this.validationError.set(null);
    this.activeStep.update((step) => Math.min(step + 1, this.steps.length - 1));
  }

  protected previousStep(): void {
    this.validationError.set(null);
    this.activeStep.update((step) => Math.max(step - 1, 0));
  }

  protected onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const remainingSlots = ProductForm.maxImages - this.selectedFiles.length;
    const files = Array.from(input.files ?? []).slice(0, Math.max(remainingSlots, 0));
    input.value = '';

    if (!files.length) {
      return;
    }

    this.selectedFiles = [
      ...this.selectedFiles,
      ...files,
    ].slice(0, ProductForm.maxImages);
  }

  protected removeFile(index: number): void {
    this.selectedFiles = this.selectedFiles.filter((_, fileIndex) => fileIndex !== index);
  }

  protected onImageDragStart(index: number, event: DragEvent): void {
    this.draggedImageIndex = index;
    this.dragOverImageIndex = index;

    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', String(index));
    }
  }

  protected onImageDragOver(index: number, event: DragEvent): void {
    event.preventDefault();
    this.dragOverImageIndex = index;

    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
  }

  protected onImageDrop(targetIndex: number, event: DragEvent): void {
    event.preventDefault();

    const dataIndex = Number(event.dataTransfer?.getData('text/plain'));
    const sourceIndex = this.draggedImageIndex ?? (Number.isInteger(dataIndex) ? dataIndex : null);

    if (sourceIndex === null || sourceIndex < 0 || sourceIndex >= this.selectedFiles.length) {
      this.onImageDragEnd();
      return;
    }

    if (sourceIndex !== targetIndex) {
      const reorderedFiles = [...this.selectedFiles];
      const [movedFile] = reorderedFiles.splice(sourceIndex, 1);
      const targetElement = event.currentTarget as HTMLElement | null;
      const targetRect = targetElement?.getBoundingClientRect();
      const droppedAfterTarget = targetRect
        ? event.clientX > targetRect.left + targetRect.width / 2
        : false;
      let insertionIndex = targetIndex + (droppedAfterTarget ? 1 : 0);

      if (sourceIndex < insertionIndex) {
        insertionIndex -= 1;
      }

      reorderedFiles.splice(insertionIndex, 0, movedFile);
      this.selectedFiles = reorderedFiles;
    }

    this.onImageDragEnd();
  }

  protected onImageDragEnd(): void {
    this.draggedImageIndex = null;
    this.dragOverImageIndex = null;
  }

  protected filePreview(file: File): string {
    return URL.createObjectURL(file);
  }

  protected submit(): void {
    if (this.saving() || !this.draft.name.trim()) {
      return;
    }

    if (!this.draft.purchaseSourceId || !this.draft.purchaseAccountId || !this.draft.purchaseDate) {
      this.validationError.set('Seleciona a fonte, a conta e a data da compra.');
      return;
    }

    if (!this.draft.purchaseTransactionTypeId) {
      this.validationError.set('Não foi possível preparar o movimento de compra. Tenta recarregar o formulário.');
      return;
    }

    this.validationError.set(null);

    this.submitted.emit({
      ...this.draft,
      name: this.draft.name.trim(),
      description: this.draft.description.trim(),
      category: this.draft.category.trim(),
      brand: this.draft.brand.trim(),
      size: this.draft.size.trim(),
      color: this.draft.color.trim(),
      notes: this.draft.notes.trim(),
      purchasePrice: Number(this.draft.purchasePrice) || 0,
      purchaseShippingCost: Number(this.draft.purchaseShippingCost) || 0,
      purchaseOtherCosts: Number(this.draft.purchaseOtherCosts) || 0,
      purchaseTrackingNumber: this.draft.purchaseTrackingNumber.trim(),
      purchaseOrderNotes: this.draft.purchaseOrderNotes.trim(),
      listingPrice: this.draft.listingPrice === null || this.draft.listingPrice === undefined
        ? null
        : Number(this.draft.listingPrice),
      minimumPrice: this.draft.minimumPrice === null || this.draft.minimumPrice === undefined
        ? null
        : Number(this.draft.minimumPrice),
      images: this.selectedFiles,
    });
  }

  private emptyDraft(): ProductDraft {
    return {
      name: '',
      description: '',
      notes: '',
      category: '',
      brand: '',
      size: '',
      color: '',
      condition: 'good',
      status: 'active',
      purchasePrice: 0,
      listingPrice: null,
      minimumPrice: null,
      purchaseSourceId: null,
      purchaseAccountId: null,
      purchaseTransactionTypeId: null,
      purchaseDate: new Date().toISOString().slice(0, 10),
      purchaseTrackingNumber: '',
      purchaseShippingCost: 0,
      purchaseOtherCosts: 0,
      purchaseOrderNotes: '',
      images: [],
    };
  }

}
