import {
  Component,
  OnInit,
  AfterViewInit,
  OnDestroy,
  ChangeDetectionStrategy,
  ViewChild,
  ElementRef,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import uPlot from 'uplot';
import { WeightReportService, WeightReport } from '../../services/weight-report.service';

@Component({
  selector: 'app-weight-chart',
  imports: [CommonModule],
  templateUrl: './weight-chart.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./weight-chart.component.scss'],
})
export class WeightChartComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly weightReportService = inject(WeightReportService);

  readonly isLoading$ = this.weightReportService.isLoading$;
  readonly error$ = this.weightReportService.error$;
  readonly weightReport$ = this.weightReportService.weightReport$;

  @ViewChild('chartContainer') private chartContainer?: ElementRef<HTMLDivElement>;

  private chart?: uPlot;
  private resizeObserver?: ResizeObserver;
  private subscription?: Subscription;
  private viewReady = false;
  private data?: uPlot.AlignedData;

  ngOnInit(): void {
    this.subscription = this.weightReport$.subscribe((report) => {
      this.data = this.toChartData(report);
      this.renderChart();
    });
    this.weightReportService.loadWeightReport();
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.setupResizeObserver();
    this.renderChart();
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.resizeObserver?.disconnect();
    this.chart?.destroy();
  }

  onRetry(): void {
    this.weightReportService.loadWeightReport();
  }

  /** Build uPlot [x, y] arrays: x = unix seconds (ascending), y = moving average. */
  private toChartData(report: WeightReport | null): uPlot.AlignedData | undefined {
    if (!report || report.entries.length === 0) {
      return undefined;
    }
    const sorted = [...report.entries].sort(
      (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
    );
    const xs = sorted.map((e) => Math.floor(new Date(e.date).getTime() / 1000));
    const ys = sorted.map((e) => e.averageWeight);
    return [xs, ys];
  }

  private renderChart(): void {
    if (!this.viewReady || !this.chartContainer) {
      return;
    }

    if (!this.data) {
      this.chart?.destroy();
      this.chart = undefined;
      return;
    }

    if (this.chart) {
      this.chart.setData(this.data);
    } else {
      this.chart = new uPlot(
        this.buildOptions(),
        this.data,
        this.chartContainer.nativeElement,
      );
    }
  }

  private buildOptions(): uPlot.Options {
    const { width, height } = this.currentSize();
    return {
      width,
      height,
      scales: { x: { time: true } },
      series: [
        {},
        {
          label: 'Average (kg)',
          stroke: '#3b82f6',
          width: 2,
          points: { show: false },
        },
      ],
      axes: [{}, { label: 'kg' }],
    };
  }

  private setupResizeObserver(): void {
    if (!this.chartContainer) {
      return;
    }
    this.resizeObserver = new ResizeObserver(() => {
      if (this.chart) {
        this.chart.setSize(this.currentSize());
      }
    });
    this.resizeObserver.observe(this.chartContainer.nativeElement);
  }

  private currentSize(): { width: number; height: number } {
    const el = this.chartContainer!.nativeElement;
    return {
      width: el.clientWidth || 300,
      height: el.clientHeight || 200,
    };
  }
}
