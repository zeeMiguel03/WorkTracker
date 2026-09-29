import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { DropdownOption } from '../../../shared/ui/dropdown/dropdown.component';
import { SourceService } from '../../sources/services/source.service';

export interface ProductRelationOptions {
  readonly sourceOptions: readonly DropdownOption[];
}

@Injectable({ providedIn: 'root' })
export class ProductRelationsService {
  private readonly sourceService = inject(SourceService);

  loadOptions(): Observable<ProductRelationOptions> {
    return this.sourceService.listAll().pipe(
      map((sources) => ({
        sourceOptions: sources
          .filter((source) => source.isActive)
          .map((source) => ({
            value: String(source.id),
            label: source.name,
            imageUrl: source.imageUrl ? `${environment.apiUrl}/sources/${source.id}/image` : null,
          })),
      })),
    );
  }

}
