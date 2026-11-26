import { Component, ChangeDetectionStrategy } from '@angular/core';

import { WeightChartComponent, ChartRange } from '../weight-chart/weight-chart.component';

@Component({
  selector: 'app-page-4',
  imports: [WeightChartComponent],
  templateUrl: './page-4.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./page-4.component.scss'],
})
export class Page4Component {
  readonly rangeOptions: { label: string; range: ChartRange }[] = [
    { label: '1W', range: { days: 7 } },
    { label: '1M', range: { months: 1 } },
    { label: '3M', range: { months: 3 } },
    { label: '1Y', range: { months: 12 } },
    { label: 'All', range: null },
  ];

  /** 3M is the default selection. */
  selected = this.rangeOptions[2];
}
