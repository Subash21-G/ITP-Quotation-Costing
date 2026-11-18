export interface Customer {
  id: number;
  customerName: string;
  contactPerson?: string;
  email?: string;
  phone?: string;
}

export interface Rfq {
  id: number;
  rfqNumber: string;
  customerId: number;
  rfqDate?: string;
  status: string;
}

export interface RfqItem {
  id: number;
  rfqId: number;
  lineItem?: string;
  materialNo: string;
  description?: string;
  drawingNo?: string;
  grade?: string;
  dimensions?: string;
  transportCost?: number | null;
  quantity: number;
  unit: string;
  deliveryDate?: string;
}

export interface RfqImportItem {
  lineItem?: string;
  materialNo?: string;
  description?: string;
  drawingNo?: string;
  quantity?: number;
  unit: string;
  deliveryDate?: string;
  grade?: string;
  dimensions?: string;
  transportCost?: number | null;
}

export interface RfqPdfExtraction {
  fileName: string;
  pageCount: number;
  extractedText: string;
  rfqNumber?: string;
  customerName?: string;
  rfqDate?: string;
  items: RfqImportItem[];
  warnings: string[];
  requiresConfirmation: boolean;
}

export interface RfqImportConfirmation {
  rfqId: number;
  rfqNumber: string;
  itemCount: number;
  confirmed: boolean;
}

export interface MetalMaterial {
  id: number;
  name: string;
  grade?: string;
  densityKgM3: number;
  defaultRatePerKg: number;
  isActive: boolean;
}

export interface MaterialMaster {
  id: number;
  materialNo: string;
  shortDescription?: string;
  grade?: string;
  drawingCode?: string;
  drawingVersion?: string;
  finishSize?: string;
  metalMaterialId?: number;
  rawMaterialShape?: string;
  rawMaterialSize?: string;
  notes?: string;
  isActive: boolean;
  metalMaterial?: MetalMaterial;
}

export interface MaterialRoute {
  id: number;
  materialMasterId: number;
  sequence: number;
  processId: number;
  processName: string;
  processType: string;
  vendorId?: number;
  vendorName?: string;
  machineRate: number;
  setupTimeHours: number;
  cycleTimeHours: number;
  ratePerPiece: number;
  notes?: string;
}

export interface MaterialDetails {
  material: MaterialMaster;
  routing: MaterialRoute[];
}

export interface ProcessMaster {
  id: number;
  processName: string;
  description?: string;
  defaultProcessType: string;
  defaultMachineRate: number;
  isActive: boolean;
}

export interface Vendor {
  id: number;
  vendorName: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  isActive: boolean;
}

export interface VendorRate {
  id: number;
  vendorId: number;
  vendorName: string;
  processId: number;
  processName: string;
  rateType: string;
  rate: number;
  effectiveFrom: string;
  effectiveTo?: string;
}

export interface PoTrackerProcessOption {
  id: number;
  processName: string;
  vendorName: string;
  source: 'In-house' | 'Outsource';
}

export interface MetalWeightResult {
  shape: string;
  mode: string;
  unit: string;
  densityKgM3: number;
  crossSectionArea: number;
  volumePerPiece: number;
  weightPerPieceKg: number;
  quantity: number;
  totalWeightKg: number;
  requiredLength?: number;
}

export interface CostSheetLine {
  id?: number;
  sequence: number;
  category: string;
  description?: string;
  amount: number;
}

export interface CostSheet {
  id: number;
  rfqItemId: number;
  quantity: number;
  overheadPercent: number;
  profitPercent: number;
  lines: CostSheetLine[];
  categoryTotals: Record<string, number>;
  manufacturingCost: number;
  overheadAmount: number;
  costAfterOverhead: number;
  profitAmount: number;
  sellingPrice: number;
  unitSellingPrice: number;
}

export interface QuotationProcessSnapshot {
  sequence: number;
  processName: string;
  processType: string;
  vendorName?: string;
  rateType?: string;
  rate: number;
  machineRate: number;
  setupTimeHours: number;
  cycleTimeHours: number;
  cost: number;
}

export interface QuotationCostBreakdown {
  lines: CostSheetLine[];
  categoryTotals: Record<string, number>;
  manufacturingCost: number;
  overheadPercent: number;
  overheadAmount: number;
  costAfterOverhead: number;
  profitPercent: number;
  profitAmount: number;
  sellingPrice: number;
  unitSellingPrice: number;
}

export interface QuotationItemSnapshot {
  id?: number;
  rfqItemId: number;
  materialNo: string;
  description?: string;
  drawingNo?: string;
  quantity: number;
  materialRatePerKg?: number;
  densityKgM3?: number;
  rawMaterialShape?: string;
  rawMaterialDimensions?: string;
  weightPerPieceKg?: number;
  totalWeightKg?: number;
  processRoute: QuotationProcessSnapshot[];
  costBreakdown: QuotationCostBreakdown;
  unitPrice: number;
  totalPrice: number;
}

export interface QuotationItemWriteRequest {
  rfqItemId: number;
  description?: string | null;
  materialRatePerKg?: number | null;
  densityKgM3?: number | null;
  rawMaterialShape?: string | null;
  rawMaterialDimensions?: string | null;
  weightPerPieceKg?: number | null;
  totalWeightKg?: number | null;
  unitPriceOverride?: number | null;
  processRoute: QuotationProcessSnapshot[];
}

export interface Quotation {
  poTrackerPurchaseOrderId?: number;
  poTrackerPoNumber?: string;
  poTrackerExportedRevision?: number;
  id: number;
  quotationNumber: string;
  customerId: number;
  customerName: string;
  rfqId: number;
  rfqNumber: string;
  items: QuotationItemSnapshot[];
  revision: number;
  quotationDate: string;
  validUntil?: string;
  paymentTerms?: string;
  deliveryTerms?: string;
  deliveryTime?: string;
  freightTerms?: string;
  taxNotes?: string;
  status: string;
  totalPrice: number;
}
