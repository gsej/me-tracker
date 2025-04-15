import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SettingsService } from '../../settings/settings.service';
import { HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-weight-input',
  templateUrl: './weight-input.component.html',
  styleUrls: ['./weight-input.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule]
})
export class WeightInputComponent {
  weightForm: FormGroup;  

  apiUrl: string = '';
  status: string | null = null;
  errorStatusCode: string | null = null;
  private successTimer: any;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private settingsService: SettingsService
  ) {
    this.weightForm = this.fb.group({
      weight: ['', [Validators.required, Validators.min(0)]]
    });
    this.apiUrl = this.settingsService.settings.apiUrl;
  }

  private clearStatusAfterDelay() {
    if (this.successTimer) {
      clearTimeout(this.successTimer);
    }
    this.successTimer = setTimeout(() => {
      this.status = null;
    }, 3000);
  }

  onSubmit() {
    if (this.weightForm.valid) {
      const weight = this.weightForm.value.weight;
      const headers = new HttpHeaders({
        'x-api-key': localStorage.getItem('api_key') || ''
      });

      this.http.post(`${this.apiUrl}/api/weight`, {
        weight: weight,
        date: new Date()
      }, { headers })
        .subscribe({
          next: () => {
            this.status = 'Success';
            this.errorStatusCode = null;
            this.weightForm.reset();         
            this.clearStatusAfterDelay();
          },
          error: (error) => {
            console.error('Error submitting weight:', error);
            this.status = 'Error';
            this.errorStatusCode = error.status || 'Unknown';
            this.clearStatusAfterDelay();
          }         
        });
    }
  }
}
