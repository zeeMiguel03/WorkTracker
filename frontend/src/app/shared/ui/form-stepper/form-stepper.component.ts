import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

export interface FormStep {
  readonly id: number;
  readonly label: string;
  readonly description: string;
}

@Component({
  imports: [TranslatePipe],
  selector: 'app-form-stepper',
  templateUrl: './form-stepper.component.html',
  styleUrl: './form-stepper.component.scss',
})
export class FormStepper {
  readonly steps = input.required<readonly FormStep[]>();
  readonly activeStep = input.required<number>();
  readonly disabled = input(false);
  readonly ariaLabel = input('Etapas do formulário');
  readonly stepSelected = output<number>();

  protected select(step: number): void {
    if (!this.disabled()) this.stepSelected.emit(step);
  }
}
