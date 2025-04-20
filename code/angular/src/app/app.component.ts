import { Component, ViewChild, ElementRef, AfterViewInit, HostListener } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { SettingsService } from './settings/settings.service';
import { PiComponent } from './components/pi/pi.component';
import { ApiKeyInputComponent } from './components/api-key-input/api-key-input.component';
import { Page1Component } from './components/page-1/page-1.component';
import { Page2Component } from './components/page-2/page-2.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CommonModule,
    Page1Component,
    Page2Component,
    PiComponent,
    ApiKeyInputComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements AfterViewInit {
  title = 'me-tracker';
  public gitHash: string = 'not set';

  currentPage: number = 0;
  totalPages: number = 2;
  startX: number = 0;

  @ViewChild('pagesContainer') pagesContainer!: ElementRef;

  constructor(settingsService: SettingsService) {
    this.gitHash = settingsService.settings.gitHash;
  }

  ngAfterViewInit() {
    this.updatePagePosition();
  }

  onTouchStart(event: TouchEvent) {
    this.startX = event.touches[0].clientX;
  }

  onTouchMove(event: TouchEvent) {
    const currentX = event.touches[0].clientX;
    const diff = this.startX - currentX;

    if (Math.abs(diff) > 50) {
      if (diff > 0 && this.currentPage < this.totalPages - 1) {
        // Swipe left
        this.goToPage(this.currentPage + 1);
      } else if (diff < 0 && this.currentPage > 0) {
        // Swipe right
        this.goToPage(this.currentPage - 1);
      }
      this.startX = currentX;
    }
  }

  @HostListener('window:resize')
  onResize() {
    this.updatePagePosition();
  }

  goToPage(pageIndex: number) {
    if (pageIndex >= 0 && pageIndex < this.totalPages) {
      this.currentPage = pageIndex;
      this.updatePagePosition();
    }
  }

  private updatePagePosition() {
    if (this.pagesContainer) {
      const containerElement = this.pagesContainer.nativeElement;
      const containerWidth = containerElement.offsetWidth;
      containerElement.style.transform = `translateX(-${this.currentPage * containerWidth}px)`;
    }
  }
}
