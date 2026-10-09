import {
  Component,
  OnInit,
  AfterViewInit,
  OnDestroy,
  ChangeDetectionStrategy,
  ViewChild,
  ElementRef,
  Input,
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
  private report: WeightReport | null = null;
  private _rangeMonths: number | null = 3;

  /** Number of months (from the latest entry) to show, or `null` for all data. */
  @Input() set rangeMonths(value: number | null) {
    this._rangeMonths = value;
    this.rebuild();
  }

  ngOnInit(): void {
    this.subscription = this.weightReport$.subscribe((report) => {
      this.report = report;
      this.rebuild();
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

  /** Re-filter the current report for the selected range and repaint. */
  private rebuild(): void {
    this.data = this.toChartData(this.report);
    this.renderChart();
  }

  /** Build uPlot [x, y] arrays: x = unix seconds (ascending), y = moving average. */
  private toChartData(report: WeightReport | null): uPlot.AlignedData | undefined {
    if (!report || report.entries.length === 0) {
      return undefined;
    }
    const sorted = [...report.entries].sort(
      (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
    );
    const windowed = this.filterByRange(sorted);
    if (windowed.length === 0) {
      return undefined;
    }
    const xs = windowed.map((e) => Math.floor(new Date(e.date).getTime() / 1000));
    const ys = windowed.map((e) => e.averageWeight);
    return [xs, ys];
  }

  /**
   * Keep only entries within `rangeMonths` of the most recent entry (so the
   * default view always shows the latest data). `null` means "all data".
   */
  private filterByRange<T extends { date: string }>(sortedAsc: T[]): T[] {
    if (this._rangeMonths == null || sortedAsc.length === 0) {
      return sortedAsc;
    }
    const latest = new Date(sortedAsc[sortedAsc.length - 1].date);
    const cutoff = new Date(latest);
    cutoff.setMonth(cutoff.getMonth() - this._rangeMonths);
    return sortedAsc.filter((e) => new Date(e.date).getTime() >= cutoff.getTime());
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
        {
          label: 'Date',
          value: (_self, rawValue) =>
            rawValue == null ? '--' : new Date(rawValue * 1000).toLocaleDateString(),
        },
        {
          label: 'Weight (kg)',
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
    const width = el.clientWidth || 300;
    const available = el.clientHeight || 200;
    // Prefer a 16:9 height, but never exceed the space available (keeps
    // landscape fitting on screen, while portrait gets the nicer ratio).
    const height = Math.min(available, Math.max(150, Math.round(width * 9 / 16)));
    return { width, height };
  }
}
