import { HttpClient } from '@angular/common/http';
import { Component, computed, ElementRef, effect, HostBinding, HostListener, inject, input, OnDestroy, output, signal } from '@angular/core';
import { Subscription } from 'rxjs';

export interface DropdownOption {
  readonly value: string;
  readonly label: string;
  readonly imageUrl?: string | null;
  readonly color?: string | null;
  readonly meta?: string | null;
}

let dropdownId = 0;

@Component({
  selector: 'app-dropdown',
  styleUrl: './dropdown.component.scss',
  templateUrl: './dropdown.component.html',
})
export class Dropdown implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly imageRequests = new Map<string, Subscription>();
  private readonly imageSources = new Map<string, string>();
  private readonly imageObjectUrls = new Map<string, string>();

  readonly options = input.required<readonly DropdownOption[]>();
  readonly value = input('');
  readonly fullWidth = input(false);
  readonly openUp = input(false);
  readonly valueChange = output<string>();

  protected readonly isOpen = signal(false);
  protected readonly resolvedImageUrls = signal<Record<string, string>>({});
  protected readonly triggerId = `dropdown-trigger-${++dropdownId}`;

  private readonly imageOptionsEffect = effect(() => {
    this.syncImages(this.options());
  });

  @HostBinding('class.dropdown--full-width')
  protected get isFullWidth(): boolean {
    return this.fullWidth();
  }

  protected readonly selectedOption = computed(() =>
    this.options().find((option) => option.value === this.value()) ?? this.options()[0],
  );

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {}

  ngOnDestroy(): void {
    this.imageOptionsEffect.destroy();

    for (const request of this.imageRequests.values()) {
      request.unsubscribe();
    }

    for (const imageUrl of this.imageObjectUrls.values()) {
      URL.revokeObjectURL(imageUrl);
    }
  }

  protected imageUrl(option: DropdownOption | undefined): string | null {
    if (!option?.imageUrl) {
      return null;
    }

    return this.resolvedImageUrls()[option.value] ?? null;
  }

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

  private syncImages(options: readonly DropdownOption[]): void {
    const imageOptions = new Map(
      options
        .filter((option) => Boolean(option.imageUrl))
        .map((option) => [option.value, option.imageUrl!] as const),
    );

    for (const [value, source] of this.imageSources) {
      if (imageOptions.get(value) !== source) {
        this.clearImage(value);
      }
    }

    for (const [value, source] of imageOptions) {
      if (this.imageSources.get(value) === source || this.imageRequests.has(value)) {
        continue;
      }

      this.imageSources.set(value, source);
      this.imageRequests.set(
        value,
        this.http.get(source, { responseType: 'blob', withCredentials: true }).subscribe({
          next: (blob) => {
            const objectUrl = URL.createObjectURL(blob);
            this.imageObjectUrls.set(value, objectUrl);
            this.resolvedImageUrls.update((current) => ({ ...current, [value]: objectUrl }));
            this.imageRequests.delete(value);
          },
          error: () => {
            this.imageRequests.delete(value);
          },
        }),
      );
    }
  }

  private clearImage(value: string): void {
    this.imageRequests.get(value)?.unsubscribe();
    this.imageRequests.delete(value);
    this.imageSources.delete(value);

    const objectUrl = this.imageObjectUrls.get(value);
    if (objectUrl) {
      URL.revokeObjectURL(objectUrl);
      this.imageObjectUrls.delete(value);
    }

    this.resolvedImageUrls.update((current) => {
      if (!(value in current)) {
        return current;
      }

      const next = { ...current };
      delete next[value];
      return next;
    });
  }
}
