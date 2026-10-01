import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-info-page',
  imports: [MatButtonModule, RouterLink],
  templateUrl: './info-page.html',
  styleUrl: './info-page.scss',
})
export class InfoPage {
  private readonly route = inject(ActivatedRoute);
  protected readonly kind = signal(String(this.route.snapshot.data['kind'] ?? 'reports'));
  protected readonly title = signal(this.kind() === 'reports' ? 'Reports' : 'Settings');
  protected readonly description = signal(
    this.kind() === 'reports'
      ? 'Commercial and costing insights will appear here as quotation history grows.'
      : 'Application defaults and company preferences will be managed here.',
  );
}
