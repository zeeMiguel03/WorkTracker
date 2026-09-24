import { Component, input, output, signal } from '@angular/core';

import { FilePicker } from '../../../../shared/ui/file-picker/file-picker.component';
import { SourceCardModel } from '../../models/source-card.model';
import {
  CreateSourceRequest,
  UpdateSourceRequest,
} from '../../models/source.model';

@Component({
  imports: [FilePicker],
  selector: 'app-source-form',
  templateUrl: './source-form.component.html',
  styleUrl: './source-form.component.scss',
})
export class SourceForm {
  readonly source = input<SourceCardModel | null>(null);
  readonly saving = input(false);
  readonly error = input<string | null>(null);

  readonly cancelled = output<void>();
  readonly submitted = output<CreateSourceRequest | UpdateSourceRequest>();

  protected readonly selectedImage = signal<File | null>(null);

  protected handleImageSelected(file: File | null): void {
    this.selectedImage.set(file);
  }

  protected submit(event: SubmitEvent): void {
    event.preventDefault();

    if (this.saving()) {
      return;
    }

    const form = event.currentTarget as HTMLFormElement;
    const formData = new FormData(form);
    const name = String(formData.get('sourceName') ?? '').trim();
    const link = String(formData.get('sourceLink') ?? '').trim();

    if (!name) {
      return;
    }

    const imageFile = this.selectedImage();
    const currentSource = this.source();

    if (currentSource) {
      this.submitted.emit({
        name,
        imageFile,
        link,
        isActive: formData.get('sourceActive') === 'on',
      });

      return;
    }

    this.submitted.emit({
      name,
      imageFile,
      link,
    });
  }
}
