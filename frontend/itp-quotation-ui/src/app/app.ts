import { NgTemplateOutlet } from '@angular/common';
import { Component, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

@Component({
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NgTemplateOutlet,
    MatSidenavModule,
    MatButtonModule,
    MatDividerModule,
    MatTooltipModule,
  ],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly pageTitle = signal('Executive overview');
  protected readonly mobileOpen = signal(false);
  protected readonly navItems = [
    { label: 'Dashboard', glyph: '⌂', route: '/dashboard' },
    { label: 'New quotation', glyph: '+', route: '/new-quotation', accent: true },
    { label: 'RFQs', glyph: 'R', route: '/rfqs' },
    { label: 'Quotations', glyph: 'Q', route: '/quotations' },
  ];
  protected readonly masterItems = [
    { label: 'Materials', glyph: 'M', route: '/masters/materials' },
    { label: 'Metals & density', glyph: 'ρ', route: '/masters/metals' },
    { label: 'Processes', glyph: 'P', route: '/masters/processes' },
    { label: 'Vendors', glyph: 'V', route: '/masters/vendors' },
    { label: 'Rates', glyph: '₹', route: '/masters/rates' },
  ];

  constructor(router: Router) {
    router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe((event) => {
        const title =
          [...this.navItems, ...this.masterItems].find((item) =>
            event.urlAfterRedirects.startsWith(item.route),
          )?.label ?? (event.urlAfterRedirects.startsWith('/reports') ? 'Reports' : 'Settings');
        this.pageTitle.set(title);
        this.mobileOpen.set(false);
      });
  }
}
