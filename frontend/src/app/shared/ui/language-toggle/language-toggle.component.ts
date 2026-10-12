import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { LanguageService } from '../../../core/i18n/language.service';

@Component({
  selector: 'app-language-toggle',
  imports: [CommonModule],
  template: `<button class="language-toggle" type="button" (click)="language.toggle()" [attr.aria-label]="switchLabel()" [attr.title]="switchLabel()"><svg class="flag" viewBox="0 0 30 20" aria-hidden="true" *ngIf="language.isEnglish(); else portugueseFlag"><rect width="30" height="20" fill="#012169"/><path d="M0 0 30 20M30 0 0 20" stroke="#fff" stroke-width="4"/><path d="M0 0 30 20M30 0 0 20" stroke="#c8102e" stroke-width="1.7"/><path d="M15 0v20M0 10h30" stroke="#fff" stroke-width="7"/><path d="M15 0v20M0 10h30" stroke="#c8102e" stroke-width="3.5"/></svg><ng-template #portugueseFlag><svg class="flag" viewBox="0 0 30 20" aria-hidden="true"><rect width="12" height="20" fill="#046a38"/><rect x="12" width="18" height="20" fill="#da291c"/><circle cx="12" cy="10" r="4" fill="#ffcd00"/><circle cx="12" cy="10" r="2.5" fill="#fff"/></svg></ng-template><span>{{ language.isEnglish() ? 'EN' : 'PT' }}</span></button>`,
  styles: [`:host{display:inline-flex}.language-toggle{display:inline-flex;min-width:44px;height:44px;align-items:center;justify-content:center;gap:6px;padding:0 9px;color:var(--app-text-muted,#667085);background:transparent;border:1px solid var(--app-border,#d0d5dd);border-radius:9px;font-family:inherit;font-size:12px;font-weight:600;line-height:1;cursor:pointer}.language-toggle:hover{color:var(--app-text,#101828);background:var(--app-surface-muted,#f2f4f7);border-color:var(--app-border-strong,#98a2b3)}.language-toggle:focus-visible{outline:2px solid #7592ff;outline-offset:2px}.language-toggle .flag{width:20px;height:14px;border-radius:2px;box-shadow:0 0 0 1px rgb(16 24 40 / 12%);flex:none}`],
})
export class LanguageToggle {
  protected readonly language = inject(LanguageService);
  protected readonly switchLabel = computed(() => this.language.isEnglish() ? 'Switch language to Portuguese' : 'Mudar idioma para inglês');
}
