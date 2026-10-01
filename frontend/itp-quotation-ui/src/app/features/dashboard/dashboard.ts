import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Quotation, Rfq } from '../../core/api.models';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DecimalPipe, RouterLink, MatButtonModule, MatProgressBarModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly api = inject(ApiService);
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly customers = signal(0);
  protected readonly materials = signal(0);
  protected readonly rfqs = signal<Rfq[]>([]);
  protected readonly quotations = signal<Quotation[]>([]);
  protected readonly openRfqs = computed(
    () => this.rfqs().filter((rfq) => !['Closed', 'Cancelled'].includes(rfq.status)).length,
  );
  protected readonly pipelineValue = computed(() =>
    this.quotations()
      .filter((quote) => !['Rejected', 'Lost', 'Expired'].includes(quote.status))
      .reduce((total, quote) => total + quote.totalPrice, 0),
  );
  protected readonly conversion = computed(() => {
    const quotes = this.quotations();
    if (!quotes.length) return 0;
    return (quotes.filter((quote) => quote.status === 'Accepted').length / quotes.length) * 100;
  });

  constructor() {
    forkJoin({
      customers: this.api.customers(),
      materials: this.api.materials(),
      rfqs: this.api.rfqs(),
      quotations: this.api.quotations(),
    }).subscribe({
      next: (data) => {
        this.customers.set(data.customers.length);
        this.materials.set(data.materials.length);
        this.rfqs.set(data.rfqs);
        this.quotations.set(data.quotations);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The API is unavailable. Start the .NET backend on port 5263.');
        this.loading.set(false);
      },
    });
  }
}
