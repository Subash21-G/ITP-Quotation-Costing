import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';

interface FieldDefinition {
  key: string;
  label: string;
}

@Component({
  selector: 'app-master-data',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './master-data.html',
  styleUrls: ['../shared-list.scss', './master-data.scss'],
})
export class MasterData {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);
  protected readonly type = signal('metals');
  protected readonly title = signal('Metals & density');
  protected readonly description = signal('Density and default raw-material rates.');
  protected readonly rows = signal<unknown[]>([]);
  protected readonly fields = signal<FieldDefinition[]>([]);
  protected readonly loading = signal(true);
  protected readonly canCreate = signal(true);
  protected form: Record<string, string | number | boolean> = {};

  constructor() {
    this.route.data.subscribe((data) => {
      this.type.set(String(data['type'] ?? 'metals'));
      this.configure();
      this.load();
    });
  }

  protected value(row: unknown, key: string): unknown {
    return (row as Record<string, unknown>)[key] ?? '—';
  }

  protected save(): void {
    const type = this.type();
    let payload: object;
    if (type === 'metals') {
      payload = {
        name: this.form['name'],
        grade: this.form['grade'] || null,
        densityKgM3: Number(this.form['densityKgM3']),
        defaultRatePerKg: Number(this.form['defaultRatePerKg']),
        isActive: true,
      };
    } else if (type === 'processes') {
      payload = {
        processName: this.form['processName'],
        description: this.form['description'] || null,
        defaultProcessType: this.form['defaultProcessType'] || 'InHouse',
        defaultMachineRate: Number(this.form['defaultMachineRate']),
        isActive: true,
      };
    } else {
      payload = {
        vendorName: this.form['vendorName'],
        contactPerson: this.form['contactPerson'] || null,
        phone: this.form['phone'] || null,
        email: this.form['email'] || null,
        address: this.form['address'] || null,
        isActive: true,
      };
    }

    this.api.createMaster(type, payload).subscribe({
      next: () => {
        this.snack.open('Master record created', 'Dismiss', { duration: 2300 });
        this.configure();
        this.load();
      },
      error: () => this.snack.open('Record could not be created', 'Dismiss', { duration: 3500 }),
    });
  }

  private configure(): void {
    const type = this.type();
    this.canCreate.set(type !== 'vendor-process-rates');
    if (type === 'metals') {
      this.title.set('Metals & density');
      this.description.set('Engineering density and default material rates per kilogram.');
      this.fields.set([
        { key: 'name', label: 'Metal' },
        { key: 'grade', label: 'Grade' },
        { key: 'densityKgM3', label: 'Density kg/m³' },
        { key: 'defaultRatePerKg', label: 'Default ₹/kg' },
      ]);
      this.form = { name: '', grade: '', densityKgM3: 7850, defaultRatePerKg: 0 };
    } else if (type === 'processes') {
      this.title.set('Process masters');
      this.description.set('Manufacturing operations and default machine rates.');
      this.fields.set([
        { key: 'processName', label: 'Process' },
        { key: 'defaultProcessType', label: 'Type' },
        { key: 'defaultMachineRate', label: 'Default ₹/hr' },
        { key: 'description', label: 'Description' },
      ]);
      this.form = {
        processName: '',
        description: '',
        defaultProcessType: 'InHouse',
        defaultMachineRate: 0,
      };
    } else if (type === 'vendors') {
      this.title.set('Vendors');
      this.description.set('Approved outsource partners and contact details.');
      this.fields.set([
        { key: 'vendorName', label: 'Vendor' },
        { key: 'contactPerson', label: 'Contact' },
        { key: 'phone', label: 'Phone' },
        { key: 'email', label: 'Email' },
      ]);
      this.form = { vendorName: '', contactPerson: '', phone: '', email: '', address: '' };
    } else {
      this.title.set('Vendor process rates');
      this.description.set('Effective outsource rates by vendor and process.');
      this.fields.set([
        { key: 'vendorName', label: 'Vendor' },
        { key: 'processName', label: 'Process' },
        { key: 'rateType', label: 'Rate type' },
        { key: 'rate', label: 'Rate' },
        { key: 'effectiveFrom', label: 'Effective from' },
      ]);
      this.form = {};
    }
  }

  private load(): void {
    this.loading.set(true);
    const type = this.type();
    const request: Observable<unknown[]> =
      type === 'metals'
        ? this.api.metals()
        : type === 'processes'
          ? this.api.processes()
          : type === 'vendors'
            ? this.api.vendors()
            : this.api.vendorRates();
    request.subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
