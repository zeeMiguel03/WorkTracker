import { Component, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Dropdown, DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { ProductCondition, ProductDraft } from '../models/product.model';

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

  protected draft: ProductDraft = this.emptyDraft();
  protected selectedFiles: File[] = [];
  protected draggedImageIndex: number | null = null;
  protected dragOverImageIndex: number | null = null;

  protected readonly conditions: readonly DropdownOption[] = [
    { value: 'new-with-tags', label: 'Novo com etiqueta' },
    { value: 'new-without-tags', label: 'Novo sem etiqueta' },
    { value: 'very-good', label: 'Muito bom' },
    { value: 'good', label: 'Bom' },
    { value: 'satisfactory', label: 'Satisfatório' },
  ];

  protected chooseCondition(value: string): void {
    if (this.conditions.some((option) => option.value === value)) {
      this.draft = { ...this.draft, condition: value as ProductCondition };
    }
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
    if (!this.draft.name.trim()) {
      return;
    }

    this.submitted.emit({
      ...this.draft,
      name: this.draft.name.trim(),
      description: this.draft.description.trim(),
      category: this.draft.category.trim(),
      brand: this.draft.brand.trim(),
      size: this.draft.size.trim(),
      color: this.draft.color.trim(),
      purchasePrice: Number(this.draft.purchasePrice) || 0,
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
      category: '',
      brand: '',
      size: '',
      color: '',
      condition: 'good',
      purchasePrice: 0,
      listingPrice: null,
      minimumPrice: null,
      images: [],
    };
  }

}
