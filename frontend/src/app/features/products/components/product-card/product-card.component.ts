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
import { Subscription } from 'rxjs';

import { ProductListItem } from '../../models/product.model';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-product-card',
  styleUrl: './product-card.component.scss',
  templateUrl: './product-card.component.html',
})
export class ProductCard implements OnInit, OnChanges, OnDestroy {
  private readonly productService = inject(ProductService);

  readonly product = input.required<ProductListItem>();
  readonly selected = output<number>();
  protected readonly imageSrc = signal<string | null>(null);
  protected readonly imageFailed = signal(false);

  private imageObjectUrl: string | null = null;
  private imageSubscription: Subscription | null = null;

  ngOnInit(): void {
    this.loadProductImage();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['product'] && !changes['product'].firstChange) {
      this.loadProductImage();
    }
  }

  ngOnDestroy(): void {
    this.imageSubscription?.unsubscribe();
    this.revokeImageUrl();
  }

  protected handleImageError(): void {
    this.imageFailed.set(true);
    this.revokeImageUrl();
    this.imageSrc.set(null);
  }

  protected formatPrice(price: number): string {
    return new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR', minimumFractionDigits: 2 }).format(price);
  }

  private loadProductImage(): void {
    this.imageSubscription?.unsubscribe();
    this.revokeImageUrl();
    this.imageSrc.set(null);
    this.imageFailed.set(false);

    const product = this.product();

    if (!product.imageId) {
      return;
    }

    this.imageSubscription = this.productService.getImage(product.id, product.imageId).subscribe({
      next: (image) => {
        this.imageObjectUrl = URL.createObjectURL(image);
        this.imageSrc.set(this.imageObjectUrl);
      },
      error: () => this.imageFailed.set(true),
    });
  }

  private revokeImageUrl(): void {
    if (this.imageObjectUrl) {
      URL.revokeObjectURL(this.imageObjectUrl);
      this.imageObjectUrl = null;
    }
  }
}
