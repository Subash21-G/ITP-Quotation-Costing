import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { Quotation } from '../../core/api.models';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-quotations',
  imports: [CurrencyPipe, DatePipe, MatButtonModule, RouterLink],
  templateUrl: './quotations.html',
  styleUrl: '../shared-list.scss',
})
export class Quotations {
  private readonly api = inject(ApiService);
  protected readonly rows = signal<Quotation[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');

  constructor() {
    this.api.quotations().subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load quotations.');
        this.loading.set(false);
      },
    });
  }
}
