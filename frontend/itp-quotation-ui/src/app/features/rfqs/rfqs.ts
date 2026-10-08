import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Customer, Rfq } from '../../core/api.models';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-rfqs',
  imports: [DatePipe, MatButtonModule, RouterLink],
  templateUrl: './rfqs.html',
  styleUrl: '../shared-list.scss',
})
export class Rfqs {
  private readonly api = inject(ApiService);
  protected readonly rows = signal<Rfq[]>([]);
  protected readonly customers = signal<Map<number, Customer>>(new Map());
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly deletingId = signal<number | null>(null);

  constructor() {
    forkJoin({ rfqs: this.api.rfqs(), customers: this.api.customers() }).subscribe({
      next: ({ rfqs, customers }) => {
        this.rows.set(rfqs);
        this.customers.set(new Map(customers.map((customer) => [customer.id, customer])));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load RFQs.');
        this.loading.set(false);
      },
    });
  }

  protected customerName(id: number): string {
    return this.customers().get(id)?.customerName ?? 'Unknown customer';
  }

  protected deleteRfq(rfq: Rfq): void {
    if (this.deletingId() !== null) return;
    if (!window.confirm(`Delete RFQ ${rfq.rfqNumber} and all of its extracted items?`)) return;

    this.deletingId.set(rfq.id);
    this.error.set('');
    this.api.deleteRfq(rfq.id).subscribe({
      next: () => {
        this.rows.update((rows) => rows.filter((row) => row.id !== rfq.id));
        this.deletingId.set(null);
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(
          typeof error.error === 'string' && error.error
            ? error.error
            : error.error?.detail ?? 'Unable to delete this RFQ.',
        );
        this.deletingId.set(null);
      },
    });
  }
}
