import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { ProductCondition, ProductDraft, ProductStatus } from '../models/product.model';

@Component({
  imports: [FormsModule, Dropdown],
  selector: 'app-product-form',
  styleUrl: './product-form.component.scss',
  templateUrl: './product-form.component.html',
})
export class ProductForm {
  private static readonly maxImages = 5;

  readonly submitted = output<ProductDraft>();
  readonly cancelled = output<void>();
  readonly saving = input(false);
  readonly errorMessage = input<string | null>(null);

  protected draft: ProductDraft = this.emptyDraft();
  protected selectedFiles: File[] = [];
  protected draggedImageIndex: number | null = null;
  protected dragOverImageIndex: number | null = null;
  protected readonly validationError = signal<string | null>(null);

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
    { value: 'archived', label: 'Arquivado' },
  ];

  protected chooseCondition(value: string): void {
    if (this.conditions.some((option) => option.value === value)) {
      this.draft = { ...this.draft, condition: value as ProductCondition };
    }
  }

  protected chooseStatus(value: string): void {
    if (this.statuses.some((option) => option.value === value)) {
      this.draft = { ...this.draft, status: value as ProductStatus };
    }
  }

  protected onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []).slice(0, Math.max(ProductForm.maxImages - this.selectedFiles.length, 0));
    input.value = '';
    if (files.length) this.selectedFiles = [...this.selectedFiles, ...files].slice(0, ProductForm.maxImages);
  }

  protected removeFile(index: number): void {
    this.selectedFiles = this.selectedFiles.filter((_, fileIndex) => fileIndex !== index);
  }

  protected onImageDragStart(index: number, event: DragEvent): void {
    this.draggedImageIndex = index;
    this.dragOverImageIndex = index;
    event.dataTransfer?.setData('text/plain', String(index));
  }

  protected onImageDragOver(index: number, event: DragEvent): void {
    event.preventDefault();
    this.dragOverImageIndex = index;
  }

  protected onImageDrop(targetIndex: number, event: DragEvent): void {
    event.preventDefault();
    const sourceIndex = this.draggedImageIndex ?? Number(event.dataTransfer?.getData('text/plain'));
    if (Number.isInteger(sourceIndex) && sourceIndex >= 0 && sourceIndex < this.selectedFiles.length && sourceIndex !== targetIndex) {
      const files = [...this.selectedFiles];
      const [file] = files.splice(sourceIndex, 1);
      files.splice(targetIndex, 0, file);
      this.selectedFiles = files;
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
    if (this.saving()) return;

    const purchasePrice = this.numberOrZero(this.draft.purchasePrice);
    const allocatedShippingCost = this.numberOrZero(this.draft.allocatedShippingCost);
    const allocatedOtherCosts = this.numberOrZero(this.draft.allocatedOtherCosts);
    const listingPrice = this.optionalNumber(this.draft.listingPrice);
    const minimumPrice = this.optionalNumber(this.draft.minimumPrice);

    if (!this.draft.name.trim()) {
      this.validationError.set('Indica o nome do produto.');
      return;
    }
    if ([purchasePrice, allocatedShippingCost, allocatedOtherCosts, listingPrice, minimumPrice].some((value) => value !== null && value < 0)) {
      this.validationError.set('Os valores não podem ser negativos.');
      return;
    }
    if (listingPrice !== null && minimumPrice !== null && minimumPrice > listingPrice) {
      this.validationError.set('O preço mínimo não pode ser superior ao preço de venda.');
      return;
    }

    this.validationError.set(null);
    this.submitted.emit({
      ...this.draft,
      name: this.draft.name.trim(), description: this.draft.description.trim(), category: this.draft.category.trim(),
      brand: this.draft.brand.trim(), size: this.draft.size.trim(), color: this.draft.color.trim(), notes: this.draft.notes.trim(),
      purchasePrice, allocatedShippingCost, allocatedOtherCosts, listingPrice, minimumPrice, images: this.selectedFiles,
    });
  }

  private numberOrZero(value: number): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private optionalNumber(value: number | null): number | null {
    if (value === null || value === undefined) return null;
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  private emptyDraft(): ProductDraft {
    return {
      name: '', description: '', notes: '', category: '', brand: '', size: '', color: '', condition: 'good', status: 'active',
      purchasePrice: 0, allocatedShippingCost: 0, allocatedOtherCosts: 0, listingPrice: null, minimumPrice: null, images: [],
    };
  }
}
