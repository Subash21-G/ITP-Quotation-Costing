import { DatePipe } from '@angular/common';
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
}
