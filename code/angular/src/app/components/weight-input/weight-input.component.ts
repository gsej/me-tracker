import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SettingsService } from '../../settings/settings.service';
import { HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-weight-input',
  templateUrl: './weight-input.component.html',
  styleUrls: ['./weight-input.component.scss'],
  standalone: true,
  imports: [ReactiveFormsModule]
})
export class WeightInputComponent {
  weightForm: FormGroup;
  @Output() weightSubmitted = new EventEmitter<number | null>();
  apiUrl: string = '';

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
            this.weightForm.reset();
            this.weightSubmitted.emit(weight);
          },
          error: (error) => {
            console.error('Error submitting weight:', error);
          }
        });
    }
  }
}
