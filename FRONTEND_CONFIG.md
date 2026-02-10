# Frontend Configuration & Design System Reference
## AI-Driven Investment Portfolio Platform

> **Purpose:** This file serves as the single source of truth for all frontend styling, design tokens, and configuration. Keep all pages and components consistent with these standards.

---

## 🎨 Color Palette

### Primary Colors
```css
--color-primary-navy: #0A2463;      /* Trust, stability, main brand */
--color-primary-purple: #6C63FF;    /* AI, innovation, CTAs */
--color-primary-teal: #00C9A7;      /* Growth, success, accents */
```

### Neutral Colors
```css
--color-white: #FFFFFF;
--color-background-light: #F8F9FA;  /* Section backgrounds */
--color-gray-100: #F1F3F5;
--color-gray-200: #E9ECEF;
--color-gray-300: #DEE2E6;
--color-gray-400: #CED4DA;
--color-gray-500: #ADB5BD;
--color-gray-600: #6C757D;
--color-gray-700: #495057;
--color-gray-800: #343A40;
--color-gray-900: #212529;
--color-dark: #1E1E2E;              /* Text, dark backgrounds */
--color-footer-dark: #0A1929;       /* Footer background */
```

### Semantic Colors
```css
--color-success: #00D084;           /* Positive returns, gains */
--color-danger: #FF6B6B;            /* Losses, warnings */
--color-info: #4DABF7;              /* Information */
--color-warning: #FFB84D;           /* Caution */
```

### Gradients
```css
--gradient-hero: linear-gradient(135deg, #0A2463 0%, #6C63FF 100%);
--gradient-cta: linear-gradient(135deg, #6C63FF 0%, #00C9A7 100%);
--gradient-card-hover: linear-gradient(135deg, rgba(108,99,255,0.1) 0%, rgba(0,201,167,0.1) 100%);
--gradient-text: linear-gradient(135deg, #6C63FF 0%, #00C9A7 100%);
```

---

## 📝 Typography

### Font Families
```css
--font-primary: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif;
--font-display: 'Plus Jakarta Sans', 'Inter', sans-serif;
--font-mono: 'JetBrains Mono', 'Courier New', monospace;
```

**CDN Links (add to index.html):**
```html
<!-- Google Fonts -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=Plus+Jakarta+Sans:wght@600;700;800&family=JetBrains+Mono:wght@500&display=swap" rel="stylesheet">
```

### Type Scale
```css
/* Hero & Display */
--font-size-hero: 72px;
--line-height-hero: 1.1;
--letter-spacing-hero: -0.02em;

/* Headings */
--font-size-h1: 48px;
--font-size-h2: 40px;
--font-size-h3: 32px;
--font-size-h4: 24px;
--font-size-h5: 20px;
--font-size-h6: 18px;

/* Body */
--font-size-large: 20px;
--font-size-base: 16px;
--font-size-small: 14px;
--font-size-xs: 12px;

/* Line Heights */
--line-height-tight: 1.2;
--line-height-normal: 1.5;
--line-height-relaxed: 1.6;

/* Font Weights */
--font-weight-regular: 400;
--font-weight-medium: 500;
--font-weight-semibold: 600;
--font-weight-bold: 700;
--font-weight-extrabold: 800;
```

### Responsive Typography (Mobile)
```css
@media (max-width: 768px) {
  --font-size-hero: 48px;
  --font-size-h1: 36px;
  --font-size-h2: 32px;
  --font-size-h3: 24px;
}
```

---

## 📐 Spacing System

Use consistent spacing scale (8px base unit):

```css
--space-xs: 4px;
--space-sm: 8px;
--space-md: 16px;
--space-lg: 24px;
--space-xl: 32px;
--space-2xl: 48px;
--space-3xl: 64px;
--space-4xl: 80px;
--space-5xl: 120px;
--space-6xl: 160px;
```

### Section Padding
```css
--section-padding-desktop: 120px;
--section-padding-mobile: 60px;
```

---

## 🎯 Border Radius

```css
--radius-sm: 4px;
--radius-md: 8px;
--radius-lg: 12px;
--radius-xl: 16px;
--radius-2xl: 24px;
--radius-full: 9999px;
```

---

## 🌈 Shadows

```css
--shadow-xs: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
--shadow-sm: 0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06);
--shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
--shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
--shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
--shadow-2xl: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
--shadow-glow: 0 0 20px rgba(108, 99, 255, 0.3);
```

---

## 📱 Responsive Breakpoints

