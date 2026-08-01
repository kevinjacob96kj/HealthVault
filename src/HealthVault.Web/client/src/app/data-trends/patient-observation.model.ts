export interface PatientObservation {
  id: number;
  loincCodeId: number;
  loincNum: string;
  shortName: string;
  longCommonName: string;
  value: string;
  units: string | null;
  observedAt: string;
  notes: string | null;
}
