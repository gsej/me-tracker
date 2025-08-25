import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { SettingsService } from '../settings/settings.service';

export interface WeightReportEntry {
  date: string;
  recordedWeight: number;
  averageWeight: number;
}

export interface WeightReport {
  entries: WeightReportEntry[];
}

@Injectable({
  providedIn: 'root'
})
export class WeightReportService {

  private weightReportSubject = new BehaviorSubject<WeightReport | null>(null);
  public weightReport$ = this.weightReportSubject.asObservable();

  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  public isLoading$ = this.isLoadingSubject.asObservable();

  private errorSubject = new BehaviorSubject<string | null>(null);
  public error$ = this.errorSubject.asObservable();

  private apiUrl: string;

  constructor(
    private http: HttpClient,
    private settingsService: SettingsService
  ) {
    this.apiUrl = this.settingsService.settings.apiUrl;
  }

  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'x-api-key': localStorage.getItem('api_key') || ''
    });
  }

  loadWeightRecords(): void {
    this.isLoadingSubject.next(true);
    this.errorSubject.next(null);

    this.http.get<WeightReport>(`${this.apiUrl}/api/report`, { headers: this.getHeaders() })
      .subscribe({
        next: (data) => {
          const formattedRecords = data.entries.map(entry => ({
            date: new Date(entry.date).toLocaleDateString(),
            recordedWeight: entry.recordedWeight,
            averageWeight: entry.averageWeight
          }));
          this.weightReportSubject.next({ entries: formattedRecords  });
          this.isLoadingSubject.next(false);
        },
        error: (error) => {
          console.error('Error fetching weight report:', error);
          this.errorSubject.next(`Error: ${error.status || 'Unknown'}`);
          this.isLoadingSubject.next(false);
        }
      });
  }
}
