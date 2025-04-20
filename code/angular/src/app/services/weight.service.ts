import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { SettingsService } from '../settings/settings.service';

export interface WeightRecord {
  date: string;
  weight: number;
}

export interface WeightsCollection {
  weightRecords: WeightRecord[];
}

@Injectable({
  providedIn: 'root'
})
export class WeightService {
  private weightRecordsSubject = new BehaviorSubject<WeightRecord[]>([]);
  public weightRecords$ = this.weightRecordsSubject.asObservable();

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

    this.http.get<WeightsCollection>(`${this.apiUrl}/api/backup`, { headers: this.getHeaders() })
      .subscribe({
        next: (data) => {
          const formattedRecords = data.weightRecords.map(record => ({
            date: new Date(record.date).toLocaleDateString(),
            weight: record.weight
          }));
          this.weightRecordsSubject.next(formattedRecords);
          this.isLoadingSubject.next(false);
        },
        error: (error) => {
          console.error('Error fetching weight records:', error);
          this.errorSubject.next(`Error: ${error.status || 'Unknown'}`);
          this.isLoadingSubject.next(false);
        }
      });
  }

  addWeightRecord(weight: number): Observable<any> {
    const payload = {
      weight: weight,
      date: new Date()
    };

    return this.http.post(`${this.apiUrl}/api/weight`, payload, { headers: this.getHeaders() })
      .pipe(
        tap(() => {
          // After successfully adding a new record, refresh the list
          this.loadWeightRecords();
        })
      );
  }
}