```css
/* Mobile first approach */
--breakpoint-sm: 640px;   /* Small devices */
--breakpoint-md: 768px;   /* Tablets */
--breakpoint-lg: 1024px;  /* Small laptops */
--breakpoint-xl: 1280px;  /* Desktops */
--breakpoint-2xl: 1536px; /* Large screens */
```

**Usage:**
```css
/* Mobile first */
.element {
  width: 100%;
}

/* Tablet and up */
@media (min-width: 768px) {
  .element {
    width: 50%;
  }
}

/* Desktop and up */
@media (min-width: 1024px) {
  .element {
    width: 33.333%;
  }
}
```

---

## 🎭 Transitions & Animations

### Standard Transitions
```css
--transition-fast: 150ms ease-in-out;
--transition-base: 200ms ease-in-out;
--transition-slow: 300ms ease-in-out;
--transition-slower: 500ms ease-in-out;
```

### Common Animations
```css
/* Fade in from bottom */
@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Counter animation for numbers */
@keyframes countUp {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* Subtle float */
@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-10px); }
}

/* Pulse (for CTAs) */
@keyframes pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.05); }
}
```

---

## 🔘 Component Standards

### Buttons

**Primary Button:**
```css
background: var(--gradient-cta);
color: white;
padding: 16px 32px;
border-radius: var(--radius-lg);
font-weight: var(--font-weight-semibold);
transition: transform var(--transition-base), box-shadow var(--transition-base);

&:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}
```

**Secondary Button:**
```css
background: transparent;
border: 2px solid white;
color: white;
padding: 16px 32px;
border-radius: var(--radius-lg);

&:hover {
  background: rgba(255, 255, 255, 0.1);
}
```

### Cards
```css
background: white;
border-radius: var(--radius-xl);
padding: var(--space-2xl);
box-shadow: var(--shadow-md);
transition: transform var(--transition-base), box-shadow var(--transition-base);

&:hover {
  transform: translateY(-8px);
  box-shadow: var(--shadow-xl);
}
```

### Inputs
```css
padding: 14px 16px;
border: 1px solid var(--color-gray-300);
border-radius: var(--radius-md);
font-size: var(--font-size-base);

&:focus {
  outline: none;
  border-color: var(--color-primary-purple);
  box-shadow: 0 0 0 3px rgba(108, 99, 255, 0.1);
}
```

---

## 📊 Layout Guidelines

### Container Widths
```css
--container-sm: 640px;
--container-md: 768px;
--container-lg: 1024px;
--container-xl: 1280px;
--container-2xl: 1400px;
```

### Grid System
```css
/* Standard 12-column grid */
display: grid;
grid-template-columns: repeat(12, 1fr);
gap: var(--space-lg);
```

---

## ♿ Accessibility Standards

### Color Contrast
- Minimum 4.5:1 for normal text
- Minimum 3:1 for large text (18px+)
- Use tools like WebAIM Contrast Checker

### Focus States
All interactive elements must have visible focus states:
```css
&:focus-visible {
  outline: 2px solid var(--color-primary-purple);
  outline-offset: 2px;
}
```

### Reduced Motion
```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

---

## 🎯 Performance Best Practices

1. **Lazy load images** below the fold
2. **Use WebP** format with fallbacks
3. **Minimize layout shifts** (CLS)
4. **Debounce scroll events** (use requestAnimationFrame)
5. **Code splitting** for routes
6. **Use CSS containment** for animation-heavy sections

---

## 📦 Component Naming Convention

Follow **BEM (Block Element Modifier)** or **Component-based naming**:

```
LandingPage/
├── LandingPage.jsx
├── LandingPage.css
├── components/
│   ├── Hero/
│   │   ├── Hero.jsx
│   │   └── Hero.css
│   ├── Features/
│   │   ├── Features.jsx
│   │   ├── Features.css
│   │   └── FeatureCard.jsx
│   └── ...
```

---

## 🚀 Quick Reference Checklist

Before deploying any new page/component:

- [ ] Uses CSS variables from this config
- [ ] Responsive on mobile, tablet, desktop
- [ ] All interactive elements have hover/focus states
- [ ] Color contrast meets WCAG AA standards
- [ ] Animations respect prefers-reduced-motion
- [ ] Images have alt text
- [ ] Buttons have aria-labels where needed
- [ ] Links have meaningful text (not "click here")
- [ ] Form inputs have associated labels

---

**Last Updated:** 2026-02-09  
**Maintained by:** Development Team  
**Questions?** Keep this file updated as the design system evolves!
