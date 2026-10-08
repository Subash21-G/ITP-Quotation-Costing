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
  materialNo: string;
  description?: string;
  drawingNo?: string;
  grade?: string;
  dimensions?: string;
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
  revision: number;
  quotationDate: string;
  validUntil?: string;
  status: string;
  totalPrice: number;
}
