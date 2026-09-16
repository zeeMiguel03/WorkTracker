import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss',
})
export class Modal {
  readonly open = input(false);
  readonly titleId = input.required<string>();
  readonly size = input<'default' | 'confirmation' | 'success'>('default');
  readonly closeLabel = input('Fechar');
  readonly closeOnBackdrop = input(true);
  readonly closed = output<void>();

  protected requestClose(): void {
    this.closed.emit();
  }

  protected requestBackdropClose(): void {
    if (this.closeOnBackdrop()) {
      this.requestClose();
    }
  }
}
