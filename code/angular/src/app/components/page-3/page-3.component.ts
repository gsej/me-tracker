import { Component } from '@angular/core';

import { WeightReportComponent } from '../weight-report/weight-report.component';

@Component({
  selector: 'app-page-3',
  imports: [WeightReportComponent],
  templateUrl: './page-3.component.html',
  styleUrls: ['./page-3.component.scss'],
})
export class Page3Component {
  // This component now only acts as a container
}
