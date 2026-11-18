import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import {
  CostSheet,
  Customer,
  MaterialDetails,
  MaterialRoute,
  MetalMaterial,
  MetalWeightResult,
  PoTrackerProcessOption,
  ProcessMaster,
  Quotation,
  QuotationItemSnapshot,
  QuotationItemWriteRequest,
  QuotationProcessSnapshot,
  Rfq,
  RfqItem,
  Vendor,
  VendorRate,
} from '../../core/api.models';
import { ApiService } from '../../core/api.service';
import { combineProcessOptions, QuotationProcessOption } from './quotation-process-options';

interface WorkspaceCostLine {
  sequence: number;
  processName?: string;
  category: string;
  description: string;
  amount: number;
  route?: MaterialRoute;
  rateType?: string;
  rate?: number;
  autoCost?: boolean;
}

interface QuotationItemDraft {
  request: QuotationItemWriteRequest;
  lineItem: string;
  materialNo: string;
  quantity: number;
  unit: string;
  totalPrice: number;
  editorState?: QuotationItemEditorState;
}

interface QuotationItemEditorState {
  engineering: {
    materialNo: string;
    shortDescription: string;
    metalId: number;
    shape: string;
    unit: string;
    densityKgM3: number;
    quantity: number;
    length: number;
    width: number;
    height: number;
    thickness: number;
    diameter: number;
    outerDiameter: number;
    innerDiameter: number;
    side: number;
    acrossFlats: number;
    materialRatePerKg: number;
    scrapWeight: number;
    scrapRecoveryRate: number;
  };
  materialRateInput: string;
  detectedMetal: string;
  master: MaterialDetails | null;
  weight: MetalWeightResult | null;
  rawMaterialCost: number;
  costSheet: CostSheet | null;
  processLines: WorkspaceCostLine[];
  profitPercent: number;
  transportPercent: number;
  rawMaterialDimensions: string | null;
  unitPriceOverride: number | null;
  quotationProcessRoute: QuotationProcessSnapshot[];
  calculatedWeightInputs: string;
  calculatedMaterialInputs: string;
}

