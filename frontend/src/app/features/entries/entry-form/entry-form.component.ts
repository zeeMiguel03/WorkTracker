import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { FilePicker } from '../../../shared/ui/file-picker/file-picker.component';
import { Dropdown } from '../../../shared/ui/dropdown/dropdown.component';
import {
  EntryEndMode,
  EntryFilterOption,
  EntryFlow,
  EntryFormDraft,
  EntryFrequency,
  EntryOperationMode,
} from '../models/entry-list.model';

@Component({
  imports: [Dropdown, FilePicker, FormsModule],
  selector: 'app-entry-form',
  styleUrl: './entry-form.component.scss',
  templateUrl: './entry-form.component.html',
})
export class EntryForm {
  readonly accountOptions = input.required<readonly EntryFilterOption[]>();
  readonly sourceOptions = input.required<readonly EntryFilterOption[]>();
  readonly transactionTypeOptions = input.required<readonly EntryFilterOption[]>();
  readonly submitted = output<EntryFormDraft>();
  readonly cancelled = output<void>();

  protected readonly operationModes: readonly { value: EntryOperationMode; label: string; description: string }[] = [
    { value: 'single', label: 'Operação única', description: 'Acontece uma vez' },
    { value: 'recurring', label: 'Recorrente', description: 'Repete automaticamente' },
  ];
  protected readonly flowOptions: readonly EntryFilterOption[] = [
    { value: 'expense', label: 'Gastei dinheiro' },
    { value: 'income', label: 'Ganhei dinheiro' },
  ];
  protected readonly frequencyOptions: readonly EntryFilterOption[] = [
    { value: 'daily', label: 'Diária' },
    { value: 'weekly', label: 'Semanal' },
    { value: 'monthly', label: 'Mensal' },
    { value: 'yearly', label: 'Anual' },
  ];
  protected readonly endModeOptions: readonly EntryFilterOption[] = [
    { value: 'never', label: 'Sem data de fim' },
    { value: 'date', label: 'Termina numa data' },
  ];

  protected draft: EntryFormDraft = this.createDraft();
  protected imagePreviewUrl: string | null = null;
  private imageObjectUrl: string | null = null;

  protected selectOperationMode(value: string): void {
    if (value === 'single' || value === 'recurring') {
      this.draft = { ...this.draft, operationMode: value };
    }
  }

  protected selectFlow(value: string): void {
    if (value === 'expense' || value === 'income') {
      this.draft = { ...this.draft, flow: value };
    }
  }

  protected selectAccount(value: string): void {
    this.draft = { ...this.draft, account: value };
  }

  protected selectSource(value: string): void {
    this.draft = { ...this.draft, source: value };
  }

  protected selectTransactionType(value: string): void {
    this.draft = { ...this.draft, transactionType: value };
  }

  protected selectImage(file: File | null): void {
    if (this.imageObjectUrl) {
      URL.revokeObjectURL(this.imageObjectUrl);
      this.imageObjectUrl = null;
    }

    this.draft = { ...this.draft, imageFile: file };

    if (file) {
      this.imageObjectUrl = URL.createObjectURL(file);
      this.imagePreviewUrl = this.imageObjectUrl;
    } else {
      this.imagePreviewUrl = null;
    }
  }

  protected selectFrequency(value: string): void {
    if (value === 'daily' || value === 'weekly' || value === 'monthly' || value === 'yearly') {
      this.draft = { ...this.draft, frequency: value };
    }
  }

  protected selectEndMode(value: string): void {
    if (value === 'never' || value === 'date') {
      this.draft = {
        ...this.draft,
        endMode: value,
        endDate: value === 'never' ? '' : this.draft.endDate,
      };
    }
  }

  protected submit(): void {
    this.submitted.emit({
      ...this.draft,
      name: this.draft.name.trim(),
      quantity: Number(this.draft.quantity) || 1,
      value: Number(this.draft.value) || 0,
    });
  }

  private createDraft(): EntryFormDraft {
    return {
      name: '',
      source: 'Vinted',
      transactionType: 'Compra',
      account: 'Revolut',
      quantity: 1,
      value: 0,
      date: new Date().toISOString().slice(0, 10),
      description: '',
      operationMode: 'single',
      flow: 'expense',
      frequency: 'monthly',
      endMode: 'never',
      endDate: '',
      imageFile: null,
    };
  }
}
