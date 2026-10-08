import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-info-page',
  imports: [CurrencyPipe, MatButtonModule, RouterLink],
  templateUrl: './info-page.html',
  styleUrl: './info-page.scss',
})
export class InfoPage {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  protected readonly kind = signal(String(this.route.snapshot.data['kind'] ?? 'reports'));
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly quotations = signal<any[]>([]);
  protected readonly rfqs = signal<any[]>([]);
  protected readonly customers = signal<any[]>([]);
  protected readonly materials = signal<any[]>([]);
  protected readonly title = signal(this.kind() === 'reports' ? 'Reports' : 'Settings');
  protected readonly description = signal(
    this.kind() === 'reports'
      ? 'Commercial and costing insights will appear here as quotation history grows.'
      : 'Application defaults and company preferences will be managed here.',
  );

  constructor() {
    forkJoin({
      quotations: this.api.quotations(),
      rfqs: this.api.rfqs(),
      customers: this.api.customers(),
      materials: this.api.materials(),
    }).subscribe({
      next: (data) => {
        this.quotations.set(data.quotations);
        this.rfqs.set(data.rfqs);
        this.customers.set(data.customers);
        this.materials.set(data.materials);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The API is unavailable. Start the .NET backend on port 5263.');
        this.loading.set(false);
      },
    });
  }

  protected totalQuotedValue(): number {
    return this.quotations().reduce((total, quote) => total + quote.totalPrice, 0);
  }

  protected statusCount(status: string): number {
    return this.quotations().filter((quote) => quote.status === status).length;
  }
}
