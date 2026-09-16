import { Component, effect, input, OnDestroy, output } from '@angular/core';

import { Modal } from '../modal/modal.component';

@Component({
  imports: [Modal],
  selector: 'app-success-modal',
  templateUrl: './success-modal.component.html',
  styleUrl: './success-modal.component.scss',
})
export class SuccessModal implements OnDestroy {
  private readonly autoCloseDelay = 1000;
  private autoCloseTimer: ReturnType<typeof setTimeout> | null = null;

  readonly open = input(false);
  readonly titleId = input.required<string>();
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly closeLabel = input('Fechar');
  readonly closed = output<void>();

  private readonly autoCloseEffect = effect(() => {
    this.clearAutoCloseTimer();

    if (this.open()) {
      this.autoCloseTimer = setTimeout(() => {
        this.autoCloseTimer = null;
        this.closed.emit();
      }, this.autoCloseDelay);
    }
  });

  ngOnDestroy(): void {
    this.clearAutoCloseTimer();
  }

  private clearAutoCloseTimer(): void {
    if (this.autoCloseTimer !== null) {
      clearTimeout(this.autoCloseTimer);
      this.autoCloseTimer = null;
    }
  }
}
