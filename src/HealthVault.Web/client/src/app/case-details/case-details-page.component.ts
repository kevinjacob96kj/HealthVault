import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CaseDetailsComponent } from './case-details.component';

@Component({
  selector: 'app-case-details-page',
  standalone: true,
  imports: [CaseDetailsComponent],
  template: `
    @if (patientId !== null) {
      <app-case-details [patientId]="patientId" />
    } @else {
      <p class="status error">Invalid patient case.</p>
    }
  `,
  styles: `
    .status {
      margin: 2rem auto;
      max-width: 44rem;
      color: #9b2c2c;
      padding: 0 1rem;
    }
  `
})
export class CaseDetailsPageComponent {
  private readonly route = inject(ActivatedRoute);

  readonly patientId: number | null = (() => {
    const raw = this.route.snapshot.paramMap.get('patientId');
    const parsed = raw ? Number(raw) : NaN;
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  })();
}
