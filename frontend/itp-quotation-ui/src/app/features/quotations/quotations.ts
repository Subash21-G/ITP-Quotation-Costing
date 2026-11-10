import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { Quotation } from '../../core/api.models';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-quotations',
  imports: [CurrencyPipe, DatePipe, FormsModule, MatButtonModule, RouterLink],
  templateUrl: './quotations.html',
  styleUrl: '../shared-list.scss',
})
export class Quotations {
  protected readonly auth = inject(AuthService);
  protected readonly downloading = signal<number | null>(null);

  protected downloadPdf(quote: Quotation): void {
    if (this.downloading() !== null) return;
    this.downloading.set(quote.id);
    this.error.set('');
    this.api.quotationPdf(quote.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `quotation-${quote.id}-rev-${quote.revision}.pdf`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        this.downloading.set(null);
      },
      error: () => { this.error.set('Unable to download the quotation PDF.'); this.downloading.set(null); },
    });
  }
  protected readonly api = inject(ApiService);
  protected readonly rows = signal<Quotation[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly exporting = signal(false);
  protected readonly selected = signal<Quotation | null>(null);
  protected customerPoNumber = '';

  constructor() {
    this.api.quotations().subscribe({
      next: (rows) => { this.rows.set(rows); this.loading.set(false); },
      error: () => { this.error.set('Unable to load quotations.'); this.loading.set(false); },
    });
  }

  protected selectExport(quote: Quotation): void {
    this.selected.set(quote);
    this.customerPoNumber = quote.poTrackerPoNumber ?? '';
    this.error.set('');
    this.message.set('');
  }

  protected sendToPoTracker(): void {
    const quote = this.selected();
    if (!quote || this.exporting() || !this.customerPoNumber.trim()) return;
    this.exporting.set(true);
    this.error.set('');
    this.api.exportQuotation(quote.id, this.customerPoNumber.trim()).subscribe({
      next: (result) => {
        this.rows.update(rows => rows.map(row => row.id === quote.id ? {
          ...row, poTrackerPurchaseOrderId: result.purchaseOrderId,
          poTrackerPoNumber: result.poNumber, poTrackerExportedRevision: quote.revision,
        } : row));
        this.message.set(`PO ${result.poNumber} ${result.alreadyImported ? 'already exists' : 'created'} in POTracker (ID ${result.purchaseOrderId}).`);
        this.exporting.set(false);
        this.selected.set(null);
      },
      error: (response) => {
        this.error.set(response.error?.detail ?? 'Unable to send to POTracker. Retry with the same customer PO number.');
        this.exporting.set(false);
      },
    });
  }
}
