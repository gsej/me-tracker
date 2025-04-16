import { Component } from '@angular/core';
import { WeightInputComponent } from '../weight-input/weight-input.component';

@Component({
  selector: 'app-page-1',
  standalone: true,
  imports: [WeightInputComponent],
  templateUrl: './page-1.component.html',
  styleUrl: './page-1.component.scss'
})
export class Page1Component {

}
