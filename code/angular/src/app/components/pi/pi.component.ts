import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-pi',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex items-center justify-end h-10 cursor-pointer" (click)="toggleText()">
      <span class="mr-2" [class.hidden]="!isTextVisible">{{ text }}</span>
      <span class="text-lg">&pi;</span>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PiComponent {
  @Input() text = "";
  isTextVisible = false;

  toggleText() {
    this.isTextVisible = !this.isTextVisible;
  }
}
