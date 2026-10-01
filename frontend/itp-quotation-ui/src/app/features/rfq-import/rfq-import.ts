import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import {
  Customer,
  RfqImportConfirmation,
  RfqImportItem,
  RfqPdfExtraction,
} from '../../core/api.models';
import { ApiService } from '../../core/api.service';

interface ReviewItem {
  lineItem: string;
  materialNo: string;
  description: string;
  drawingNo: string;
  quantity: number;
  unit: string;
  deliveryDate: string;
  grade: string;
  dimensions: string;
}

@Component({
  selector: 'app-rfq-import',
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    RouterLink,
  ],
  templateUrl: './rfq-import.html',
  styleUrl: './rfq-import.scss',
})
export class RfqImport {
  private readonly api = inject(ApiService);
  protected readonly customers = signal<Customer[]>([]);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly extraction = signal<RfqPdfExtraction | null>(null);
  protected readonly saved = signal<RfqImportConfirmation | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected reviewed = false;
  protected review = {
    rfqNumber: '',
    customerId: 0,
    rfqDate: '',
    status: 'Draft',
    items: [] as ReviewItem[],
  };

  constructor() {
    this.api.customers().subscribe({
      next: (customers) => this.customers.set(customers),
      error: () => this.error.set('Customers could not be loaded. Start the backend API.'),
    });
  }

  protected chooseFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.error.set('');
    this.extraction.set(null);
    this.saved.set(null);
    this.reviewed = false;
    if (file && (!file.name.toLowerCase().endsWith('.pdf') || file.size > 10 * 1024 * 1024)) {
      this.selectedFile.set(null);
      this.error.set('Choose a PDF file no larger than 10 MB.');
      return;
    }
    this.selectedFile.set(file);
  }

  protected extract(): void {
    const file = this.selectedFile();
    if (!file) {
      this.error.set('Choose an RFQ PDF first.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.api.extractRfqPdf(file).subscribe({
      next: (result) => {
        this.extraction.set(result);
        this.populateReview(result);
        this.busy.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(this.problemDetail(error, 'The PDF could not be extracted.'));
        this.busy.set(false);
      },
    });
  }

  protected addItem(): void {
    this.review.items.push(this.blankItem());
    this.reviewed = false;
  }

  protected removeItem(index: number): void {
    this.review.items.splice(index, 1);
    this.reviewed = false;
  }

  protected confirm(): void {
    if (!this.reviewed) {
      this.error.set('Confirm that you reviewed the extracted fields before saving.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.api.confirmRfqImport({
      rfqNumber: this.review.rfqNumber,
      customerId: Number(this.review.customerId),
      rfqDate: this.review.rfqDate || null,
      status: this.review.status,
      items: this.review.items.map((item) => ({
        ...item,
        quantity: Number(item.quantity),
        deliveryDate: item.deliveryDate || null,
      })),
    }).subscribe({
      next: (result) => {
        this.saved.set(result);
        this.busy.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(this.problemDetail(error, 'The reviewed RFQ could not be saved.'));
        this.busy.set(false);
      },
    });
  }

  private populateReview(result: RfqPdfExtraction): void {
    const customerName = result.customerName?.trim().toLowerCase();
    const customer = customerName
      ? this.customers().find(
          (candidate) => candidate.customerName.trim().toLowerCase() === customerName,
        )
      : undefined;
    this.review = {
      rfqNumber: result.rfqNumber ?? '',
      customerId: customer?.id ?? 0,
      rfqDate: result.rfqDate?.slice(0, 10) ?? '',
      status: 'Draft',
      items: result.items.length
        ? result.items.map((item) => this.toReviewItem(item))
        : [this.blankItem()],
    };
  }

  private toReviewItem(item: RfqImportItem): ReviewItem {
    return {
      lineItem: item.lineItem ?? '',
      materialNo: item.materialNo ?? '',
      description: item.description ?? '',
      drawingNo: item.drawingNo ?? '',
      quantity: item.quantity ?? 0,
      unit: item.unit || 'Nos',
      deliveryDate: item.deliveryDate?.slice(0, 10) ?? '',
      grade: item.grade ?? '',
      dimensions: item.dimensions ?? '',
    };
  }

  private blankItem(): ReviewItem {
    return {
      lineItem: '',
      materialNo: '',
      description: '',
      drawingNo: '',
      quantity: 0,
      unit: 'Nos',
      deliveryDate: '',
      grade: '',
      dimensions: '',
    };
  }

  private problemDetail(error: HttpErrorResponse, fallback: string): string {
    const detail = error.error?.detail;
    return typeof detail === 'string' && detail ? detail : fallback;
  }
}
