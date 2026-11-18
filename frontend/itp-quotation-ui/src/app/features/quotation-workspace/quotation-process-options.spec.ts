import { combineProcessOptions } from './quotation-process-options';

describe('combineProcessOptions', () => {
  it('merges both master sources and labels matching choices from both', () => {
    const options = combineProcessOptions(
      [
        { id: 1, processName: 'Tapping', defaultProcessType: 'InHouse', defaultMachineRate: 0, isActive: true },
        { id: 2, processName: 'Inactive', defaultProcessType: 'InHouse', defaultMachineRate: 0, isActive: false },
      ],
      [
        { id: 1, vendorName: 'Mohan Engg', isActive: true },
        { id: 2, vendorName: 'Inactive Vendor', isActive: false },
      ],
      [
        { id: 1, vendorId: 1, vendorName: 'Mohan Engg', processId: 1, processName: 'Tapping', rateType: 'PerPiece', rate: 10, effectiveFrom: '2020-01-01' },
        { id: 2, vendorId: 2, vendorName: 'Inactive Vendor', processId: 1, processName: 'Tapping', rateType: 'PerPiece', rate: 10, effectiveFrom: '2020-01-01' },
      ],
      [
        { id: 10, processName: 'Tapping', vendorName: '', source: 'In-house' },
        { id: 11, processName: 'Turning', vendorName: 'Vendor B', source: 'Outsource' },
      ],
    );

    expect(options).toEqual([
      expect.objectContaining({
        processName: 'Tapping',
        vendorName: '',
        source: 'In-house',
        origin: 'ITP Quotation + POTracker',
      }),
      expect.objectContaining({
        processName: 'Tapping',
        vendorName: 'Mohan Engg',
        source: 'Outsource',
        origin: 'ITP Quotation',
      }),
      expect.objectContaining({
        processName: 'Turning',
        vendorName: 'Vendor B',
        source: 'Outsource',
        origin: 'POTracker',
      }),
    ]);
  });
});
