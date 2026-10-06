import { Component, computed, inject } from '@angular/core';
import { LanguageService } from '../../../core/i18n/language.service';

@Component({
  selector: 'app-language-toggle',
  template: `<button class="language-toggle" type="button" (click)="language.toggle()" [attr.aria-label]="switchLabel()" [attr.title]="switchLabel()"><svg viewBox="0 0 20 20" aria-hidden="true"><circle cx="10" cy="10" r="7.5"/><path d="M2.8 10h14.4M10 2.5c2 2.1 3 4.6 3 7.5s-1 5.4-3 7.5c-2-2.1-3-4.6-3-7.5s1-5.4 3-7.5Z"/></svg><span>{{ language.isEnglish() ? 'EN' : 'PT' }}</span></button>`,
  styles: [`:host{display:inline-flex}.language-toggle{display:inline-flex;min-width:44px;height:44px;align-items:center;justify-content:center;gap:5px;padding:0 9px;color:var(--app-text-muted,#667085);background:transparent;border:1px solid var(--app-border,#d0d5dd);border-radius:9px;font-family:inherit;font-size:12px;font-weight:600;line-height:1;cursor:pointer}.language-toggle:hover{color:var(--app-text,#101828);background:var(--app-surface-muted,#f2f4f7);border-color:var(--app-border-strong,#98a2b3)}.language-toggle:focus-visible{outline:2px solid #7592ff;outline-offset:2px}.language-toggle svg{width:17px;height:17px;fill:none;stroke:currentColor;stroke-width:1.5;stroke-linecap:round;stroke-linejoin:round}`],
})
export class LanguageToggle {
  protected readonly language = inject(LanguageService);
  protected readonly switchLabel = computed(() => this.language.isEnglish() ? 'Switch language to Portuguese' : 'Mudar idioma para inglês');
}
