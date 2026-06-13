# Frontend Configuration & Design System Reference
## QuantWise — "Quant Terminal"

> **Purpose:** Single source of truth for QuantWise's frontend styling and design tokens.
> The UI is an **instrument, not a brochure**: a dark plotting-desk where the daily
> risk-graded run is the live readout. Dark, mono-forward, phosphor-amber, signal-coded.
>
> **Canonical tokens live in [`frontend/src/shared/styles/tokens.css`](frontend/src/shared/styles/tokens.css).**
> This doc mirrors them for reference — keep the two in sync.

---

## 🎨 Color Palette

### Surfaces
```css
--qw-ink:           #0B0E11;   /* app canvas */
--qw-panel:         #12161B;   /* raised surfaces */
--qw-panel-2:       #171C22;   /* nested / hover surfaces */
--qw-grid:          rgba(255,255,255,.04);  /* faint plotting grid */
--qw-border:        rgba(255,255,255,.08);
--qw-border-strong: rgba(255,255,255,.14);
```

### Brand / accent
```css
--qw-amber:      #FFB000;   /* dominant accent / brand */
--qw-amber-dim:  #B57A00;
--qw-amber-glow: rgba(255,176,0,.14);
```

### Text
```css
--qw-text:       #E8EAED;
--qw-text-dim:   #8A9099;
--qw-text-faint: #5A6068;
```

### Signal system — **BUY / SELL / HOLD**
Drives every recommendation pill / action chip.
```css
--qw-buy:  #3DDC84;   /* green  */
--qw-sell: #FF5247;   /* red    */
--qw-hold: #FFB000;   /* amber  */
```

### Per-stock risk level — **LOW / MEDIUM / HIGH**
Comes from the Pipeline's risk grading (`Pipeline/risk_rules.py`), surfaced read-only.
```css
--qw-low:  #3DDC84;
--qw-med:  #FFB000;
--qw-high: #FF5247;
```

### Gradients
```css
--gradient-text: linear-gradient(135deg, #FFB000 0%, #FFD36B 100%);  /* amber wordmark */
--gradient-cta:  linear-gradient(135deg, #FFB000 0%, #B57A00 100%);
--gradient-hero: linear-gradient(135deg, #0E1217 0%, #1A140A 100%);
```

> **Removed:** the old purple/teal system (`#6C63FF`, `#00C9A7`, `#0A2463`) and the
> purple→teal gradient. Legacy `--color-*` names still resolve via deprecated
> compatibility shims in `tokens.css` while pages are migrated — do **not** author new
> styles against them; use `--qw-*`. Shims are deleted in Phase 4/5.

---

## 📝 Typography

Self-hosted via `@fontsource` (imported in `frontend/src/shared/styles/fonts.ts`) — **no CDN in prod**.

```css
--qw-font-display: 'Martian Mono', ui-monospace, monospace;        /* wordmark + all-caps labels */
--qw-font-mono:    'Spline Sans Mono', ui-monospace, monospace;    /* data / figures (tabular) */
--qw-font-body:    'Hanken Grotesk', -apple-system, sans-serif;    /* body / UI */
```

- **Display / wordmark** = Martian Mono (700, tracked, all-caps for labels — used sparingly).
- **Data / figures** = Spline Sans Mono (tabular numerals; `font-feature-settings: 'tnum'`).
- **Body / UI** = Hanken Grotesk.
- **No Inter, no Plus Jakarta Sans, no JetBrains Mono, no Space Grotesk anywhere.**

### Type scale
```css
--qw-fs-hero: 64px;   --qw-fs-h1: 44px;  --qw-fs-h2: 34px;  --qw-fs-h3: 24px;
--qw-fs-h4:   19px;   --qw-fs-fig: 21px; /* metric figure */
--qw-fs-base: 15px;   --qw-fs-sm: 13px;  --qw-fs-xs: 11px;  /* dense reason lines */
```
Mobile (`max-width: 768px`) steps hero/h1/h2/h3 down (see `tokens.css`).

---

## 📐 Spacing (8px base)
```css
--space-xs:4px; --space-sm:8px; --space-md:16px; --space-lg:24px; --space-xl:32px;
--space-2xl:48px; --space-3xl:64px; --space-4xl:80px; --space-5xl:120px;
```

## 🎯 Border Radius (terminal = tighter corners)
```css
--radius-sm:3px; --radius-md:6px; --radius-lg:10px; --radius-xl:14px; --radius-2xl:20px; --radius-full:9999px;
```

## 🌈 Shadows (tuned for dark surfaces)
```css
--shadow-sm: 0 1px 2px rgba(0,0,0,.4);
--shadow-md: 0 4px 12px rgba(0,0,0,.45);
--shadow-lg: 0 12px 32px rgba(0,0,0,.55);
--shadow-xl: 0 20px 48px rgba(0,0,0,.6);
--shadow-glow: 0 0 24px var(--qw-amber-glow);
```

## 🎭 Transitions
```css
--transition-fast: 120ms ease;  --transition-base: 200ms ease;  --transition-slow: 320ms ease;
```

---

## ✨ Atmosphere & density

- Layered background: `--qw-ink` base + faint repeating plotting grid (`--qw-grid`) +
  one soft radial amber glow per view + the animated SVG **risk-surface** behind the hero.
- **Dense** terminal rows on data surfaces (one line per pick + 11px reason line);
  spacious only on hero / empty / auth moments.

## 🧩 Signature elements
- **Risk-surface banner** — amber wireframe/ridgeline (stacked `<path>` polylines, opacity
  ramp dim→bright), animated with CSS keyframes (slow drift + line draw-on). SVG/CSS, **not WebGL**.
- **Recommendation rows** — grid `[ticker | signal pill | conviction bar | conf | alloc%]`
  + 11px muted reason line. Signal pills use buy/sell/hold colors at 0.12-alpha fill + matching border.
- **Metric tiles** — small `--qw-panel` cards, 10–11px mono label + 21px figure.

## 🎬 Motion
- Framer Motion (`motion` pkg): per-route orchestrated load (grid fade → wordmark reveal →
  rows stagger → conviction bars fill → figures count up) + homepage scroll animations
  (`whileInView`, `useScroll`/`useTransform`).
- Everything gated behind `prefers-reduced-motion` (reveal instantly, freeze the SVG, no parallax).

---

## ♿ Accessibility
- WCAG-AA contrast on the dark palette (verify amber-on-ink for small text).
- Visible focus rings: `outline: 2px solid var(--qw-amber); outline-offset: 2px;`
- `prefers-reduced-motion` honored globally (see `index.css`).

---

**Last Updated:** 2026-06-13 (Phase 0 — Quant Terminal foundation)
**Note:** Update this file whenever `tokens.css` changes.
