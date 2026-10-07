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
  // This component now only acts as a container
}
