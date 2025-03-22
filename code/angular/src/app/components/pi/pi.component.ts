import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-pi',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="pi-container text-lg">
      <span class="pi-symbol">&pi;</span>
      <span class="hover-text">&nbsp;&nbsp;{{ text }}</span>
    </div>
  `,
  styles: [`
    .hover-text {
      display: none;
    }
    .pi-container:hover .hover-text {
      display: inline;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PiComponent {

  @Input()
  text = "";
}
