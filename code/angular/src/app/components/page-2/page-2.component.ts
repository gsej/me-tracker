import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { SettingsService } from '../../settings/settings.service';

interface WeightRecord {
  date: string;
  weight: number;
}

interface WeightsCollection {
  weightRecords: WeightRecord[];
}

@Component({
  selector: 'app-page-2',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './page-2.component.html',
  styleUrls: ['./page-2.component.scss']
})
export class Page2Component implements OnInit {
  weightRecords: WeightRecord[] = [];
  isLoading: boolean = false;
  error: string | null = null;
  apiUrl: string = '';

  constructor(
    private http: HttpClient,
    private settingsService: SettingsService
  ) {
    this.apiUrl = this.settingsService.settings.apiUrl;
  }

  ngOnInit(): void {
    this.loadWeightRecords();
  }

  loadWeightRecords(): void {
    this.isLoading = true;
    this.error = null;

    const headers = new HttpHeaders({
      'x-api-key': localStorage.getItem('api_key') || ''
    });

    this.http.get<WeightsCollection>(`${this.apiUrl}/api/backup`, { headers })
      .subscribe({
        next: (data) => {
          this.weightRecords = data.weightRecords.map(record => ({
            date: new Date(record.date).toLocaleDateString(),
            weight: record.weight
          }));
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error fetching weight records:', error);
          this.error = `Error: ${error.status || 'Unknown'}`;
          this.isLoading = false;
        }
      });
  }
}
