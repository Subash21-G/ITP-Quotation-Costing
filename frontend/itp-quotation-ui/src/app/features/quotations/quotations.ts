import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
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
  private readonly router = inject(Router);
  protected readonly downloading = signal<{
    id: number;
    format: 'standard' | 'costing';
  } | null>(null);
  protected readonly deletingId = signal<number | null>(null);

  protected editQuotation(quote: Quotation): void {
    void this.router.navigate(['/new-quotation'], {
      queryParams: { quotationId: quote.id },
    });
  }

  protected deleteQuotation(quote: Quotation): void {
    if (this.deletingId() !== null || quote.poTrackerPurchaseOrderId) return;
    if (!window.confirm(`Delete quotation ${quote.quotationNumber} and all its revisions? This cannot be undone.`)) return;

    this.deletingId.set(quote.id);
    this.error.set('');
    this.message.set('');
    this.api.deleteQuotation(quote.id).subscribe({
      next: () => {
        this.rows.update(rows => rows.filter(row => row.id !== quote.id));
        this.deletingId.set(null);
        this.message.set(`Quotation ${quote.quotationNumber} deleted.`);
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(error.error?.detail ?? 'Unable to delete this quotation.');
        this.deletingId.set(null);
      },
    });
  }

  protected downloadPdf(quote: Quotation, format: 'standard' | 'costing'): void {
    if (this.downloading() !== null) return;
    this.downloading.set({ id: quote.id, format });
    this.error.set('');
    const request = format === 'standard'
      ? this.api.quotationPdf(quote.id)
      : this.api.quotationCostingPdf(quote.id);
    request.subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = format === 'standard'
          ? `quotation-${quote.id}-rev-${quote.revision}.pdf`
          : `quotation-costing-${quote.id}-rev-${quote.revision}.pdf`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        this.downloading.set(null);
      },
      error: () => {
        this.error.set(`Unable to download the ${format === 'standard' ? 'quotation' : 'detailed costing'} PDF.`);
        this.downloading.set(null);
      },
    });
  }
  protected readonly api = inject(ApiService);
  protected readonly rows = signal<Quotation[]>([]);
  protected readonly lineItemNumbers = signal<Record<number, string>>({});
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly exporting = signal(false);
  protected readonly selected = signal<Quotation | null>(null);
  protected customerPoNumber = '';

  constructor() {
    this.api.quotations().subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
        for (const rfqId of new Set(rows.map(row => row.rfqId))) {
          this.api.rfqItems(rfqId).subscribe({
            next: items => {
              const numbers = new Map(items.map(item => [item.id, item.lineItem?.trim() || '—']));
              this.lineItemNumbers.update(current => {
                const updated = { ...current };
                for (const quote of rows.filter(row => row.rfqId === rfqId)) {
                  updated[quote.id] = quote.items.map(item => numbers.get(item.rfqItemId) ?? '—').join(', ');
                }
                return updated;
              });
            },
          });
        }
      },
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
