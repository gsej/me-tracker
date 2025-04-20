import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WeightRecordsComponent } from '../weight-records/weight-records.component';

@Component({
  selector: 'app-page-2',
  standalone: true,
  imports: [CommonModule, WeightRecordsComponent],
  templateUrl: './page-2.component.html',
  styleUrls: ['./page-2.component.scss']
})
export class Page2Component {
  // This component now only acts as a container
}