@Component({
  selector: 'app-quotation-workspace',
  imports: [
    CurrencyPipe,
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './quotation-workspace.html',
  styleUrl: './quotation-workspace.scss',
})
export class QuotationWorkspace {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  protected readonly customers = signal<Customer[]>([]);
  protected readonly rfqs = signal<Rfq[]>([]);
  protected readonly items = signal<RfqItem[]>([]);
  protected readonly metals = signal<MetalMaterial[]>([]);
  protected readonly detectedMetal = signal('');
  protected readonly master = signal<MaterialDetails | null>(null);
  protected readonly weight = signal<MetalWeightResult | null>(null);
  protected readonly rawMaterialCost = signal(0);
  protected readonly costSheet = signal<CostSheet | null>(null);
  protected readonly processLines = signal<WorkspaceCostLine[]>([]);
  protected readonly quotationItems = signal<QuotationItemDraft[]>([]);
  protected readonly editingQuotationId = signal<number | null>(null);
  protected readonly editLoading = signal(false);
  protected readonly processOptions = signal<QuotationProcessOption[]>([]);
  protected readonly processOptionsLoading = signal(true);
  protected readonly processOptionsError = signal('');
  protected readonly selectedMasterProcessName = signal('');
  protected readonly selectedMasterProcessSource = signal<QuotationProcessOption['source'] | ''>('');
  protected readonly selectedMasterProcessVendorKey = signal('');
  protected readonly masterProcessNames = computed(() => {
    const processNames = new Map<string, string>();
    for (const option of this.processOptions()) {
      const key = option.processName.toLocaleLowerCase();
      if (!processNames.has(key)) processNames.set(key, option.processName);
    }
    return [...processNames.values()].sort((left, right) => left.localeCompare(right));
  });
  protected readonly masterProcessSources: QuotationProcessOption['source'][] = [
    'In-house',
    'Outsource',
  ];
  protected readonly masterProcessVendors = computed(() => {
    const processName = this.selectedMasterProcessName().toLocaleLowerCase();
    const vendors = new Map<string, QuotationProcessOption>();
    for (const option of this.processOptions()) {
      if (option.processName.toLocaleLowerCase() !== processName) continue;
      const key = JSON.stringify(option.vendorName.trim().toLocaleLowerCase());
      const existing = vendors.get(key);
      if (!existing || option.origin === 'ITP Quotation + POTracker') {
        vendors.set(key, option);
      }
    }
    return [...vendors.entries()].map(([key, option]) => ({
      key,
      vendorName: option.vendorName,
    }));
  });
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly manualRfqOpen = signal(false);
  protected manualRfq = {
    customerName: '',
    rfqNumber: '',
    rfqDate: '',
    lineItem: '',
    materialNo: '',
    description: '',
    quantity: 1,
    unit: 'Nos',
  };
  protected readonly shapes = [
    { value: 'RoundBar', label: 'Round bar', glyph: '●' },
    { value: 'SquareBar', label: 'Square bar', glyph: '■' },
    { value: 'Plate', label: 'Plate / flat', glyph: '▰' },
    { value: 'RoundTube', label: 'Round tube', glyph: '◉' },
    { value: 'SquareTube', label: 'Square tube', glyph: '▣' },
    { value: 'RectangularTube', label: 'Rect. tube', glyph: '▭' },
    { value: 'HexBar', label: 'Hex bar', glyph: '⬢' },
  ];
  protected readonly categories = [
    'Cutting',
    'Turning',
    'CNC',
    'Milling',
    'VTL',
    'Drilling',
    'Grinding',
    'Wire Cutting',
    'Heat Treatment',
    'Welding',
    'Painting',
    'Outsource',
    'Tooling',
    'Inspection',
    'Packing',
    'Transport',
    'Other',
  ];

  protected commercial = {
    quotationNumber: '',
    quotationDate: new Date().toISOString().slice(0, 10),
    validUntil: '',
    customerId: 0,
    rfqId: 0,
    rfqItemId: 0,
    paymentTerms: '30 days',
    deliveryTerms: 'Ex works',
    deliveryTime: '4 weeks',
    freightTerms: '',
    taxNotes: '',
  };

  protected selectedRfqItem(): RfqItem | undefined {
    return this.items().find(item => item.id === this.commercial.rfqItemId);
  }

  protected currentRawMaterialSize(): string {
    const enteredSize = this.rawMaterialDimensions()?.trim();
    if (enteredSize) return enteredSize;

    const masterSize = this.master()?.material.rawMaterialSize?.trim();
    if (masterSize) return masterSize;
    if (!this.commercial.rfqItemId) return '';

    const engineering = this.engineering;
    const formattedDimensions = (values: number[], prefixes: string[] = []): string => {
      const formatted = values.map((value, index) => {
        if (!Number.isFinite(value) || value <= 0) return '';
        return `${prefixes[index] ?? ''}${value}`;
      });
      return formatted.every(Boolean) ? `${formatted.join(' x ')} ${engineering.unit}` : '';
    };

    switch (engineering.shape) {
      case 'RoundBar':
        return formattedDimensions([engineering.diameter, engineering.length], ['Ø']);
      case 'SquareBar':
        return formattedDimensions([engineering.side, engineering.length]);
      case 'Plate':
        return formattedDimensions([
          engineering.length,
          engineering.width,
          engineering.thickness,
        ]);
      case 'RoundTube':
        return formattedDimensions(
          [engineering.outerDiameter, engineering.innerDiameter, engineering.length],
          ['OD', 'ID'],
        );
      case 'SquareTube':
        return formattedDimensions([
          engineering.side,
          engineering.side,
          engineering.thickness,
          engineering.length,
        ]);
      case 'RectangularTube':
        return formattedDimensions([
          engineering.width,
          engineering.height,
          engineering.thickness,
          engineering.length,
        ]);
      case 'HexBar':
        return formattedDimensions([engineering.acrossFlats, engineering.length], ['AF']);
      default:
        return '';
    }
  }

  protected engineering: QuotationItemEditorState['engineering'] = {
    materialNo: '',
    shortDescription: '',
    metalId: 0,
    shape: 'Plate',
    unit: 'mm',
    densityKgM3: 7850,
    quantity: 1,
    length: 1000,
    width: 100,
    height: 50,
    thickness: 10,
    diameter: 50,
    outerDiameter: 50,
    innerDiameter: 40,
    side: 50,
    acrossFlats: 50,
    materialRatePerKg: 0,
    scrapWeight: 0,
    scrapRecoveryRate: 0,
  };
  protected materialRateInput = '';

  protected setMaterialRateInput(value: string): void {
    this.materialRateInput = value;
    const rate = value.trim() ? Number(value) : 0;
    this.engineering.materialRatePerKg = Number.isFinite(rate) ? rate : 0;
  }

  private setMaterialRate(rate: number): void {
    this.engineering.materialRatePerKg = Number.isFinite(rate) ? rate : 0;
    this.materialRateInput = this.engineering.materialRatePerKg === 0
      ? ''
      : String(this.engineering.materialRatePerKg);
  }

  protected get specificGravity(): number {
    return Number(this.engineering.densityKgM3) / 1000;
  }

  protected set specificGravity(value: number) {
    this.engineering.densityKgM3 = Number(value) * 1000;
  }
  protected margins = { profitPercent: 10 };
  protected transportPercent = 5;
  protected quotationStatus = 'Draft';
  protected readonly unitPriceOverride = signal<number | null>(null);
  protected readonly rawMaterialDimensions = signal<string | null>(null);
  private readonly quotationProcessRoute = signal<QuotationProcessSnapshot[]>([]);
  private generatedQuotationNumber = '';
  private calculatedWeightInputs = '';
  private calculatedMaterialInputs = '';
  private savedCostInputs = '';
  private hasImportedDimensions = false;

  private weightInputs(): string {
    const { shape, unit, densityKgM3, quantity, length, width, height, thickness,
      diameter, outerDiameter, innerDiameter, side, acrossFlats } = this.engineering;
    return JSON.stringify({ shape, unit, densityKgM3, quantity, length, width, height,
      thickness, diameter, outerDiameter, innerDiameter, side, acrossFlats });
  }

  private materialInputs(): string {
    return JSON.stringify({ weight: this.weightInputs(), rate: this.engineering.materialRatePerKg,
      scrapWeight: this.engineering.scrapWeight,
      scrapRecoveryRate: this.engineering.scrapRecoveryRate });
  }

  private costInputs(): string {
    return JSON.stringify({ item: this.commercial.rfqItemId, material: this.materialInputs(),
      materialNo: this.engineering.materialNo, lines: this.processLines(), transportPercent: this.transportPercent,
      profit: this.margins.profitPercent });
  }

  protected cuttingCost(): number {
    return this.processLines()
      .filter(line => line.category === 'Cutting')
      .reduce((total, line) => total + this.lineAmount(line), 0);
  }

  protected processCost(): number {
    return this.processLines()
      .filter(line => line.category !== 'Cutting')
      .reduce((total, line) => total + this.lineAmount(line), 0);
  }

  protected basicCost(): number {
    return this.rawMaterialCost() + this.cuttingCost();
  }

  protected summaryProcessLines(): WorkspaceCostLine[] {
    return this.processLines().filter(line => line.category !== 'Cutting');
  }

  protected lineAmount(line: WorkspaceCostLine): number {
    if (!line.autoCost || !line.route) return Number(line.amount || 0);
    const route = line.route;
    const quantity = Number(this.engineering.quantity || 0);
    if (route.processType === 'InHouse') {
      return (Number(route.setupTimeHours) + Number(route.cycleTimeHours) * quantity) * Number(route.machineRate);
    }
    const rate = Number(line.rate ?? route.ratePerPiece);
    switch (line.rateType) {
      case 'PerKg': return rate * Number(this.weight()?.totalWeightKg || 0);
      case 'PerHour': return rate * (Number(route.setupTimeHours) + Number(route.cycleTimeHours) * quantity);
      case 'Fixed': return rate;
      default: return rate * quantity;
    }
  }

  protected setLineAmount(line: WorkspaceCostLine, amount: number): void {
    line.amount = Number(amount);
    line.autoCost = false;
  }

  protected transportCost(): number {
    return (
      this.rawMaterialCost() +
      this.cuttingCost() +
      this.processCost() +
      this.profitAmount()
    ) * Number(this.transportPercent || 0) / 100;
  }

  protected manufacturingCost(): number {
    return this.rawMaterialCost() + this.cuttingCost() + this.processCost();
  }
  protected profitAmount(): number {
    return this.processCost() * Number(this.margins.profitPercent || 0) / 100;
  }
  protected finalPrice(): number { return this.manufacturingCost() + this.profitAmount() + this.transportCost(); }
  protected unitPrice(): number {
    return this.engineering.quantity > 0 ? this.finalPrice() / this.engineering.quantity : 0;
  }

  constructor() {
    this.api.customers().subscribe((rows) => {
      this.customers.set(rows);
      const quotationId = Number(this.route.snapshot.queryParamMap.get('quotationId') ?? 0);
      const requestedRfq = Number(this.route.snapshot.queryParamMap.get('rfqId') ?? 0);
      if (quotationId) {
        this.loadQuotationForEdit(quotationId);
      } else if (requestedRfq) {
        this.api.rfqs().subscribe((rfqs) => {
          const rfq = rfqs.find((row) => row.id === requestedRfq);
          if (rfq) {
            this.commercial.customerId = rfq.customerId;
            this.onCustomer();
            this.commercial.rfqId = rfq.id;
            this.onRfq();
          }
        });
      }
    });
    this.refreshMetalData();
    this.loadProcessOptions();
  }

  private loadQuotationForEdit(quotationId: number): void {
    this.editLoading.set(true);
    this.error.set('');
    this.api.quotation(quotationId).subscribe({
      next: (quotation) => {
        if (quotation.items.length === 0) {
          this.error.set('This quotation has no items to edit.');
          this.editLoading.set(false);
          return;
        }

        const quotedItem = quotation.items[0];
        forkJoin({
          rfqs: this.api.rfqs(quotation.customerId),
          items: this.api.rfqItems(quotation.rfqId),
          metals: this.api.metals(),
          costSheet: this.api.costSheetByRfqItem(quotedItem.rfqItemId).pipe(
            catchError(() => of(null as CostSheet | null)),
          ),
          material: this.api.material(quotedItem.materialNo).pipe(
            catchError(() => of(null as MaterialDetails | null)),
          ),
        }).subscribe({
          next: ({ rfqs, items, metals, costSheet, material }) => {
            this.editingQuotationId.set(quotation.id);
            this.quotationStatus = quotation.status;
            this.rfqs.set(rfqs);
            this.items.set(items);
            this.quotationItems.set(quotation.items.map(item =>
              this.quotationItemDraftFromSnapshot(
                item,
                items.find(rfqItem => rfqItem.id === item.rfqItemId),
              ),
            ));
            this.metals.set(metals.filter((metal) => metal.isActive));
            const rfqItem = items.find((item) => item.id === quotedItem.rfqItemId);
            if (rfqItem) this.applyDetectedGeometry(rfqItem);
            this.commercial = {
              quotationNumber: quotation.quotationNumber,
              quotationDate: quotation.quotationDate.slice(0, 10),
              validUntil: quotation.validUntil?.slice(0, 10) ?? '',
              customerId: quotation.customerId,
              rfqId: quotation.rfqId,
              rfqItemId: quotedItem.rfqItemId,
              paymentTerms: quotation.paymentTerms ?? '',
              deliveryTerms: quotation.deliveryTerms ?? '',
              deliveryTime: quotation.deliveryTime ?? '',
              freightTerms: quotation.freightTerms ?? '',
              taxNotes: quotation.taxNotes ?? '',
            };
            this.engineering.materialNo = quotedItem.materialNo;
            this.engineering.shortDescription = quotedItem.description ?? '';
            this.engineering.quantity = quotedItem.quantity;
            this.engineering.densityKgM3 = quotedItem.densityKgM3 ?? 7850;
            this.setMaterialRate(quotedItem.materialRatePerKg ?? 0);
            this.engineering.shape = this.normalizeShape(quotedItem.rawMaterialShape);
            this.engineering.unit = rfqItem?.unit ?? 'mm';
            this.detectedMetal.set(
              rfqItem?.grade ?? '',
            );
            this.master.set(material);
            this.rawMaterialDimensions.set(
              quotedItem.rawMaterialDimensions ?? material?.material.rawMaterialSize ?? null,
            );
            this.rawMaterialCost.set(
              quotedItem.costBreakdown.categoryTotals['Raw Material'] ?? 0,
            );
            this.processLines.set(
              quotedItem.costBreakdown.lines
                .filter((line) =>
                  line.category.toLowerCase() !== 'raw material' &&
                  line.category.toLowerCase() !== 'transport',
                )
                .map((line) => {
                  const route = quotedItem.processRoute.find((entry) =>
                    line.description?.toLowerCase().includes(entry.processName.toLowerCase()),
                  );
                  return {
                    sequence: line.sequence,
                    processName: route?.processName ??
                      (line.category === 'Other' ? undefined : line.category),
                    category: line.category,
                    description: line.description ?? '',
                    amount: line.amount,
                  };
                }),
            );
            this.costSheet.set(costSheet);
            this.quotationProcessRoute.set(quotedItem.processRoute);
            this.margins.profitPercent = quotedItem.costBreakdown.profitPercent;
            const transportLine = quotedItem.costBreakdown.lines.find(
              (line) => line.category.toLowerCase() === 'transport',
            );
            const percentMatch = transportLine?.description?.match(/\\((\\d+(?:\\.\\d+)?)%\\)/);
            if (percentMatch) {
              this.transportPercent = Number(percentMatch[1]);
            } else {
              const transportBase =
                this.rawMaterialCost() +
                this.cuttingCost() +
                this.processCost() +
                quotedItem.costBreakdown.profitAmount;
              this.transportPercent = transportBase > 0
                ? (transportLine?.amount ?? 0) * 100 / transportBase
                : 0;
            }
            this.unitPriceOverride.set(
              quotedItem.unitPrice === quotedItem.costBreakdown.unitSellingPrice
                ? null
                : quotedItem.unitPrice,
            );
            this.weight.set({
              shape: quotedItem.rawMaterialShape ?? 'Plate',
              mode: 'LengthToWeight',
              unit: this.engineering.unit,
              densityKgM3: this.engineering.densityKgM3,
              crossSectionArea: 0,
              volumePerPiece: 0,
              weightPerPieceKg: quotedItem.weightPerPieceKg ?? 0,
              quantity: quotedItem.quantity,
              totalWeightKg: quotedItem.totalWeightKg ??
                (quotedItem.weightPerPieceKg ?? 0) * quotedItem.quantity,
            });
            this.calculatedWeightInputs = this.weightInputs();
            this.calculatedMaterialInputs = this.materialInputs();
            this.savedCostInputs = '';
            this.editLoading.set(false);
          },
          error: () => {
            this.error.set('Could not load quotation details for editing.');
            this.editLoading.set(false);
          },
        });
      },
      error: () => {
        this.error.set('Could not load quotation details for editing.');
        this.editLoading.set(false);
      },
    });
  }

  protected loadProcessOptions(): void {
    this.processOptionsLoading.set(true);
    this.processOptionsError.set('');
    const unavailableSources: string[] = [];
    forkJoin({
      processes: this.api.processes().pipe(catchError(() => {
        unavailableSources.push('ITP Quotation processes');
        return of([] as ProcessMaster[]);
      })),
      vendors: this.api.vendors().pipe(catchError(() => {
        unavailableSources.push('ITP Quotation vendors');
        return of([] as Vendor[]);
      })),
      rates: this.api.vendorRates().pipe(catchError(() => {
        unavailableSources.push('ITP Quotation vendor rates');
        return of([] as VendorRate[]);
      })),
      poTracker: this.api.poTrackerProcessOptions().pipe(catchError(() => {
        unavailableSources.push('POTracker');
        return of([] as PoTrackerProcessOption[]);
      })),
    }).subscribe({
      next: ({ processes, vendors, rates, poTracker }) => {
        this.processOptions.set(combineProcessOptions(processes, vendors, rates, poTracker));
        if (unavailableSources.length) {
          this.processOptionsError.set(
            `Could not load ${unavailableSources.join(', ')}; showing available options only.`,
          );
        }
        this.processOptionsLoading.set(false);
      },
      error: () => {
        this.processOptionsError.set(
          'Process/vendor options could not be loaded. You can still add costs manually.',
        );
        this.processOptionsLoading.set(false);
      },
    });
  }

  protected addSelectedMasterProcess(): void {
    const processName = this.selectedMasterProcessName();
    const source = this.selectedMasterProcessSource();
    const vendor = this.masterProcessVendors().find(
      (row) => row.key === this.selectedMasterProcessVendorKey(),
    );
    if (!processName || !source || (source === 'Outsource' && !vendor)) return;

    const processOptions = this.processOptions().filter(
      (option) =>
        option.processName.toLocaleLowerCase() === processName.toLocaleLowerCase(),
    );
    const matchingOptions = processOptions.filter(
      (option) =>
        source === 'In-house'
          ? option.source === 'In-house' && !option.vendorName.trim()
          : option.vendorName.trim().toLocaleLowerCase() ===
            vendor?.vendorName.trim().toLocaleLowerCase(),
    );
    const originOptions = matchingOptions.length ? matchingOptions : processOptions;
    const hasItpQuotation = originOptions.some(
      (option) => option.origin !== 'POTracker',
    );
    const hasPoTracker = originOptions.some(
      (option) => option.origin !== 'ITP Quotation',
    );
    const origin: QuotationProcessOption['origin'] = hasItpQuotation && hasPoTracker
      ? 'ITP Quotation + POTracker'
      : hasPoTracker
        ? 'POTracker'
        : 'ITP Quotation';

    const category =
      this.categories.find(
        (value) => value.toLowerCase() === processName.trim().toLowerCase(),
      ) ?? (source === 'Outsource' ? 'Outsource' : 'Other');
    const vendorName = vendor?.vendorName.trim() ?? '';
    const description =
      `${processName.trim()} - ${vendorName || 'No vendor'} (${source}; ${origin})`;
    this.processLines.set([
      ...this.processLines(),
      {
        sequence: this.processLines().length + 2,
        processName: processName.trim(),
        category,
        description,
        amount: 0,
      },
    ]);
    this.selectedMasterProcessName.set('');
    this.selectedMasterProcessSource.set('');
    this.selectedMasterProcessVendorKey.set('');
  }

  protected canAddSelectedMasterProcess(): boolean {
    if (!this.selectedMasterProcessName() || !this.selectedMasterProcessSource()) return false;
    return this.selectedMasterProcessSource() === 'In-house' ||
      this.masterProcessVendors().some(
        (vendor) => vendor.key === this.selectedMasterProcessVendorKey(),
      );
  }

  protected onMasterProcessChanged(processName: string): void {
    this.selectedMasterProcessName.set(processName);
    this.selectedMasterProcessSource.set('');
    this.selectedMasterProcessVendorKey.set('');
  }

  protected onMasterProcessSourceChanged(
    source: QuotationProcessOption['source'] | '',
  ): void {
    this.selectedMasterProcessSource.set(source);
    this.selectedMasterProcessVendorKey.set('');
  }

  protected addManualRfq(): void {
    if (this.busy()) return;
    const entry = this.manualRfq;
    const quantity = Number(entry.quantity);
    if ((!this.commercial.customerId && !entry.customerName.trim()) ||
        !entry.rfqNumber.trim() || !entry.materialNo.trim() || !entry.unit.trim() ||
        !Number.isFinite(quantity) || quantity <= 0 || Number(quantity.toFixed(3)) !== quantity) {
      this.error.set('Enter a customer, RFQ number, material number, unit, and a positive quantity with up to three decimal places.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.api.confirmRfqImport({
      rfqNumber: entry.rfqNumber.trim(),
      customerId: entry.customerName.trim() ? 0 : this.commercial.customerId,
      customerName: entry.customerName.trim() || null,
      rfqDate: entry.rfqDate || null,
      status: 'Draft',
      items: [{
        lineItem: entry.lineItem.trim() || null,
        materialNo: entry.materialNo.trim(),
        description: entry.description.trim() || null,
        quantity,
        unit: entry.unit.trim(),
      }],
    }).subscribe({
      next: result => {
        forkJoin({
          customers: this.api.customers(),
          rfqs: this.api.rfqs(),
          items: this.api.rfqItems(result.rfqId),
        }).subscribe({
          next: ({ customers, rfqs, items }) => {
            const rfq = rfqs.find(row => row.id === result.rfqId);
            this.customers.set(customers);
            this.commercial.customerId = rfq?.customerId ?? 0;
            this.rfqs.set(rfqs.filter(row => row.customerId === this.commercial.customerId));
            this.commercial.rfqId = result.rfqId;
            this.items.set(items);
            this.commercial.rfqItemId = items[0]?.id ?? 0;
            this.manualRfqOpen.set(false);
            this.busy.set(false);
            if (this.commercial.rfqItemId) this.onItem();
            this.snack.open('RFQ details added to this quotation', 'Dismiss', { duration: 3200 });
          },
          error: () => {
            this.busy.set(false);
            this.error.set('RFQ was saved, but could not be loaded. Refresh the page and select it.');
          },
        });
      },
      error: (error: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(error.error?.detail ?? 'Could not add the RFQ details.');
      },
    });
  }

  protected onCustomer(): void {
    this.costSheet.set(null);
    this.quotationItems.set([]);
    this.savedCostInputs = '';
    this.commercial.rfqId = 0;
    this.commercial.rfqItemId = 0;
    this.items.set([]);
    this.api.rfqs(this.commercial.customerId).subscribe((rows) => this.rfqs.set(rows));
  }

  protected onRfq(): void {
    this.costSheet.set(null);
    this.quotationItems.set([]);
    this.savedCostInputs = '';
    this.commercial.rfqItemId = 0;
    if (!this.commercial.rfqId) return;
    this.api.rfqItems(this.commercial.rfqId).subscribe((rows) => {
      this.items.set(rows);
      if (rows.length === 1) {
        this.commercial.rfqItemId = rows[0].id;
        this.onItem();
      }
    });
  }

  protected onItem(): void {
    const item = this.items().find((row) => row.id === this.commercial.rfqItemId);
    if (!item) return;
    const stagedItem = this.quotationItems().find(
      draft => draft.request.rfqItemId === item.id,
    );
    if (stagedItem?.editorState) {
      this.restoreEditorState(stagedItem.editorState);
      return;
    }

    this.costSheet.set(null);
    this.savedCostInputs = '';
    this.master.set(null);
    this.costSheet.set(null);
    this.rawMaterialDimensions.set(null);
    this.rawMaterialCost.set(0);
    this.weight.set(null);
    this.processLines.set([]);
    this.transportPercent = 5;
    this.margins.profitPercent = 10;
    this.unitPriceOverride.set(null);
    this.quotationProcessRoute.set([]);
    this.engineering.scrapWeight = 0;
    this.engineering.scrapRecoveryRate = 0;
    this.engineering.materialNo = item.materialNo;
    this.engineering.shortDescription = this.conciseDescription(item.description);
    this.engineering.quantity = item.quantity;
    this.applyDetectedGeometry(item);
    this.detectedMetal.set(item.grade?.trim() ?? 'Material not detected');
    this.generateQuotationNumber(item);
    this.refreshMetalData(item);
    this.api.costSheetByRfqItem(item.id).subscribe({
      next: sheet => {
        if (this.commercial.rfqItemId === item.id) this.applyCostSheet(sheet);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status !== 404) {
          this.error.set('Saved cost sheet could not be loaded for this RFQ item.');
        }
      },
    });
    this.loadMaterial(true);
  }

  protected onMetal(): void {
    const metal = this.metals().find((row) => row.id === this.engineering.metalId);
    if (!metal) return;
    this.engineering.densityKgM3 = metal.densityKgM3;
    this.setMaterialRate(metal.defaultRatePerKg);
    this.calculateImportedWeight();
  }

  protected refreshMetalData(item?: RfqItem): void {
    this.api.metals().subscribe({
      next: (rows) => {
        this.metals.set(rows.filter((metal) => metal.isActive));
        const selectedItem =
          item ?? this.items().find((row) => row.id === this.commercial.rfqItemId);
        if (selectedItem) this.applyDetectedMetal(selectedItem);
      },
      error: () => this.snack.open('Metal rates could not be refreshed', 'Dismiss', { duration: 3000 }),
    });
  }

  protected loadMaterial(loadSavedCostSheet = false): void {
    if (!this.engineering.materialNo.trim()) return;
    forkJoin({
      details: this.api.material(this.engineering.materialNo),
      rates: this.api.vendorRates().pipe(catchError(() => of([] as VendorRate[]))),
    }).subscribe({
      next: ({ details, rates }) => {
        this.master.set(details);
        if (this.rawMaterialDimensions() === null) {
          this.rawMaterialDimensions.set(details.material.rawMaterialSize?.trim() || null);
        }
        this.engineering.shortDescription =
          details.material.shortDescription?.trim() || this.engineering.shortDescription;
        this.engineering.metalId = details.material.metalMaterialId ?? 0;
        this.engineering.densityKgM3 =
          details.material.metalMaterial?.densityKgM3 ?? this.engineering.densityKgM3;
        this.setMaterialRate(
          details.material.metalMaterial?.defaultRatePerKg ??
          this.engineering.materialRatePerKg,
        );
        if (details.material.rawMaterialShape?.trim()) {
          this.engineering.shape = this.normalizeShape(details.material.rawMaterialShape);
        }
        this.calculateImportedWeight();
        const today = new Date().toISOString().slice(0, 10);
        const routingLines = details.routing.map((route, index) => {
          const currentRate = rates
            .filter(rate => rate.vendorId === route.vendorId && rate.processId === route.processId &&
              rate.effectiveFrom.slice(0, 10) <= today &&
              (!rate.effectiveTo || rate.effectiveTo.slice(0, 10) >= today))
            .sort((a, b) => Date.parse(b.effectiveFrom) - Date.parse(a.effectiveFrom))[0];
          return {
            sequence: index + 2,
            processName: route.processName,
            category: this.categoryFor(route.processName, route.processType),
            description: route.processName,
            amount: 0,
            route,
            rateType: route.processType === 'Outsource' ? currentRate?.rateType ?? 'PerPiece' : 'PerHour',
            rate: route.processType === 'Outsource' ? currentRate?.rate ?? route.ratePerPiece : route.machineRate,
            autoCost: true,
          };
        });
        if (loadSavedCostSheet && this.costSheet()) {
          this.applyCostSheet(this.costSheet()!);
        } else {
          this.processLines.set(routingLines);
        }
        this.snack.open('Material master loaded', 'Dismiss', { duration: 2200 });
      },
      error: () => {
        this.master.set(null);
        const item = this.items().find((row) => row.id === this.commercial.rfqItemId);
        if (item) this.applyDetectedMetal(item);
        this.snack.open('No material master found; using the RFQ grade and editable values', 'Dismiss', {
          duration: 3200,
        });
      },
    });
  }

  private applyCostSheet(sheet: CostSheet): void {
    this.costSheet.set(sheet);
    this.rawMaterialCost.set(sheet.categoryTotals['Raw Material'] ?? 0);
    this.processLines.set(
      sheet.lines
        .filter(line =>
          line.category.toLowerCase() !== 'raw material' &&
          line.category.toLowerCase() !== 'transport',
        )
        .map(line => ({
          sequence: line.sequence,
          processName: line.category === 'Other' ? undefined : line.category,
          category: line.category,
          description: line.description ?? '',
          amount: line.amount,
        })),
    );
    this.margins.profitPercent = sheet.profitPercent;
    const transportLine = sheet.lines.find(
      line => line.category.toLowerCase() === 'transport',
    );
    const percentMatch = transportLine?.description?.match(/\((\d+(?:\.\d+)?)%\)/);
    if (percentMatch) {
      this.transportPercent = Number(percentMatch[1]);
    } else {
      const transportBase =
        this.rawMaterialCost() +
        this.cuttingCost() +
        this.processCost() +
        this.profitAmount();
      this.transportPercent = transportBase > 0
        ? (transportLine?.amount ?? 0) * 100 / transportBase
        : 5;
    }
    this.savedCostInputs = this.costInputs();
  }

  private captureEditorState(): QuotationItemEditorState {
    return {
      engineering: { ...this.engineering },
      materialRateInput: this.materialRateInput,
      detectedMetal: this.detectedMetal(),
      master: this.master(),
      weight: this.weight(),
      rawMaterialCost: this.rawMaterialCost(),
      costSheet: this.costSheet(),
      processLines: this.processLines().map(line => ({ ...line })),
      profitPercent: this.margins.profitPercent,
      transportPercent: this.transportPercent,
      rawMaterialDimensions: this.rawMaterialDimensions(),
      unitPriceOverride: this.unitPriceOverride(),
      quotationProcessRoute: this.quotationProcessRoute(),
      calculatedWeightInputs: this.calculatedWeightInputs,
      calculatedMaterialInputs: this.calculatedMaterialInputs,
    };
  }

  private restoreEditorState(state: QuotationItemEditorState): void {
    this.engineering = { ...state.engineering };
    this.materialRateInput = state.materialRateInput;
    this.detectedMetal.set(state.detectedMetal);
    this.master.set(state.master);
    this.weight.set(state.weight);
    this.rawMaterialCost.set(state.rawMaterialCost);
    this.costSheet.set(state.costSheet);
    this.processLines.set(state.processLines.map(line => ({ ...line })));
    this.margins.profitPercent = state.profitPercent;
    this.transportPercent = state.transportPercent;
    this.rawMaterialDimensions.set(state.rawMaterialDimensions);
    this.unitPriceOverride.set(state.unitPriceOverride);
    this.quotationProcessRoute.set(state.quotationProcessRoute);
    this.calculatedWeightInputs = state.calculatedWeightInputs;
    this.calculatedMaterialInputs = state.calculatedMaterialInputs;
    this.savedCostInputs = this.costInputs();
  }

  protected calculateWeight(): void {
    this.busy.set(true);
    const e = this.engineering;
    const inputs = this.weightInputs();
    const payload = {
      shape: e.shape,
      unit: e.unit,
      mode: 'LengthToWeight',
      densityKgM3: Number(e.densityKgM3),
      quantity: Number(e.quantity),
      length: Number(e.length),
      width: Number(e.width),
      height: Number(e.height),
      thickness: e.shape === 'RoundTube' ? undefined : Number(e.thickness),
      diameter: Number(e.diameter),
      outerDiameter: Number(e.outerDiameter),
      innerDiameter: e.shape === 'RoundTube' ? Number(e.innerDiameter) : undefined,
      side: Number(e.side),
      acrossFlats: Number(e.acrossFlats),
    };
    this.api.calculateMetalWeight(payload).subscribe({
      next: (result) => {
        if (inputs !== this.weightInputs()) { this.busy.set(false); return; }
        this.weight.set(result);
        this.calculatedWeightInputs = inputs;
        this.busy.set(false);
        this.calculateMaterialCost();
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Check the dimensions and tube geometry.');
      },
    });
  }

  protected calculateMaterialCost(): void {
    const weight = this.weight();
    if (!weight) return;
    if (this.calculatedWeightInputs !== this.weightInputs()) {
      this.snack.open('Calculate weight again after changing material or dimensions', 'Dismiss', { duration: 3500 });
      return;
    }
    const inputs = this.materialInputs();
    this.api
      .calculateRawMaterial({
        weightPerPiece: weight.weightPerPieceKg,
        quantity: Number(this.engineering.quantity),
        materialRatePerKg: Number(this.engineering.materialRatePerKg),
        scrapWeight: Number(this.engineering.scrapWeight),
        scrapRecoveryRate: Number(this.engineering.scrapRecoveryRate),
      })
      .subscribe({
        next: (result) => {
          if (inputs !== this.materialInputs()) return;
          this.rawMaterialCost.set(result.netMaterialCost);
          this.calculatedMaterialInputs = inputs;
        },
        error: () => this.error.set('Raw material costing could not be calculated.'),
      });
  }

  protected addLine(index: number): void {
    const lines = [...this.processLines()];
    const insertionIndex = Math.max(0, Math.min(index + 1, lines.length));
    lines.splice(insertionIndex, 0, {
      sequence: 0,
      category: 'Other',
      description: '',
      amount: 0,
    });
    this.processLines.set(
      lines.map((line, current) => ({ ...line, sequence: current + 2 })),
    );
  }

  protected removeLine(index: number): void {
    this.processLines.set(
      this.processLines()
        .filter((_, current) => current !== index)
        .map((line, current) => ({ ...line, sequence: current + 2 })),
    );
  }

  protected saveCostSheet(): void {
    if (!this.commercial.rfqItemId) {
      this.snack.open('Select an RFQ item first', 'Dismiss', { duration: 2800 });
      return;
    }
    if (this.calculatedWeightInputs !== this.weightInputs() ||
        this.calculatedMaterialInputs !== this.materialInputs()) {
      this.snack.open('Recalculate weight and material cost before saving', 'Dismiss', { duration: 4000 });
      return;
    }
    if (Number(this.engineering.materialRatePerKg) <= 0) {
      this.snack.open('Enter the material rate per kg before saving', 'Dismiss', { duration: 4000 });
      return;
    }
    if (!Number.isFinite(Number(this.transportPercent)) || Number(this.transportPercent) < 0 || Number(this.transportPercent) > 100) {
      this.snack.open('Transport percentage must be between 0 and 100', 'Dismiss', { duration: 4000 });
      return;
    }
    const inputs = this.costInputs();
    const lines = [
      {
        sequence: 1,
        category: 'Raw Material',
        description: this.engineering.materialNo || 'Raw material',
        amount: this.rawMaterialCost(),
      },
      ...this.processLines().map((line, index) => ({
        sequence: index + 2,
        category: line.category,
        description: line.description,
        amount: this.lineAmount(line),
      })),
      ...(this.transportCost() > 0 ? [{
        sequence: this.processLines().length + 2,
        category: 'Transport',
        description: `Transport (${this.transportPercent}%)`,
        amount: this.transportCost(),
      }] : []),
    ];
    this.busy.set(true);
    const costSheet = this.costSheet();
    const payload = {
        quantity: Number(this.engineering.quantity),
        overheadPercent: 0,
        profitPercent: Number(this.margins.profitPercent),
        lines,
    };
    const saveRequest = costSheet
      ? this.api.updateCostSheet(costSheet.id, payload)
      : this.api.saveCostSheet({ ...payload, rfqItemId: this.commercial.rfqItemId });
    saveRequest.subscribe({
        next: (sheet) => {
          this.costSheet.set(sheet);
          this.savedCostInputs = inputs;
          this.busy.set(false);
          this.snack.open('Cost sheet saved', 'Dismiss', { duration: 2400 });
        },
        error: () => {
          this.busy.set(false);
          this.snack.open('Cost sheet already exists or could not be saved', 'Dismiss', {
            duration: 3800,
          });
        },
      });
  }

  protected isQuotationItemStaged(rfqItemId: number): boolean {
    return this.quotationItems().some(item => item.request.rfqItemId === rfqItemId);
  }

  protected isCurrentItemStaged(): boolean {
    return this.isQuotationItemStaged(this.commercial.rfqItemId);
  }

  protected canStageCurrentItem(): boolean {
    return !!this.selectedRfqItem() &&
      !!this.costSheet() &&
      this.savedCostInputs === this.costInputs();
  }

  protected canSwitchRfqItem(): boolean {
    if (!this.commercial.rfqItemId) return true;
    const stagedItem = this.quotationItems().find(
      item => item.request.rfqItemId === this.commercial.rfqItemId,
    );
    const currentItem = this.currentQuotationItemDraft();
    return !!stagedItem &&
      this.savedCostInputs === this.costInputs() &&
      !!currentItem &&
      JSON.stringify(stagedItem.request) === JSON.stringify(currentItem.request);
  }

  protected canCreateQuotation(): boolean {
    if (this.costSheet() && this.savedCostInputs !== this.costInputs()) return false;
    return this.quotationItems().length > 0 || this.canStageCurrentItem();
  }

  protected stageCurrentItem(): void {
    if (!this.canStageCurrentItem()) {
      this.snack.open('Save the current cost sheet before adding this item', 'Dismiss', {
        duration: 3600,
      });
      return;
    }

    const draft = this.currentQuotationItemDraft();
    if (!draft) return;
    const alreadyStaged = this.isCurrentItemStaged();
    this.quotationItems.set(
      this.upsertQuotationItem(this.quotationItems(), draft),
    );
    this.snack.open(
      `${draft.materialNo} ${alreadyStaged ? 'updated in' : 'added to'} the quotation`,
      'Dismiss',
      { duration: 2800 },
    );
  }

  protected removeQuotationItem(rfqItemId: number): void {
    this.quotationItems.update(items =>
      items.filter(item => item.request.rfqItemId !== rfqItemId),
    );
    if (this.commercial.rfqItemId === rfqItemId) {
      this.commercial.rfqItemId = 0;
      this.costSheet.set(null);
      this.rawMaterialCost.set(0);
      this.weight.set(null);
      this.processLines.set([]);
      this.savedCostInputs = '';
    }
  }

  protected discardCurrentItem(): void {
    if (this.isCurrentItemStaged()) return;
    this.commercial.rfqItemId = 0;
    this.master.set(null);
    this.weight.set(null);
    this.rawMaterialCost.set(0);
    this.costSheet.set(null);
    this.processLines.set([]);
    this.rawMaterialDimensions.set(null);
    this.unitPriceOverride.set(null);
    this.quotationProcessRoute.set([]);
    this.savedCostInputs = '';
  }

  private currentQuotationItemDraft(): QuotationItemDraft | null {
    const item = this.selectedRfqItem();
    if (!item) return null;

    const override = this.unitPriceOverride();
    const totalPrice = override !== null
      ? override * this.engineering.quantity
      : this.costSheet()?.sellingPrice ?? this.finalPrice();

    return {
      request: this.currentQuotationItemRequest(),
      lineItem: item.lineItem?.trim() || '—',
      materialNo: item.materialNo,
      quantity: this.engineering.quantity,
      unit: item.unit,
      totalPrice,
      editorState: this.captureEditorState(),
    };
  }

  private quotationItemDraftFromSnapshot(
    snapshot: QuotationItemSnapshot,
    rfqItem?: RfqItem,
  ): QuotationItemDraft {
    const unitPriceOverride =
      snapshot.unitPrice === snapshot.costBreakdown.unitSellingPrice
        ? undefined
        : snapshot.unitPrice;

    return {
      request: {
        rfqItemId: snapshot.rfqItemId,
        description: snapshot.description ?? null,
        materialRatePerKg: snapshot.materialRatePerKg ?? null,
        densityKgM3: snapshot.densityKgM3 ?? null,
        rawMaterialShape: snapshot.rawMaterialShape ?? null,
        rawMaterialDimensions: snapshot.rawMaterialDimensions ?? null,
        weightPerPieceKg: snapshot.weightPerPieceKg ?? null,
        totalWeightKg: snapshot.totalWeightKg ?? null,
        unitPriceOverride,
        processRoute: snapshot.processRoute,
      },
      lineItem: rfqItem?.lineItem?.trim() || '—',
      materialNo: snapshot.materialNo,
      quantity: snapshot.quantity,
      unit: rfqItem?.unit || 'Nos',
      totalPrice: snapshot.totalPrice,
    };
  }

  private currentQuotationItemRequest(): QuotationItemWriteRequest {
    return {
      rfqItemId: this.commercial.rfqItemId,
      description: this.engineering.shortDescription.trim() || null,
      materialRatePerKg: Number(this.engineering.materialRatePerKg),
      densityKgM3: Number(this.engineering.densityKgM3),
      rawMaterialShape: this.engineering.shape,
      rawMaterialDimensions: this.currentRawMaterialSize() || null,
      weightPerPieceKg: this.weight()?.weightPerPieceKg,
      totalWeightKg: this.weight()?.totalWeightKg,
      unitPriceOverride: this.unitPriceOverride() ?? undefined,
      processRoute: this.editingQuotationId()
        ? this.quotationProcessRoute()
        : this.processLines()
            .filter(line => !!line.route)
            .map(line => ({
              sequence: line.route!.sequence,
              processName: line.route!.processName,
              processType: line.route!.processType,
              vendorName: line.route!.vendorName ?? undefined,
              rateType: line.rateType ?? undefined,
              rate: Number(line.rate ?? 0),
              machineRate: line.route!.machineRate,
              setupTimeHours: line.route!.setupTimeHours,
              cycleTimeHours: line.route!.cycleTimeHours,
              cost: this.lineAmount(line),
            })),
    };
  }

  private upsertQuotationItem(
    items: QuotationItemDraft[],
    draft: QuotationItemDraft,
  ): QuotationItemDraft[] {
    const updatedItems = [...items];
    const existingIndex = updatedItems.findIndex(
      item => item.request.rfqItemId === draft.request.rfqItemId,
    );
    if (existingIndex < 0) {
      updatedItems.push(draft);
    } else {
      updatedItems[existingIndex] = draft;
    }
    return updatedItems;
  }

  protected createQuotation(): void {
    if (!this.commercial.quotationNumber.trim()) {
      this.snack.open('Enter a quotation number', 'Dismiss', {
        duration: 3300,
      });
      return;
    }
    if (this.costSheet() && this.savedCostInputs !== this.costInputs()) {
      this.snack.open('Cost inputs changed. Recalculate and save the cost sheet again', 'Dismiss', { duration: 4000 });
      return;
    }
    if (
      this.commercial.validUntil &&
      this.commercial.validUntil < this.commercial.quotationDate
    ) {
      this.snack.open('Valid-until date cannot be earlier than quotation date', 'Dismiss', {
        duration: 3600,
      });
      return;
    }

    const currentDraft = this.canStageCurrentItem()
      ? this.currentQuotationItemDraft()
      : null;
    if (currentDraft) {
      this.quotationItems.set(
        this.upsertQuotationItem(this.quotationItems(), currentDraft),
      );
    }
    const items = this.quotationItems();
    if (items.length === 0) {
      this.snack.open('Add at least one costed RFQ item to the quotation', 'Dismiss', {
        duration: 3600,
      });
      return;
    }

    const today = new Date().toISOString().slice(0, 10);
    const payload = {
      quotationDate: this.commercial.quotationDate || today,
      validUntil: this.commercial.validUntil || null,
      paymentTerms: this.commercial.paymentTerms || null,
      deliveryTerms: this.commercial.deliveryTerms || null,
      deliveryTime: this.commercial.deliveryTime || null,
      freightTerms: this.commercial.freightTerms || null,
      taxNotes: this.commercial.taxNotes || null,
      status: this.quotationStatus,
      items: items.map(item => item.request),
    };
    const editingId = this.editingQuotationId();
    this.busy.set(true);
    const saveRequest = editingId
      ? this.api.updateQuotation(editingId, payload)
      : this.api.createQuotation({
          ...payload,
          quotationNumber: this.commercial.quotationNumber,
          customerId: this.commercial.customerId,
          rfqId: this.commercial.rfqId,
        });
    saveRequest.subscribe({
        next: (quotation) => {
          this.busy.set(false);
          this.snack.open(
            editingId
              ? `Quotation ${quotation.quotationNumber} saved as revision ${quotation.revision}`
              : 'Quotation ' + quotation.quotationNumber + ' created',
            'Dismiss',
            { duration: 4200 },
          );
          if (editingId) void this.router.navigateByUrl('/quotations');
        },
        error: () => {
          this.busy.set(false);
          this.snack.open(
            editingId ? 'Quotation revision could not be saved' : 'Quotation could not be created',
            'Dismiss',
            { duration: 3800 },
          );
        },
      });
  }

  protected cancelEditing(): void {
    void this.router.navigateByUrl('/quotations');
  }

  private normalizeShape(shape?: string): string {
    const match = this.shapes.find(
      (item) => item.value.toLowerCase() === (shape ?? '').replace(/\s/g, '').toLowerCase(),
    );
    return match?.value ?? 'Plate';
  }

  private applyDetectedMetal(item: RfqItem): void {
    const grade = item.grade?.trim() || this.gradeFromDescription(item.description);
    this.detectedMetal.set(grade || 'Material not detected');
    if (!grade) return;

    const normalizedGrade = this.normalizeMaterialText(grade);
    const family = normalizedGrade.includes('stainlesssteel') ? 'ss'
      : normalizedGrade.includes('carbonsteel') || normalizedGrade.includes('mildsteel') ? 'ms'
      : normalizedGrade.includes('castiron') ? 'castiron'
      : normalizedGrade.includes('aluminum') ? 'aluminium'
      : normalizedGrade;
    const metal = this.metals().find((candidate) => {
      const candidateText = this.normalizeMaterialText(
        `${candidate.name} ${candidate.grade ?? ''}`,
      );
      return (
        candidateText === family ||
        candidateText.includes(family) ||
        family.includes(candidateText)
      );
    });
    if (!metal) {
      this.engineering.metalId = 0;
      return;
    }

    this.engineering.metalId = metal.id;
    this.engineering.densityKgM3 = metal.densityKgM3;
    this.setMaterialRate(metal.defaultRatePerKg);
    this.calculateImportedWeight();
  }

  private calculateImportedWeight(): void {
    if (this.hasImportedDimensions && Number(this.engineering.densityKgM3) > 0) {
      this.calculateWeight();
    }
  }

  private generateQuotationNumber(item: RfqItem): void {
    if (
      this.commercial.quotationNumber.trim() &&
      this.commercial.quotationNumber !== this.generatedQuotationNumber
    ) {
      return;
    }

    const date = new Date();
    const datePart = [
      date.getFullYear(),
      String(date.getMonth() + 1).padStart(2, '0'),
      String(date.getDate()).padStart(2, '0'),
    ].join('');
    this.generatedQuotationNumber =
      `QTN-${datePart}-${this.commercial.rfqId}-${item.materialNo}`;
    this.commercial.quotationNumber = this.generatedQuotationNumber;
  }

  private normalizeMaterialText(value: string): string {
    return value.toLowerCase().replace(/[^a-z0-9]/g, '');
  }

  private conciseDescription(value?: string): string {
    if (!value?.trim()) return '';
    const concise = value.split(' - ', 1)[0].trim();
    return concise.slice(0, 500);
  }

  private applyDetectedGeometry(item: RfqItem): void {
    const fullDescription = item.description ?? '';
    const description = `${this.conciseDescription(fullDescription)} ${item.dimensions ?? ''}`.toLowerCase();
    if (/\b(square\s*tube|box\s*section)\b/.test(description)) {
      this.engineering.shape = 'SquareTube';
    } else if (/\b(rectangular\s*tube|rect\.?\s*tube)\b/.test(description)) {
      this.engineering.shape = 'RectangularTube';
    } else if (/\b(plate|flat|sheet)\b/.test(description)) {
      this.engineering.shape = 'Plate';
    } else if (/\b(ring|pipe|round\s*tube|hollow\s*round)\b/.test(description)) {
      this.engineering.shape = 'RoundTube';
    } else if (/\b(round\s*bar|rod|shaft)\b/.test(description)) {
      this.engineering.shape = 'RoundBar';
    } else if (/\b(square\s*bar)\b/.test(description)) {
      this.engineering.shape = 'SquareBar';
    } else if (/\b(hex|hexagonal)\b/.test(description)) {
      this.engineering.shape = 'HexBar';
    }

    const dimensions = (item.dimensions ?? '').match(/\d+(?:\.\d+)?/g)?.map(Number) ?? [];
    this.hasImportedDimensions = dimensions.length >= 2 &&
      (this.engineering.shape !== 'Plate' && this.engineering.shape !== 'SquareTube' &&
       this.engineering.shape !== 'RectangularTube' || dimensions.length >= 3 ||
       this.numberAfter(fullDescription, /(?:ENTRY\s+PLATE\s+)?THICKNESS\s*:/i) !== null);
    if (this.engineering.shape === 'RoundTube' && dimensions.length >= 2) {
      this.engineering.outerDiameter = dimensions[0];
      this.engineering.innerDiameter = dimensions[1];
      if (dimensions.length >= 3) this.engineering.length = dimensions[2];
    } else if (this.engineering.shape === 'RoundBar' && dimensions.length >= 1) {
      this.engineering.diameter = dimensions[0];
      if (dimensions.length >= 2) this.engineering.length = dimensions[1];
    } else if (this.engineering.shape === 'SquareBar' && dimensions.length >= 1) {
      this.engineering.side = dimensions[0];
      if (dimensions.length >= 2) this.engineering.length = dimensions[1];
    } else if (this.engineering.shape === 'HexBar' && dimensions.length >= 1) {
      this.engineering.acrossFlats = dimensions[0];
      if (dimensions.length >= 2) this.engineering.length = dimensions[1];
    } else if (dimensions.length >= 2) {
      this.engineering.length =
        this.numberAfter(fullDescription, /(?:ENTRY\s+PLATE\s+)?LENGTH\s*:/i) ?? dimensions[0];
      this.engineering.width =
        this.numberAfter(fullDescription, /(?:ENTRY\s+PLATE\s+)?WIDTH\s*:/i) ?? dimensions[1];
      this.engineering.thickness =
        this.numberAfter(fullDescription, /(?:ENTRY\s+PLATE\s+)?THICKNESS\s*:/i) ??
        (dimensions.length >= 3 ? dimensions[2] : this.engineering.thickness);
    }
  }

  private gradeFromDescription(value?: string): string {
    if (!value) return '';
    return (
      value.match(
        /\b(?:CARBON\s+STEEL|CAST\s+IRON|STAINLESS\s+STEEL|ALUMINIUM|ALUMINUM|BRASS|COPPER|NYLON)(?:\s+SAE)?\s+[A-Z0-9][A-Z0-9./-]*/i,
      )?.[0] ?? value.match(/\b(?:MS|SS|CAST\s+IRON|ALUMINIUM|ALUMINUM|BRASS|COPPER|NYLON)\b/i)?.[0] ?? ''
    ).toUpperCase();
  }

  private numberAfter(value: string, label: RegExp): number | null {
    const index = value.search(label);
    if (index < 0) return null;
    const match = value.slice(index).match(/:\s*(\d+(?:\.\d+)?)/);
    return match ? Number(match[1]) : null;
  }

  private categoryFor(process: string, type: string): string {
    if (type === 'Outsource') return 'Outsource';
    return this.categories.find((value) => process.toLowerCase().includes(value.toLowerCase())) ?? 'Other';
  }
}
