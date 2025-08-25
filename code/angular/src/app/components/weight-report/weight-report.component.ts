import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { WeightReport, WeightReportService } from '../../services/weight-report.service';

@Component({
  selector: 'app-weight-report',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './weight-report.component.html',
  styleUrls: ['./weight-report.component.scss']
})
export class WeightReportComponent implements OnInit, OnDestroy {
  weightReport: WeightReport | null = null;
  isLoading: boolean = false;
  error: string | null = null;

  private subscriptions: Subscription[] = [];

  constructor(private weightReportService: WeightReportService) { }

  ngOnInit(): void {
    this.subscriptions.push(
      this.weightReportService.weightReport$.subscribe(report => {
        this.weightReport = report;
      })
    );

    this.subscriptions.push(
      this.weightReportService.isLoading$.subscribe(loading => {
        this.isLoading = loading;
      })
    );

    this.subscriptions.push(
      this.weightReportService.error$.subscribe(error => {
        this.error = error;
      })
    );

    this.loadWeightRecords();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  loadWeightRecords(): void {
    this.weightReportService.loadWeightReport();
  }

  onRetry(): void {
    this.loadWeightRecords();
  }
}
