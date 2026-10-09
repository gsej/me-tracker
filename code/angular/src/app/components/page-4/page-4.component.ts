import { Component, ChangeDetectionStrategy } from '@angular/core';

import { WeightChartComponent } from '../weight-chart/weight-chart.component';

@Component({
  selector: 'app-page-4',
  imports: [WeightChartComponent],
  templateUrl: './page-4.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./page-4.component.scss'],
})
export class Page4Component {
  readonly rangeOptions: { label: string; months: number | null }[] = [
    { label: '3M', months: 3 },
    { label: '1Y', months: 12 },
    { label: 'All', months: null },
  ];

  selectedRange: number | null = 3;
}
