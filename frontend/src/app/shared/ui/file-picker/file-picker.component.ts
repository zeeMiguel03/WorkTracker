import { Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-file-picker',
  templateUrl: './file-picker.component.html',
  styleUrl: './file-picker.component.scss',
})
export class FilePicker {
  readonly inputId = input.required<string>();
  readonly inputName = input('file');
  readonly accept = input('');
  readonly placeholder = input('Nenhum ficheiro selecionado');
  readonly fileSelected = output<File | null>();

  protected readonly selectedFile = signal<File | null>(null);

  protected handleFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0) ?? null;

    this.selectedFile.set(file);
    this.fileSelected.emit(file);
  }
}
