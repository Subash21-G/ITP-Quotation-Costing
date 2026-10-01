import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'new-quotation',
    loadComponent: () =>
      import('./features/quotation-workspace/quotation-workspace').then(
        (m) => m.QuotationWorkspace,
      ),
  },
  {
    path: 'rfqs/import',
    loadComponent: () =>
      import('./features/rfq-import/rfq-import').then((m) => m.RfqImport),
  },
  {
    path: 'rfqs',
    loadComponent: () => import('./features/rfqs/rfqs').then((m) => m.Rfqs),
  },
  {
    path: 'quotations',
    loadComponent: () =>
      import('./features/quotations/quotations').then((m) => m.Quotations),
  },
  {
    path: 'masters/materials',
    loadComponent: () => import('./features/materials/materials').then((m) => m.Materials),
  },
  {
    path: 'masters/metals',
    data: { type: 'metals' },
    loadComponent: () =>
      import('./features/master-data/master-data').then((m) => m.MasterData),
  },
  {
    path: 'masters/processes',
    data: { type: 'processes' },
    loadComponent: () =>
      import('./features/master-data/master-data').then((m) => m.MasterData),
  },
  {
    path: 'masters/vendors',
    data: { type: 'vendors' },
    loadComponent: () =>
      import('./features/master-data/master-data').then((m) => m.MasterData),
  },
  {
    path: 'masters/rates',
    data: { type: 'vendor-process-rates' },
    loadComponent: () =>
      import('./features/master-data/master-data').then((m) => m.MasterData),
  },
  {
    path: 'reports',
    data: { kind: 'reports' },
    loadComponent: () => import('./features/info-page/info-page').then((m) => m.InfoPage),
  },
  {
    path: 'settings',
    data: { kind: 'settings' },
    loadComponent: () => import('./features/info-page/info-page').then((m) => m.InfoPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
