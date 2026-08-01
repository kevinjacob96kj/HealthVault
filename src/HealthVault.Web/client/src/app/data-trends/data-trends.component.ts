import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CaseDetailsComponent } from '../case-details/case-details.component';
import { PatientsService } from '../patients/patients.service';
import { PatientObservation } from './patient-observation.model';

interface TrendPoint {
  value: number;
  label: string;
  shortLabel: string;
  raw: string;
  x: number;
  y: number;
}

interface AxisTick {
  value: number;
  label: string;
  y: number;
}

interface ObservationTrend {
  loincNum: string;
  shortName: string;
  longCommonName: string;
  units: string | null;
  points: TrendPoint[];
  polylinePoints: string;
  min: number;
  max: number;
  latest: string;
  yTicks: AxisTick[];
}

@Component({
  selector: 'app-data-trends',
  standalone: true,
  imports: [CommonModule, CaseDetailsComponent],
  templateUrl: './data-trends.component.html',
  styleUrl: './data-trends.component.css'
})
export class DataTrendsComponent implements OnInit {
  private readonly patientsService = inject(PatientsService);

  /** SVG chart layout (viewBox units). */
  readonly chart = {
    width: 360,
    height: 220,
    left: 48,
    right: 16,
    top: 16,
    bottom: 44
  };

  observations: PatientObservation[] = [];
  trends: ObservationTrend[] = [];
  loading = true;
  error: string | null = null;

  get plotLeft(): number {
    return this.chart.left;
  }

  get plotRight(): number {
    return this.chart.width - this.chart.right;
  }

  get plotTop(): number {
    return this.chart.top;
  }

  get plotBottom(): number {
    return this.chart.height - this.chart.bottom;
  }

  get plotWidth(): number {
    return this.plotRight - this.plotLeft;
  }

  get plotHeight(): number {
    return this.plotBottom - this.plotTop;
  }

  ngOnInit(): void {
    this.patientsService.getMyPatientData().subscribe({
      next: (observations) => {
        this.observations = observations;
        this.trends = this.buildTrends(observations);
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to load your health data.';
        this.loading = false;
      }
    });
  }

  formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  private formatShortDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric'
    });
  }

  private pointY(pointValue: number, min: number, max: number): number {
    if (max === min) {
      return this.plotTop + this.plotHeight / 2;
    }

    return this.plotBottom - ((pointValue - min) / (max - min)) * this.plotHeight;
  }

  private pointX(index: number, total: number): number {
    if (total <= 1) {
      return this.plotLeft + this.plotWidth / 2;
    }

    return this.plotLeft + (index / (total - 1)) * this.plotWidth;
  }

  private formatTick(value: number): string {
    if (Number.isInteger(value)) {
      return String(value);
    }

    return Math.abs(value) >= 10 ? value.toFixed(1) : value.toFixed(2);
  }

  private buildYTicks(min: number, max: number, scaleMin: number, scaleMax: number): AxisTick[] {
    const mid = (min + max) / 2;
    const values = max === min ? [max] : [max, mid, min];

    return values.map((value) => ({
      value,
      label: this.formatTick(value),
      y: this.pointY(value, scaleMin, scaleMax)
    }));
  }

  private buildTrends(observations: PatientObservation[]): ObservationTrend[] {
    const groups = new Map<string, PatientObservation[]>();

    for (const row of observations) {
      const current = groups.get(row.loincNum) ?? [];
      current.push(row);
      groups.set(row.loincNum, current);
    }

    return [...groups.entries()]
      .map(([loincNum, rows]) => {
        const chronological = [...rows].sort(
          (a, b) => new Date(a.observedAt).getTime() - new Date(b.observedAt).getTime()
        );
        const rawPoints = chronological
          .map((row) => {
            const numeric = Number(row.value);
            if (!Number.isFinite(numeric)) {
              return null;
            }

            return {
              value: numeric,
              raw: row.value,
              label: this.formatDate(row.observedAt),
              shortLabel: this.formatShortDate(row.observedAt)
            };
          })
          .filter(
            (
              point
            ): point is { value: number; raw: string; label: string; shortLabel: string } =>
              point !== null
          );

        if (rawPoints.length < 1) {
          return null;
        }

        const values = rawPoints.map((point) => point.value);
        const dataMin = Math.min(...values);
        const dataMax = Math.max(...values);
        const pad =
          dataMax === dataMin ? Math.max(Math.abs(dataMin) * 0.05, 1) : (dataMax - dataMin) * 0.08;
        const scaleMin = dataMin - pad;
        const scaleMax = dataMax + pad;
        const first = chronological[0];
        const latestPoint = rawPoints[rawPoints.length - 1];

        const points: TrendPoint[] = rawPoints.map((point, index) => ({
          ...point,
          x: this.pointX(index, rawPoints.length),
          y: this.pointY(point.value, scaleMin, scaleMax)
        }));

        return {
          loincNum,
          shortName: first.shortName,
          longCommonName: first.longCommonName,
          units: first.units,
          points,
          min: dataMin,
          max: dataMax,
          latest: `${latestPoint.raw}${first.units ? ` ${first.units}` : ''}`,
          yTicks: this.buildYTicks(dataMin, dataMax, scaleMin, scaleMax),
          polylinePoints: points.map((point) => `${point.x},${point.y}`).join(' ')
        } satisfies ObservationTrend;
      })
      .filter((trend): trend is ObservationTrend => trend !== null);
  }
}
