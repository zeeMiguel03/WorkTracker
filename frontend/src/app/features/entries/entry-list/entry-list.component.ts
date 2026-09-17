import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Modal } from '../../../shared/ui/modal/modal.component';
import { EntryFilters } from '../components/entry-filters/entry-filters.component';
import { EntryTable } from '../components/entry-table/entry-table.component';
import { EntryForm } from '../entry-form/entry-form.component';
import {
  EntryFilterOption,
  EntryFormDraft,
  EntryListItem,
  EntrySortField,
} from '../models/entry-list.model';

const MOCK_ENTRIES: readonly EntryListItem[] = [
  {
    id: 1,
    name: 'Desenvolvimento de website',
    source: 'Cliente direto',
    transactionType: 'Serviço',
    account: 'Revolut',
    quantity: 1,
    value: 850,
    date: '2026-09-15',
    color: '#7592FF',
    imageUrl: null,
  },
  {
    id: 2,
    name: 'Subscrição Adobe Creative Cloud',
    source: 'Adobe',
    transactionType: 'Software',
    account: 'Conta principal',
    quantity: 1,
    value: 67.99,
    date: '2026-09-13',
    color: '#F97066',
    imageUrl: null,
  },
  {
    id: 3,
    name: 'Consultoria de marketing',
    source: 'Fiverr',
    transactionType: 'Serviço',
    account: 'Revolut',
    quantity: 2,
    value: 420,
    date: '2026-09-11',
    color: '#32D583',
    imageUrl: null,
  },
  {
    id: 4,
    name: 'Licença Figma Professional',
    source: 'Figma',
    transactionType: 'Software',
    account: 'Conta principal',
    quantity: 1,
    value: 15,
    date: '2026-09-08',
    color: '#B692F6',
    imageUrl: null,
  },
  {
    id: 5,
    name: 'Manutenção de loja online',
    source: 'Cliente direto',
    transactionType: 'Serviço',
    account: 'Carteira',
    quantity: 1,
    value: 275,
    date: '2026-09-05',
    color: '#F79009',
    imageUrl: null,
  },
  {
    id: 6,
    name: 'Alojamento do projeto',
    source: 'Cloudflare',
    transactionType: 'Infraestrutura',
    account: 'Conta principal',
    quantity: 1,
    value: 24.5,
    date: '2026-09-02',
    color: '#7592FF',
    imageUrl: null,
  },
];

@Component({
  imports: [EntryFilters, EntryForm, EntryTable, Modal, RouterLink],
  selector: 'app-entry-list',
  styleUrl: './entry-list.component.scss',
  templateUrl: './entry-list.component.html',
})
export class EntryList {
  protected readonly entries = signal<readonly EntryListItem[]>(MOCK_ENTRIES);
  protected readonly searchTerm = signal('');
  protected readonly filtersOpen = signal(false);
  protected readonly accountFilter = signal('all');
  protected readonly sourceFilter = signal('all');
  protected readonly transactionTypeFilter = signal('all');
  protected readonly entryModalOpen = signal(false);
  protected readonly sortField = signal<EntrySortField>('date');
  protected readonly sortDirection = signal<'asc' | 'desc'>('desc');

  protected readonly accountOptions: readonly EntryFilterOption[] = [
    { value: 'all', label: 'Todas as contas' },
    { value: 'Revolut', label: 'Revolut' },
    { value: 'Conta principal', label: 'Conta principal' },
    { value: 'Carteira', label: 'Carteira' },
  ];
  protected readonly sourceOptions: readonly EntryFilterOption[] = [
    { value: 'all', label: 'Todas as fontes' },
    { value: 'Vinted', label: 'Vinted' },
    { value: 'CTT', label: 'CTT' },
    { value: 'Outra fonte', label: 'Outra fonte' },
  ];
  protected readonly transactionTypeOptions: readonly EntryFilterOption[] = [
    { value: 'all', label: 'Todos os tipos' },
    { value: 'Compra', label: 'Compra' },
    { value: 'Venda', label: 'Venda' },
    { value: 'Envio', label: 'Envio' },
    { value: 'Taxa', label: 'Taxa' },
  ];

  protected readonly visibleEntries = computed(() => {
    const query = this.normalize(this.searchTerm());
    const account = this.accountFilter();
    const source = this.sourceFilter();
    const transactionType = this.transactionTypeFilter();
    const field = this.sortField();
    const direction = this.sortDirection() === 'asc' ? 1 : -1;

    return this.entries()
      .filter((entry) => {
        const matchesSearch = !query || this.normalize(
          `${entry.name} ${entry.source} ${entry.transactionType} ${entry.account}`,
        ).includes(query);
        const matchesAccount = account === 'all' || entry.account === account;
        const matchesSource = source === 'all' || entry.source === source;
        const matchesType = transactionType === 'all' || entry.transactionType === transactionType;

        return matchesSearch && matchesAccount && matchesSource && matchesType;
      })
      .slice()
      .sort((left, right) => {
        const leftValue = left[field];
        const rightValue = right[field];

        if (typeof leftValue === 'number' && typeof rightValue === 'number') {
          return (leftValue - rightValue) * direction;
        }

        return String(leftValue).localeCompare(String(rightValue), 'pt-PT') * direction;
      });
  });

  protected updateSearch(search: string): void {
    this.searchTerm.set(search);
  }

  protected updateFiltersOpen(open: boolean): void {
    this.filtersOpen.set(open);
  }

  protected openCreateEntry(): void {
    this.entryModalOpen.set(true);
  }

  protected closeEntryModal(): void {
    this.entryModalOpen.set(false);
  }

  protected saveEntry(draft: EntryFormDraft): void {
    const nextId = Math.max(0, ...this.entries().map((entry) => entry.id)) + 1;

    this.entries.update((entries) => [
      {
        id: nextId,
        name: draft.name || 'Novo movimento',
        source: draft.source,
        transactionType: draft.transactionType,
        account: draft.account,
        quantity: draft.quantity,
        value: draft.value,
        date: draft.date,
        color: draft.flow === 'income' ? '#32D583' : '#F97066',
        imageUrl: draft.imageFile ? URL.createObjectURL(draft.imageFile) : null,
      },
      ...entries,
    ]);

    this.closeEntryModal();
  }

  protected requestSort(field: EntrySortField): void {
    if (this.sortField() === field) {
      this.sortDirection.update((direction) => (direction === 'asc' ? 'desc' : 'asc'));
      return;
    }

    this.sortField.set(field);
    this.sortDirection.set(field === 'date' ? 'desc' : 'asc');
  }

  protected handleEntryAction(entryId: number, action: 'edit' | 'delete'): void {
    // Kept local until the entries API and form are connected.
    void entryId;
    void action;
  }

  private normalize(value: string): string {
    return value
      .toLocaleLowerCase('pt-PT')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }
}
