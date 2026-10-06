import {
  Component,
  inject,
  input,
  OnChanges,
  OnDestroy,
  OnInit,
  output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { Subscription } from 'rxjs';

import { SourceCardModel } from '../../models/source-card.model';
import { SourceService } from '../../services/source.service';
import { TranslatePipe } from '../../../../core/i18n/translate.pipe';
import { LanguageService } from '../../../../core/i18n/language.service';

@Component({
  imports: [DatePipe, TranslatePipe],
  selector: 'app-source-card',
  templateUrl: './source-card.component.html',
  styleUrl: './source-card.component.scss',
})
export class SourceCard implements OnInit, OnChanges, OnDestroy {
  private readonly sourceService = inject(SourceService);
  protected readonly language = inject(LanguageService);

  readonly source = input.required<SourceCardModel>();
  readonly editRequested = output<number>();
  readonly deleteRequested = output<number>();
  protected readonly menuOpen = signal(false);
  protected readonly imageSrc = signal<string | null>(null);
  protected readonly imageFailed = signal(false);

  private imageObjectUrl: string | null = null;
  private imageSubscription: Subscription | null = null;

  ngOnInit(): void {
    this.loadSourceImage();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['source'] && !changes['source'].firstChange) {
      this.loadSourceImage();
    }
  }

  ngOnDestroy(): void {
    this.imageSubscription?.unsubscribe();
    this.revokeImageUrl();
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected edit(): void {
    this.menuOpen.set(false);
    this.editRequested.emit(this.source().id);
  }

  protected remove(): void {
    this.menuOpen.set(false);
    this.deleteRequested.emit(this.source().id);
  }

  protected handleImageError(): void {
    this.imageFailed.set(true);
    this.revokeImageUrl();
    this.imageSrc.set(null);
  }

  private loadSourceImage(): void {
    this.imageSubscription?.unsubscribe();
    this.revokeImageUrl();
    this.imageSrc.set(null);
    this.imageFailed.set(false);

    const source = this.source();

    if (!source.imageUrl) {
      return;
    }

    this.imageSubscription = this.sourceService.getImage(source.id).subscribe({
      next: (image) => {
        this.imageObjectUrl = URL.createObjectURL(image);
        this.imageSrc.set(this.imageObjectUrl);
      },
      error: () => {
        this.imageFailed.set(true);
      },
    });
  }

  private revokeImageUrl(): void {
    if (this.imageObjectUrl) {
      URL.revokeObjectURL(this.imageObjectUrl);
      this.imageObjectUrl = null;
    }
  }
}
