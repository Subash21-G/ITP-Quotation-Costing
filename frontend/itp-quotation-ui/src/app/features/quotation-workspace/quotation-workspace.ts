import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import {
  CostSheet,
  Customer,
  MaterialDetails,
  MetalMaterial,
  MetalWeightResult,
  Rfq,
  RfqItem,
} from '../../core/api.models';
import { ApiService } from '../../core/api.service';

interface WorkspaceCostLine {
  sequence: number;
  category: string;
  description: string;
  amount: number;
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
  protected readonly busy = signal(false);
  protected readonly error = signal('');
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
    customerId: 0,
    rfqId: 0,
    rfqItemId: 0,
    paymentTerms: '30 days',
    deliveryTerms: 'Ex works',
    deliveryTime: '4 weeks',
  };
  protected engineering = {
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
  protected get specificGravity(): number {
    return Number(this.engineering.densityKgM3) / 1000;
  }

  protected set specificGravity(value: number) {
    this.engineering.densityKgM3 = Number(value) * 1000;
  }
  protected margins = { overheadPercent: 10, profitPercent: 15 };
  protected transportCost = 0;
  protected newLine: WorkspaceCostLine = {
    sequence: 1,
    category: 'CNC',
    description: '',
    amount: 0,
  };
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
      materialNo: this.engineering.materialNo, lines: this.processLines(), transportCost: this.transportCost,
      overhead: this.margins.overheadPercent,
      profit: this.margins.profitPercent });
  }

  protected processCost(): number {
    return this.processLines().reduce((total, line) => total + Number(line.amount || 0), 0);
  }

  protected manufacturingCost(): number {
    return this.rawMaterialCost() + this.processCost() + Number(this.transportCost || 0);
  }
  protected overheadAmount(): number {
    return this.manufacturingCost() * Number(this.margins.overheadPercent || 0) / 100;
  }
  protected costAfterOverhead(): number { return this.manufacturingCost() + this.overheadAmount(); }
  protected profitAmount(): number {
    return this.costAfterOverhead() * Number(this.margins.profitPercent || 0) / 100;
  }
  protected finalPrice(): number { return this.costAfterOverhead() + this.profitAmount(); }
  protected unitPrice(): number {
    return this.engineering.quantity > 0 ? this.finalPrice() / this.engineering.quantity : 0;
  }

  constructor() {
    this.api.customers().subscribe((rows) => {
      this.customers.set(rows);
      const requestedRfq = Number(this.route.snapshot.queryParamMap.get('rfqId') ?? 0);
      if (requestedRfq) {
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
  }

  protected onCustomer(): void {
    this.costSheet.set(null);
    this.savedCostInputs = '';
    this.commercial.rfqId = 0;
    this.commercial.rfqItemId = 0;
    this.items.set([]);
    this.api.rfqs(this.commercial.customerId).subscribe((rows) => this.rfqs.set(rows));
  }

  protected onRfq(): void {
    this.costSheet.set(null);
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
    this.costSheet.set(null);
    this.savedCostInputs = '';
    const item = this.items().find((row) => row.id === this.commercial.rfqItemId);
    if (!item) return;
    this.transportCost = Number(item.transportCost ?? 0);
    this.engineering.materialNo = item.materialNo;
    this.engineering.shortDescription = this.conciseDescription(item.description);
    this.engineering.quantity = item.quantity;
    this.applyDetectedGeometry(item);
    this.detectedMetal.set(item.grade?.trim() ?? 'Material not detected');
    this.generateQuotationNumber(item);
    this.refreshMetalData(item);
    this.loadMaterial();
  }

  protected onMetal(): void {
    const metal = this.metals().find((row) => row.id === this.engineering.metalId);
    if (!metal) return;
    this.engineering.densityKgM3 = metal.densityKgM3;
    this.engineering.materialRatePerKg = metal.defaultRatePerKg;
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

  protected loadMaterial(): void {
    if (!this.engineering.materialNo.trim()) return;
    this.api.material(this.engineering.materialNo).subscribe({
      next: (details) => {
        this.master.set(details);
        this.engineering.shortDescription =
          details.material.shortDescription?.trim() || this.engineering.shortDescription;
        this.engineering.metalId = details.material.metalMaterialId ?? 0;
        this.engineering.densityKgM3 =
          details.material.metalMaterial?.densityKgM3 ?? this.engineering.densityKgM3;
        this.engineering.materialRatePerKg =
          details.material.metalMaterial?.defaultRatePerKg ??
          this.engineering.materialRatePerKg;
        if (details.material.rawMaterialShape?.trim()) {
          this.engineering.shape = this.normalizeShape(details.material.rawMaterialShape);
        }
        this.calculateImportedWeight();
        this.processLines.set(
          details.routing.map((route, index) => ({
            sequence: index + 2,
            category: this.categoryFor(route.processName, route.processType),
            description:
              route.processName + (route.vendorName ? ' · ' + route.vendorName : ''),
            amount: route.processType === 'Outsource' ? route.ratePerPiece : 0,
          })),
        );
        this.newLine.sequence = this.processLines().length + 2;
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

  protected addLine(): void {
    const lines = [
      ...this.processLines(),
      { ...this.newLine, sequence: this.processLines().length + 2 },
    ];
    this.processLines.set(lines);
    this.newLine = {
      sequence: lines.length + 2,
      category: 'CNC',
      description: '',
      amount: 0,
    };
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
    if (!Number.isFinite(Number(this.transportCost)) || Number(this.transportCost) < 0) {
      this.snack.open('Transport cost must be zero or greater', 'Dismiss', { duration: 4000 });
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
      ...this.processLines().map((line, index) => ({ ...line, sequence: index + 2 })),
      ...(Number(this.transportCost) > 0 ? [{
        sequence: this.processLines().length + 2,
        category: 'Transport',
        description: 'Transport cost',
        amount: Number(this.transportCost),
      }] : []),
    ];
    this.busy.set(true);
    this.api
      .saveCostSheet({
        rfqItemId: this.commercial.rfqItemId,
        quantity: Number(this.engineering.quantity),
        overheadPercent: Number(this.margins.overheadPercent),
        profitPercent: Number(this.margins.profitPercent),
        lines,
      })
      .subscribe({
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

  protected createQuotation(): void {
    if (!this.costSheet() || !this.commercial.quotationNumber.trim()) {
      this.snack.open('Save the cost sheet and enter a quotation number', 'Dismiss', {
        duration: 3300,
      });
      return;
    }
    if (this.savedCostInputs !== this.costInputs()) {
      this.snack.open('Cost inputs changed. Recalculate and save the cost sheet again', 'Dismiss', { duration: 4000 });
      return;
    }
    const today = new Date().toISOString().slice(0, 10);
    this.busy.set(true);
    this.api
      .createQuotation({
        quotationNumber: this.commercial.quotationNumber,
        customerId: this.commercial.customerId,
        rfqId: this.commercial.rfqId,
        quotationDate: today,
        paymentTerms: this.commercial.paymentTerms,
        deliveryTerms: this.commercial.deliveryTerms,
        deliveryTime: this.commercial.deliveryTime,
        status: 'Draft',
        items: [
          {
            rfqItemId: this.commercial.rfqItemId,
            description: this.engineering.shortDescription.trim() || null,
            materialRatePerKg: Number(this.engineering.materialRatePerKg),
            densityKgM3: Number(this.engineering.densityKgM3),
            rawMaterialShape: this.engineering.shape,
            rawMaterialDimensions: this.master()?.material.rawMaterialSize,
            weightPerPieceKg: this.weight()?.weightPerPieceKg,
            totalWeightKg: this.weight()?.totalWeightKg,
          },
        ],
      })
      .subscribe({
        next: (quotation) => {
          this.busy.set(false);
          this.snack.open('Quotation ' + quotation.quotationNumber + ' created', 'Dismiss', {
            duration: 4200,
          });
        },
        error: () => {
          this.busy.set(false);
          this.snack.open('Quotation could not be created', 'Dismiss', { duration: 3800 });
        },
      });
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
    this.engineering.materialRatePerKg = metal.defaultRatePerKg;
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
