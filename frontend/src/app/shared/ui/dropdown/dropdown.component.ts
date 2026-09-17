import { Component, computed, ElementRef, HostBinding, HostListener, input, output, signal } from '@angular/core';

export interface DropdownOption {
  readonly value: string;
  readonly label: string;
}

let dropdownId = 0;

@Component({
  selector: 'app-dropdown',
  styleUrl: './dropdown.component.scss',
  templateUrl: './dropdown.component.html',
})
export class Dropdown {
  readonly options = input.required<readonly DropdownOption[]>();
  readonly value = input('');
  readonly fullWidth = input(false);
  readonly valueChange = output<string>();

  protected readonly isOpen = signal(false);
  protected readonly triggerId = `dropdown-trigger-${++dropdownId}`;

  @HostBinding('class.dropdown--full-width')
  protected get isFullWidth(): boolean {
    return this.fullWidth();
  }

  protected readonly selectedOption = computed(() =>
    this.options().find((option) => option.value === this.value()) ?? this.options()[0],
  );

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {}

  open(): void {
    this.isOpen.set(true);
  }

  protected toggle(event: MouseEvent): void {
    event.stopPropagation();
    this.isOpen.update((open) => !open);
  }

  protected choose(option: DropdownOption, event: MouseEvent): void {
    event.stopPropagation();
    this.valueChange.emit(option.value);
    this.isOpen.set(false);
  }

  protected close(): void {
    this.isOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  protected handleDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.close();
    }
  }

  @HostListener('document:keydown.escape')
  protected handleEscape(): void {
    this.close();
  }
}
