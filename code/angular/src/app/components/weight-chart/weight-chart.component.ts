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
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import uPlot from 'uplot';
import { WeightReportService, WeightReport } from '../../services/weight-report.service';

/** A day with comments, drawn as a marker line on the chart. */
interface CommentMarker {
  /** Unix seconds, matching the chart's x values. */
  x: number;
  date: string;
  comments: string[];
}

/** The comment popup, positioned within the chart wrapper (CSS px). */
export interface CommentPopup {
  top: number;
  /** Set when the popup sits to the right of its marker... */
  left: number | null;
  /** ...or this when it sits to the left (markers in the right half). */
  right: number | null;
  date: string;
  comments: string[];
}

const MARKER_COLOUR = '#f59e0b';
/** How close (CSS px) a tap or hover must be to a marker line to pick it. */
const MARKER_HIT_PX = 16;
/** Pointer movement (CSS px) beyond which a press is a drag, not a tap. */
const TAP_SLOP_PX = 5;

/** How far back (from the latest entry) to show; `null` means all data. */
export type ChartRange = { days: number } | { months: number } | null;

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
  @ViewChild('chartWrapper') private chartWrapper?: ElementRef<HTMLDivElement>;

  /** The comment popup currently shown, if any. */
  readonly popup = signal<CommentPopup | null>(null);
  private markers: CommentMarker[] = [];
  /** Set from pointerdown until the pointer moves too far to count as a tap. */
  private tap?: { pointerId: number; x: number; y: number };

  private chart?: uPlot;
  private resizeObserver?: ResizeObserver;
  private subscription?: Subscription;
  private viewReady = false;
  private data?: uPlot.AlignedData;
  private report: WeightReport | null = null;
  private _range: ChartRange = { months: 3 };
  /** The visible x window (unix seconds); panning slides it within the data. */
  private window?: { min: number; max: number };
  /** Set while a drag is in progress. */
  private drag?: { pointerId: number; startX: number; min: number; max: number };

  /** How far back (from the latest entry) to show, or `null` for all data. */
  @Input() set range(value: ChartRange) {
    this._range = value;
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

  /** Rebuild the data, reset the window to end at the latest entry, and repaint. */
  private rebuild(): void {
    this.data = this.toChartData(this.report);
    this.markers = this.toMarkers(this.report);
    this.window = this.initialWindow();
    this.drag = undefined;
    this.tap = undefined;
    this.popup.set(null);
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
    const xs = sorted.map((e) => Math.floor(new Date(e.date).getTime() / 1000));
    const ys = sorted.map((e) => e.averageWeight);
    return [xs, ys];
  }

  /** One marker per day that has comments, x matching the chart's x values. */
  private toMarkers(report: WeightReport | null): CommentMarker[] {
    if (!report) {
      return [];
    }
    return report.entries
      .filter((e) => e.comments?.length > 0)
      .map((e) => ({
        x: Math.floor(new Date(e.date).getTime() / 1000),
        date: e.date,
        comments: e.comments,
      }))
      .sort((a, b) => a.x - b.x);
  }

  /** Draw a dashed line, with a dot at the top, for each day with comments. */
  private drawMarkers(u: uPlot): void {
    if (this.markers.length === 0) {
      return;
    }
    const pxr = window.devicePixelRatio || 1;
    const { left, top, width, height } = u.bbox; // canvas px
    const ctx = u.ctx;
    ctx.save();
    ctx.beginPath();
    ctx.rect(left, top, width, height);
    ctx.clip();
    ctx.strokeStyle = MARKER_COLOUR;
    ctx.fillStyle = MARKER_COLOUR;
    ctx.lineWidth = pxr;
    ctx.setLineDash([4 * pxr, 3 * pxr]);
    const dotRadius = 4 * pxr;
    for (const m of this.markers) {
      const x = Math.round(u.valToPos(m.x, 'x', true));
      if (x < left || x > left + width) {
        continue;
      }
      ctx.beginPath();
      ctx.moveTo(x, top + dotRadius * 2);
      ctx.lineTo(x, top + height);
      ctx.stroke();
      ctx.beginPath();
      ctx.arc(x, top + dotRadius + pxr, dotRadius, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
  }

  /** The marker nearest a client x position, if within MARKER_HIT_PX of it. */
  private markerNear(clientX: number): CommentMarker | undefined {
    const chart = this.chart;
    if (!chart || this.markers.length === 0) {
      return undefined;
    }
    const plotX = clientX - chart.over.getBoundingClientRect().left;
    let best: CommentMarker | undefined;
    let bestDist = MARKER_HIT_PX;
    for (const m of this.markers) {
      const pos = chart.valToPos(m.x, 'x');
      if (pos < 0 || pos > chart.over.clientWidth) {
        continue;
      }
      const dist = Math.abs(pos - plotX);
      if (dist <= bestDist) {
        best = m;
        bestDist = dist;
      }
    }
    return best;
  }

  /** Show the popup for a marker, beside its line and kept inside the chart. */
  private showPopup(marker: CommentMarker): void {
    const chart = this.chart;
    const wrapper = this.chartWrapper?.nativeElement;
    if (!chart || !wrapper) {
      return;
    }
    const wrapperRect = wrapper.getBoundingClientRect();
    const overRect = chart.over.getBoundingClientRect();
    const markerX = overRect.left - wrapperRect.left + chart.valToPos(marker.x, 'x');
    const top = overRect.top - wrapperRect.top + 14;
    const inLeftHalf = markerX < wrapperRect.width / 2;
    this.popup.set({
      top,
      left: inLeftHalf ? markerX + 8 : null,
      right: inLeftHalf ? null : wrapperRect.width - markerX + 8,
      date: new Date(marker.date).toLocaleDateString('en-GB', {
        weekday: 'short',
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      }),
      comments: marker.comments,
    });
  }

  /** Show the popup for the marker near clientX, or hide it if there's none. */
  private updatePopupAt(clientX: number): void {
    const marker = this.markerNear(clientX);
    if (marker) {
      this.showPopup(marker);
    } else {
      this.popup.set(null);
    }
  }

  /** First and last x values (unix seconds) of the data. */
  private dataExtent(): { first: number; last: number } | undefined {
    const xs = this.data?.[0];
    if (!xs || xs.length === 0) {
      return undefined;
    }
    return { first: xs[0], last: xs[xs.length - 1] };
  }

  /**
   * The window for the selected range, ending at the most recent entry (so the
   * default view always shows the latest data). `null` range means all data.
   */
  private initialWindow(): { min: number; max: number } | undefined {
    const extent = this.dataExtent();
    if (!extent) {
      return undefined;
    }
    const range = this._range;
    if (range == null) {
      return { min: extent.first, max: extent.last };
    }
    const cutoff = new Date(extent.last * 1000);
    if ('days' in range) {
      cutoff.setDate(cutoff.getDate() - range.days);
    } else {
      cutoff.setMonth(cutoff.getMonth() - range.months);
    }
    const min = Math.max(extent.first, Math.floor(cutoff.getTime() / 1000));
    return { min, max: extent.last };
  }

  /** True when the window is narrower than the data, i.e. there's somewhere to pan to. */
  private canPan(): boolean {
    const extent = this.dataExtent();
    return (
      !!extent &&
      !!this.window &&
      this.window.max - this.window.min < extent.last - extent.first
    );
  }

  private applyWindow(): void {
    if (this.chart && this.window) {
      this.chart.setScale('x', { min: this.window.min, max: this.window.max });
    }
    if (this.chart) {
      this.chart.over.style.cursor = this.canPan() ? 'grab' : '';
    }
  }

  /** Wire up drag-to-pan on uPlot's plot overlay (mouse and touch). */
  private setupPanning(over: HTMLElement): void {
    // Let vertical scrolling through; horizontal drags come to us.
    over.style.touchAction = 'pan-y';
    // Swipes on the plot pan it rather than changing page. Only the plot area is
    // marked, so swipes on the axes, legend and padding still change page.
    over.setAttribute('data-no-page-swipe', '');

    over.addEventListener('pointerdown', (e: PointerEvent) => {
      if (e.pointerType === 'mouse' && e.button !== 0) {
        return;
      }
      this.tap = { pointerId: e.pointerId, x: e.clientX, y: e.clientY };
      if (!this.window || !this.canPan()) {
        return;
      }
      this.drag = {
        pointerId: e.pointerId,
        startX: e.clientX,
        min: this.window.min,
        max: this.window.max,
      };
      over.setPointerCapture(e.pointerId);
      over.style.cursor = 'grabbing';
    });

    over.addEventListener('pointermove', (e: PointerEvent) => {
      const tap = this.tap;
      if (
        tap?.pointerId === e.pointerId &&
        Math.hypot(e.clientX - tap.x, e.clientY - tap.y) > TAP_SLOP_PX
      ) {
        // It's a drag now, not a tap; get the popup out of the way while panning.
        this.tap = undefined;
        this.popup.set(null);
      }
      // Desktop hover (no button held): show the popup for a nearby marker.
      if (e.pointerType === 'mouse' && !this.drag && !this.tap) {
        this.updatePopupAt(e.clientX);
      }
      const drag = this.drag;
      const extent = this.dataExtent();
      if (!drag || drag.pointerId !== e.pointerId || !extent || over.clientWidth === 0) {
        return;
      }
      const span = drag.max - drag.min;
      const secondsPerPx = span / over.clientWidth;
      // Dragging right moves back in time (the line follows the finger).
      let min = drag.min - (e.clientX - drag.startX) * secondsPerPx;
      min = Math.min(Math.max(min, extent.first), extent.last - span);
      this.window = { min, max: min + span };
      this.chart?.setScale('x', this.window);
    });

    const endDrag = (e: PointerEvent) => {
      if (this.drag?.pointerId !== e.pointerId) {
        return;
      }
      this.drag = undefined;
      over.style.cursor = this.canPan() ? 'grab' : '';
    };
    over.addEventListener('pointerup', (e: PointerEvent) => {
      // A tap (or click) opens the nearest marker's popup, or closes it.
      if (this.tap?.pointerId === e.pointerId) {
        this.tap = undefined;
        this.updatePopupAt(e.clientX);
      }
      endDrag(e);
    });
    over.addEventListener('pointercancel', (e: PointerEvent) => {
      if (this.tap?.pointerId === e.pointerId) {
        this.tap = undefined;
      }
      endDrag(e);
    });
    over.addEventListener('pointerleave', (e: PointerEvent) => {
      if (e.pointerType === 'mouse' && !this.drag) {
        this.popup.set(null);
      }
    });
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
      // Keep scales; the window is applied explicitly below.
      this.chart.setData(this.data, false);
    } else {
      this.chart = new uPlot(
        this.buildOptions(),
        this.data,
        this.chartContainer.nativeElement,
      );
      this.setupPanning(this.chart.over);
    }
    this.applyWindow();
  }

  private buildOptions(): uPlot.Options {
    const { width, height } = this.currentSize();
    return {
      width,
      height,
      scales: { x: { time: true } },
      // No drag-to-zoom: dragging pans the window instead.
      cursor: { drag: { x: false, y: false, setScale: false } },
      hooks: { draw: [(u) => this.drawMarkers(u)] },
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
      axes: [{ values: this.ukTimeAxisValues() }, { label: 'kg' }],
    };
  }

  /**
   * uPlot's default time-axis format tiers, but with day/month flipped to UK
   * order ({D}/{M} instead of {M}/{D}). uPlot picks a tier from how much width
   * is available, so without this the ticks silently switch to US-style
   * {M}/{D} when the chart is wide (e.g. landscape). Scale is in seconds.
   */
  private ukTimeAxisValues(): uPlot.Axis.Values {
    const s = 1;
    const m = s * 60;
    const h = m * 60;
    const d = h * 24;
    const y = d * 365;
    const _ = null;
    // [incr, default, year, month, day, hour, min, sec, mode]
    return [
      [y,      '{YYYY}',        _,                             _, _,                        _, _,            _, 1],
      [d * 28, '{MMM}',         '\n{YYYY}',                    _, _,                        _, _,            _, 1],
      [d,      '{D}/{M}',       '\n{YYYY}',                    _, _,                        _, _,            _, 1],
      [h,      '{h}{aa}',       '\n{D}/{M}/{YY}',              _, '\n{D}/{M}',              _, _,            _, 1],
      [m,      '{h}:{mm}{aa}',  '\n{D}/{M}/{YY}',              _, '\n{D}/{M}',              _, _,            _, 1],
      [s,      ':{ss}',         '\n{D}/{M}/{YY} {h}:{mm}{aa}', _, '\n{D}/{M} {h}:{mm}{aa}', _, '\n{h}:{mm}{aa}', _, 1],
    ];
  }

  private setupResizeObserver(): void {
    if (!this.chartContainer) {
      return;
    }
    this.resizeObserver = new ResizeObserver(() => {
      if (this.chart) {
        this.chart.setSize(this.currentSize());
        this.popup.set(null);
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
