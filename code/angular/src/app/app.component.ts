import {
  Component,
  ViewChild,
  ElementRef,
  AfterViewInit,
  HostListener,
  ChangeDetectionStrategy,
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';

import { SettingsService } from './settings/settings.service';
import { HealthService } from './services/health.service';
import { PiComponent } from './components/pi/pi.component';
import { ApiKeyInputComponent } from './components/api-key-input/api-key-input.component';
import { Page1Component } from './components/page-1/page-1.component';
import { Page2Component } from './components/page-2/page-2.component';
import { Page3Component } from './components/page-3/page-3.component';
import { Page4Component } from './components/page-4/page-4.component';

@Component({
  selector: 'app-root',
  imports: [
    ReactiveFormsModule,
    Page1Component,
    Page2Component,
    Page3Component,
    Page4Component,
    PiComponent,
    ApiKeyInputComponent,
  ],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.component.scss',
})
export class AppComponent implements AfterViewInit {
  title = 'me-tracker';
  public gitHash: string = 'not set';
  public apiGitHash: string = '…';

  get hashText(): string {
    return `ui: ${this.gitHash}, api: ${this.apiGitHash}`;
  }

  currentPage: number = 0;
  totalPages: number = 4;
  startX: number = 0;

  // The horizontally-scrollable element (if any) under the finger when a swipe starts.
  // While it still has room to scroll in the swipe direction we let it scroll instead of
  // changing page, so e.g. scrolling the report table's columns no longer flips the page.
  private horizontalScroller: HTMLElement | null = null;

  isInitialLoad: boolean = true;

  pageNames: string[] = ['input', 'history', 'report', 'chart'];

  @ViewChild('pagesContainer') pagesContainer!: ElementRef;

  public swaggerUrl: string = '';

  constructor(
    settingsService: SettingsService,
    healthService: HealthService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.gitHash = settingsService.settings.gitHash;
    this.swaggerUrl = settingsService.settings.apiUrl + '/swagger/index.html';

    healthService.getApiGitHash().subscribe({
      next: (hash) => (this.apiGitHash = hash),
      error: () => (this.apiGitHash = 'unknown'),
    });
  }

  ngAfterViewInit() {
    this.route.queryParams.subscribe((params) => {
      const page = params['page'];
      if (page !== undefined) {
        const pageIndex = this.pageNames.indexOf(page);
        if (pageIndex !== -1) {
          this.currentPage = pageIndex;
        }
      }

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
    this.horizontalScroller = this.findHorizontalScroller(
      event.target as HTMLElement | null,
    );
  }

  onTouchMove(event: TouchEvent) {
    const currentX = event.touches[0].clientX;
    const diff = this.startX - currentX;

    if (Math.abs(diff) <= 100) {
      return;
    }

    // If the swipe started inside something that can still scroll horizontally in this
    // direction, let it scroll rather than changing page. Only once it's at the edge
    // (or there's no such scroller) does the swipe fall through to page navigation.
    if (this.horizontalScroller) {
      const scroller = this.horizontalScroller;
      const maxScrollLeft = scroller.scrollWidth - scroller.clientWidth;
      const atStart = scroller.scrollLeft <= 0;
      const atEnd = scroller.scrollLeft >= maxScrollLeft - 1;

      if (diff > 0 && !atEnd) {
        // Swiping left with columns still to reveal on the right.
        return;
      }
      if (diff < 0 && !atStart) {
        // Swiping right with columns still to reveal on the left.
        return;
      }
    }

    if (diff > 0 && this.currentPage < this.totalPages - 1) {
      // Swipe left
      this.goToPage(this.currentPage + 1);
    } else if (diff < 0 && this.currentPage > 0) {
      // Swipe right
      this.goToPage(this.currentPage - 1);
    }
    this.startX = currentX;
  }

  // Walk up from the touch target to the paging container, returning the first ancestor
  // that actually overflows horizontally and is allowed to scroll (overflow-x auto/scroll).
  private findHorizontalScroller(
    element: HTMLElement | null,
  ): HTMLElement | null {
    const container = this.pagesContainer?.nativeElement as
      | HTMLElement
      | undefined;

    while (element && element !== container) {
      if (element.scrollWidth > element.clientWidth) {
        const overflowX = getComputedStyle(element).overflowX;
        if (overflowX === 'auto' || overflowX === 'scroll') {
          return element;
        }
      }
      element = element.parentElement;
    }

    return null;
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
        queryParamsHandling: 'merge',
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
