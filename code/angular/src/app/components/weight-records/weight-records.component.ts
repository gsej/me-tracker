import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WeightService, WeightRecord } from '../../services/weight.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-weight-records',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './weight-records.component.html',
  styleUrls: ['./weight-records.component.scss']
})
export class WeightRecordsComponent implements OnInit, OnDestroy {
  weightRecords: WeightRecord[] = [];
  isLoading: boolean = false;
  error: string | null = null;

  private subscriptions: Subscription[] = [];

  constructor(private weightService: WeightService) { }

  ngOnInit(): void {
    // Subscribe to the service observables
    this.subscriptions.push(
      this.weightService.weightRecords$.subscribe(records => {
        this.weightRecords = records;
      })
    );

    this.subscriptions.push(
      this.weightService.isLoading$.subscribe(loading => {
        this.isLoading = loading;
      })
    );

    this.subscriptions.push(
      this.weightService.error$.subscribe(error => {
        this.error = error;
      })
    );

    // Load the weight records
    this.loadWeightRecords();
  }

  ngOnDestroy(): void {
    // Clean up subscriptions to prevent memory leaks
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  loadWeightRecords(): void {
    this.weightService.loadWeightRecords();
  }

  onRetry(): void {
    this.loadWeightRecords();
  }
}
