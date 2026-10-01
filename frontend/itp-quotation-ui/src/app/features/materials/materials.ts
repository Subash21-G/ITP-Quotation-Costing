import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { concatMap, forkJoin } from 'rxjs';
import {
  MaterialDetails,
  MaterialMaster,
  MaterialRoute,
  MetalMaterial,
  ProcessMaster,
  Vendor,
} from '../../core/api.models';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-materials',
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './materials.html',
  styleUrl: './materials.scss',
})
export class Materials {
  private readonly api = inject(ApiService);
  private readonly snack = inject(MatSnackBar);
  protected readonly rows = signal<MaterialMaster[]>([]);
  protected readonly selected = signal<MaterialDetails | null>(null);
  protected readonly routes = signal<MaterialRoute[]>([]);
  protected readonly metals = signal<MetalMaterial[]>([]);
  protected readonly processes = signal<ProcessMaster[]>([]);
  protected readonly vendors = signal<Vendor[]>([]);
  protected readonly saving = signal(false);
  protected search = '';
  protected form = this.emptyMaterial();
  protected routeForm = {
    sequence: 1,
    processId: 0,
    processType: 'InHouse',
    vendorId: undefined as number | undefined,
    machineRate: 0,
    setupTimeHours: 0,
    cycleTimeHours: 0,
    ratePerPiece: 0,
    notes: '',
  };

  constructor() {
    forkJoin({
      materials: this.api.materials(),
      metals: this.api.metals(),
      processes: this.api.processes(),
      vendors: this.api.vendors(),
    }).subscribe(({ materials, metals, processes, vendors }) => {
      this.rows.set(materials);
      this.metals.set(metals);
      this.processes.set(processes);
      this.vendors.set(vendors);
    });
  }

  protected loadRows(): void {
    this.api.materials(this.search).subscribe((rows) => this.rows.set(rows));
  }

  protected select(material: MaterialMaster): void {
    this.api.material(material.materialNo).subscribe((details) => {
      this.selected.set(details);
      this.routes.set(details.routing);
      this.form = {
        id: details.material.id,
        materialNo: details.material.materialNo,
        shortDescription: details.material.shortDescription ?? '',
        grade: details.material.grade ?? '',
        drawingCode: details.material.drawingCode ?? '',
        drawingVersion: details.material.drawingVersion ?? '',
        finishSize: details.material.finishSize ?? '',
        metalMaterialId: details.material.metalMaterialId,
        rawMaterialShape: details.material.rawMaterialShape ?? '',
        rawMaterialSize: details.material.rawMaterialSize ?? '',
        notes: details.material.notes ?? '',
        isActive: details.material.isActive,
      };
      this.resetRoute();
    });
  }

  protected newMaterial(): void {
    this.selected.set(null);
    this.routes.set([]);
    this.form = this.emptyMaterial();
  }

  protected saveMaterial(): void {
    if (!this.form.materialNo.trim()) return;
    this.saving.set(true);
    const payload = { ...this.form, id: undefined };
    const request = this.form.id
      ? this.api.updateMaterial(this.form.id, payload)
      : this.api.createMaterial(payload);
    request.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.snack.open('Material master saved', 'Dismiss', { duration: 2500 });
        this.loadRows();
        this.select(saved);
      },
      error: () => {
        this.saving.set(false);
        this.snack.open('Material could not be saved', 'Dismiss', { duration: 3500 });
      },
    });
  }

  protected addRoute(): void {
    const materialId = this.form.id;
    if (!materialId || !this.routeForm.processId) return;
    const payload = {
      ...this.routeForm,
      vendorId: this.routeForm.processType === 'Outsource' ? this.routeForm.vendorId : null,
    };
    this.api.createRoute(materialId, payload).subscribe({
      next: () => {
        this.snack.open('Process added to route', 'Dismiss', { duration: 2200 });
        this.reloadSelected();
      },
      error: () => this.snack.open('Process route could not be added', 'Dismiss', { duration: 3500 }),
    });
  }

  protected removeRoute(route: MaterialRoute): void {
    if (!this.form.id) return;
    this.api.deleteRoute(this.form.id, route.id).subscribe(() => this.reloadSelected());
  }

  protected moveRoute(index: number, delta: number): void {
    const materialId = this.form.id;
    const target = index + delta;
    const routes = this.routes();
    if (!materialId || target < 0 || target >= routes.length) return;
    const current = routes[index];
    const other = routes[target];
    const temporarySequence = Math.max(...routes.map((route) => route.sequence)) + 1000;

    this.api
      .updateRoute(materialId, current.id, this.routePayload(current, temporarySequence))
      .pipe(
        concatMap(() =>
          this.api.updateRoute(materialId, other.id, this.routePayload(other, current.sequence)),
        ),
        concatMap(() =>
          this.api.updateRoute(materialId, current.id, this.routePayload(current, other.sequence)),
        ),
      )
      .subscribe({
        next: () => this.reloadSelected(),
        error: () => this.snack.open('Route order could not be changed', 'Dismiss', { duration: 3500 }),
      });
  }

  private reloadSelected(): void {
    const materialNo = this.form.materialNo;
    this.api.material(materialNo).subscribe((details) => {
      this.selected.set(details);
      this.routes.set(details.routing);
      this.routeForm.sequence = (details.routing.at(-1)?.sequence ?? 0) + 1;
    });
  }

  private resetRoute(): void {
    this.routeForm = {
      sequence: (this.routes().at(-1)?.sequence ?? 0) + 1,
      processId: 0,
      processType: 'InHouse',
      vendorId: undefined,
      machineRate: 0,
      setupTimeHours: 0,
      cycleTimeHours: 0,
      ratePerPiece: 0,
      notes: '',
    };
  }

  private routePayload(route: MaterialRoute, sequence: number): object {
    return {
      sequence,
      processId: route.processId,
      processType: route.processType,
      vendorId: route.vendorId ?? null,
      machineRate: route.machineRate,
      setupTimeHours: route.setupTimeHours,
      cycleTimeHours: route.cycleTimeHours,
      ratePerPiece: route.ratePerPiece,
      notes: route.notes,
    };
  }

  private emptyMaterial() {
    return {
      id: 0,
      materialNo: '',
      shortDescription: '',
      grade: '',
      drawingCode: '',
      drawingVersion: '',
      finishSize: '',
      metalMaterialId: undefined as number | undefined,
      rawMaterialShape: '',
      rawMaterialSize: '',
      notes: '',
      isActive: true,
    };
  }
}
