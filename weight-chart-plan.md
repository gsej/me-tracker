# Plan: Moving-average weight chart page (uPlot)

Iteration 1 — a static line chart of the moving-average weight over the whole
period, on its own carousel page. Interactivity (pinch/pan/zoom) is explicitly
deferred to a later iteration.

## Context / decisions

- Frontend is Angular 22 PWA in `code/angular/`. No routed pages — `AppComponent`
  is a horizontal swipe carousel; pages are `page-1/2/3` wrapping
  `weight-input`, `weight-records`, `weight-report`. URL driven by `?page=`.
- `/api/report` already returns `averageWeight` per date — that IS the moving
  average to plot. `WeightReportService` is a root singleton exposing
  `weightReport$`, so the chart reuses the same stream (no second API call).
- Chart library: **uPlot** — tiny (~10-15KB gz), fast on mobile, framework-agnostic.
- Must be visible in portrait and usable in landscape.

## Status

- [x] Date-formatting refactor (prerequisite — see Step 0 below).
- [x] Steps 1-6 implemented; `npm run build` passes.

### Known follow-up
- `weight-report` and `weight-chart` both call `loadWeightReport()` on init, and
  all carousel pages mount at startup, so the app issues two `GET /api/report`
  calls on load. Harmless but wasteful — a later cleanup could dedupe in the
  service (skip if a load is already in flight / data is fresh).

## Step 0 (DONE): move date formatting from service to components
- `weight-report.service.ts` now passes raw API entries straight through
  (sorted newest-first); `WeightReportEntry.date` is the raw ISO string.
- `weight-report.component.ts` has a `formatDate(date)` helper using
  `toLocaleDateString()` (preserves each user's own browser locale); the
  table template calls `formatDate(entry.date)`.
- This removes the need for a separate `rawDate` field — the chart can parse
  the raw `date` directly.

## Steps

### 1. Install + global stylesheet
- `npm install uplot` (from `code/angular/`).
- In `src/styles.scss` add: `@import 'uplot/dist/uPlot.min.css';`

### 2. Data
- No service change needed — `WeightReportEntry.date` is already raw ISO after
  Step 0. The chart parses it into unix-seconds timestamps directly.

### 3. New `weight-chart` component (`components/weight-chart/`)
- Mirror `weight-report.component.ts`: inject `WeightReportService`, use
  `weightReport$ / isLoading$ / error$`, reuse loading/error/empty template blocks.
- On data: build uPlot arrays — x = unix **seconds** parsed from `date`,
  y = `averageWeight`; sort **ascending** by x (service sorts descending for the
  table, so reverse for the chart).
- Create uPlot in `ngAfterViewInit` against a `@ViewChild` container div:
  single line series, time x-axis, kg y-axis, default cursor/legend.
- Resize: `ResizeObserver` on the container → `u.setSize({ width, height })`
  (handles orientation flips + container size changes).
- Cleanup in `ngOnDestroy`: destroy uPlot instance, disconnect observer,
  unsubscribe.

### 4. New `page-4` component (`components/page-4/`)
- Wrap `weight-chart`, mirroring `page-3` layout but give the chart a filled
  flex height (`h-[calc(100%-3rem)]`, inner `flex-1 min-h-0`) instead of a
  scroll area, so it fills both portrait and landscape.

### 5. Wire into the carousel (`app.component.ts` / `.html`)
- `pageNames` → `['input','history','report','chart']`.
- `totalPages` → `4`.
- Import `Page4Component`, add to `imports`, add one
  `<div class="w-full flex-shrink-0 h-full"><app-page-4></app-page-4></div>`
  inside `#pagesContainer`.
- Pagination dots + swipe logic scale off `totalPages` automatically.

### 6. Verify
- `npm run build` passes (CI only builds the frontend; no Angular specs per
  CLAUDE.md). Sanity-check the chart renders in portrait and landscape.

## Deferred to later iterations
- Pinch-to-zoom / drag-to-pan / wheel-zoom (uPlot `setScale` + touch handlers).
- Resolving the gesture conflict with the carousel's `touchstart`/`touchmove`
  page-swipe (e.g. `stopPropagation` on chart touches, or landscape-only gestures).

## Note on dates
The service no longer formats dates — `date` is the raw ISO string from the API,
which the chart turns into a numeric timestamp and the report component formats
for display via its `formatDate()` helper. (Earlier we considered a separate
`rawDate` field or a `LOCALE_ID` + DatePipe approach; we chose the per-user
`toLocaleDateString()` helper instead.)
