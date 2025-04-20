import { Component, ViewChild, ElementRef, AfterViewInit, HostListener } from '@angular/core';
import { RouterOutlet, Router, ActivatedRoute } from '@angular/router';
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

  // Flag to track if this is the initial page load
  isInitialLoad: boolean = true;

  // Define page names for more descriptive URLs
  pageNames: string[] = ['input', 'history'];

  @ViewChild('pagesContainer') pagesContainer!: ElementRef;

  constructor(
    settingsService: SettingsService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.gitHash = settingsService.settings.gitHash;
  }

  ngAfterViewInit() {
    // Get initial page from URL parameter
    this.route.queryParams.subscribe(params => {
      const page = params['page'];
      if (page !== undefined) {
        // Find index of the page name in our array
        const pageIndex = this.pageNames.indexOf(page);
        if (pageIndex !== -1) {
          this.currentPage = pageIndex;
        }
      }

      // Apply position immediately without transition for initial load
      if (this.isInitialLoad) {
        this.disableTransitionTemporarily();
        this.updatePagePosition();
        this.isInitialLoad = false;
      } else {
        this.updatePagePosition();
      }
    });
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
      // Update URL with the current page name
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { page: this.pageNames[pageIndex] },
        queryParamsHandling: 'merge'
      });
      this.updatePagePosition();
    }
  }

  // Temporarily disable the transition effect
  private disableTransitionTemporarily() {
    if (this.pagesContainer) {
      const containerElement = this.pagesContainer.nativeElement;
      containerElement.classList.remove('transition-transform');

      // Re-enable the transition after position is set
      setTimeout(() => {
        containerElement.classList.add('transition-transform');
      }, 50);
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
