import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WeightService } from '../../services/weight.service';

@Component({
  selector: 'app-weight-records',
  imports: [CommonModule],
  templateUrl: './weight-records.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./weight-records.component.scss'],
})
export class WeightRecordsComponent implements OnInit {
  private readonly weightService = inject(WeightService);

  readonly weightRecords$ = this.weightService.weightRecords$;
  readonly isLoading$ = this.weightService.isLoading$;
  readonly error$ = this.weightService.error$;

  ngOnInit(): void {
    this.loadWeightRecords();
  }

  loadWeightRecords(): void {
    this.weightService.loadWeightRecords();
  }

  onRetry(): void {
    this.loadWeightRecords();
  }

  deleteRecord(id: string): void {
    if (confirm('Are you sure you want to delete this weight record?')) {
      this.weightService.deleteWeightRecord(id).subscribe({
        error: (error) => {
          console.error('Error deleting weight record:', error);
          // Error will be handled by the service subscription
        },
      });
    }
  }
}
