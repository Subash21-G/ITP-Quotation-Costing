import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CostSheet,
  Customer,
  MaterialDetails,
  MaterialMaster,
  MetalMaterial,
  MetalWeightResult,
  ProcessMaster,
  Quotation,
  Rfq,
  RfqImportConfirmation,
  RfqPdfExtraction,
  RfqItem,
  Vendor,
  VendorRate,
} from './api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  customers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.baseUrl + '/customers');
  }

  rfqs(customerId?: number): Observable<Rfq[]> {
    const params = customerId
      ? new HttpParams().set('customerId', customerId)
      : undefined;
    return this.http.get<Rfq[]>(this.baseUrl + '/rfqs', { params });
  }

  rfqItems(rfqId: number): Observable<RfqItem[]> {
    return this.http.get<RfqItem[]>(this.baseUrl + '/rfqs/' + rfqId + '/items');
  }

  extractRfqPdf(file: File): Observable<RfqPdfExtraction> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<RfqPdfExtraction>(this.baseUrl + '/rfq-imports/extract', form);
  }

  confirmRfqImport(payload: object): Observable<RfqImportConfirmation> {
    return this.http.post<RfqImportConfirmation>(
      this.baseUrl + '/rfq-imports/confirm',
      payload,
    );
  }

  materials(search = ''): Observable<MaterialMaster[]> {
    const params = search ? new HttpParams().set('search', search) : undefined;
    return this.http.get<MaterialMaster[]>(this.baseUrl + '/materials', { params });
  }

  material(materialNo: string): Observable<MaterialDetails> {
    return this.http.get<MaterialDetails>(
      this.baseUrl + '/materials/' + encodeURIComponent(materialNo),
    );
  }

  createMaterial(payload: object): Observable<MaterialMaster> {
    return this.http.post<MaterialMaster>(this.baseUrl + '/materials', payload);
  }

  updateMaterial(id: number, payload: object): Observable<MaterialMaster> {
    return this.http.put<MaterialMaster>(this.baseUrl + '/materials/' + id, payload);
  }

  createRoute(materialId: number, payload: object): Observable<object> {
    return this.http.post(this.baseUrl + '/materials/' + materialId + '/routing', payload);
  }

  updateRoute(materialId: number, routeId: number, payload: object): Observable<object> {
    return this.http.put(
      this.baseUrl + '/materials/' + materialId + '/routing/' + routeId,
      payload,
    );
  }

  deleteRoute(materialId: number, routeId: number): Observable<void> {
    return this.http.delete<void>(
      this.baseUrl + '/materials/' + materialId + '/routing/' + routeId,
    );
  }

  metals(): Observable<MetalMaterial[]> {
    return this.http.get<MetalMaterial[]>(this.baseUrl + '/metals');
  }

  processes(): Observable<ProcessMaster[]> {
    return this.http.get<ProcessMaster[]>(this.baseUrl + '/processes');
  }

  vendors(): Observable<Vendor[]> {
    return this.http.get<Vendor[]>(this.baseUrl + '/vendors');
  }

  vendorRates(): Observable<VendorRate[]> {
    return this.http.get<VendorRate[]>(this.baseUrl + '/vendor-process-rates');
  }

  createMaster<T>(resource: string, payload: object): Observable<T> {
    return this.http.post<T>(this.baseUrl + '/' + resource, payload);
  }

  calculateMetalWeight(payload: object): Observable<MetalWeightResult> {
    return this.http.post<MetalWeightResult>(
      this.baseUrl + '/calculations/metal-weight',
      payload,
    );
  }

  calculateRawMaterial(payload: object): Observable<{
    grossMaterialCost: number;
    scrapRecovery: number;
    netMaterialCost: number;
  }> {
    return this.http.post<{
      grossMaterialCost: number;
      scrapRecovery: number;
      netMaterialCost: number;
    }>(this.baseUrl + '/calculations/raw-material-cost', payload);
  }

  saveCostSheet(payload: object): Observable<CostSheet> {
    return this.http.post<CostSheet>(this.baseUrl + '/cost-sheets', payload);
  }

  exportQuotation(id: number, customerPoNumber: string): Observable<{ purchaseOrderId: number; poNumber: string; revision: number; alreadyImported: boolean }> {
    return this.http.post<{ purchaseOrderId: number; poNumber: string; revision: number; alreadyImported: boolean }>(
      this.baseUrl + '/quotations/' + id + '/potracker', { customerPoNumber });
  }

  quotations(): Observable<Quotation[]> {
    return this.http.get<Quotation[]>(this.baseUrl + '/quotations');
  }

  quotationPdfUrl(id: number, revision?: number): string {
    const suffix = revision === undefined ? '/pdf' : '/revisions/' + revision + '/pdf';
    return this.baseUrl + '/quotations/' + id + suffix;
  }

  createQuotation(payload: object): Observable<Quotation> {
    return this.http.post<Quotation>(this.baseUrl + '/quotations', payload);
  }
}
