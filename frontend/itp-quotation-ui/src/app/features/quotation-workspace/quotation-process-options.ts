import {
  PoTrackerProcessOption,
  ProcessMaster,
  Vendor,
  VendorRate,
} from '../../core/api.models';

export interface QuotationProcessOption {
  key: string;
  processName: string;
  vendorName: string;
  source: 'In-house' | 'Outsource';
  origin: 'ITP Quotation' | 'POTracker' | 'ITP Quotation + POTracker';
}

interface MergedProcessOption {
  processName: string;
  vendorName: string;
  source: QuotationProcessOption['source'];
  origins: Set<'ITP Quotation' | 'POTracker'>;
}

export function combineProcessOptions(
  processes: readonly ProcessMaster[],
  vendors: readonly Vendor[],
  rates: readonly VendorRate[],
  poTrackerOptions: readonly PoTrackerProcessOption[],
): QuotationProcessOption[] {
  const activeProcesses = new Map(
    processes.filter((process) => process.isActive).map((process) => [process.id, process]),
  );
  const activeVendors = new Map(
    vendors.filter((vendor) => vendor.isActive).map((vendor) => [vendor.id, vendor]),
  );
  const merged = new Map<string, MergedProcessOption>();

  const add = (
    processName: string,
    vendorName: string,
    source: QuotationProcessOption['source'],
    origin: 'ITP Quotation' | 'POTracker',
  ) => {
    const process = processName.trim();
    const vendor = vendorName.trim();
    if (!process) return;

    const key = JSON.stringify([process.toLowerCase(), vendor.toLowerCase(), source]);
    const existing = merged.get(key);
    if (existing) {
      existing.origins.add(origin);
      return;
    }

    merged.set(key, {
      processName: process,
      vendorName: vendor,
      source,
      origins: new Set([origin]),
    });
  };

  for (const process of activeProcesses.values()) {
    add(
      process.processName,
      '',
      process.defaultProcessType === 'Outsource' ? 'Outsource' : 'In-house',
      'ITP Quotation',
    );
  }

  for (const rate of rates) {
    const process = activeProcesses.get(rate.processId);
    const vendor = activeVendors.get(rate.vendorId);
    if (process && vendor) add(process.processName, vendor.vendorName, 'Outsource', 'ITP Quotation');
  }

  for (const option of poTrackerOptions) {
    add(option.processName, option.vendorName, option.source, 'POTracker');
  }

  return [...merged.entries()]
    .map(([key, option]): QuotationProcessOption => {
      const origin: QuotationProcessOption['origin'] = option.origins.size === 2
        ? 'ITP Quotation + POTracker'
        : option.origins.has('POTracker')
          ? 'POTracker'
          : 'ITP Quotation';
      return {
        key,
        processName: option.processName,
        vendorName: option.vendorName,
        source: option.source,
        origin,
      };
    })
    .sort((left, right) =>
      left.processName.localeCompare(right.processName) ||
      left.vendorName.localeCompare(right.vendorName) ||
      left.source.localeCompare(right.source),
    );
}
