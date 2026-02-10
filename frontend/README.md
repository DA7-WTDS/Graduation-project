# SmartInvest AI - Frontend

Modern React frontend for the AI-Driven Investment Portfolio Platform.

## 🚀 Quick Start

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

The app will open at [http://localhost:3000](http://localhost:3000)

## 📁 Project Structure

```
frontend/
├── src/
│   ├── components/          # Reusable components
│   │   ├── Navbar/
│   │   ├── Hero/
│   │   ├── Features/
│   │   └── Footer/
│   ├── pages/              # Page components
│   │   └── LandingPage/
│   ├── App.jsx             # Root component
│   ├── main.jsx            # App entry point
│   └── index.css           # Global styles & design system
├── index.html              # HTML template
├── vite.config.js          # Vite configuration
└── package.json            # Dependencies
```

## 🎨 Design System

All design tokens are defined in:
- **Global CSS:** `src/index.css` (CSS variables)
- **Reference Doc:** `../FRONTEND_CONFIG.md` (comprehensive guide)

### Key Design Tokens

**Colors:**
- Primary Navy: `#0A2463`
- Primary Purple: `#6C63FF`
- Primary Teal: `#00C9A7`

**Typography:**
- Primary Font: Inter
- Display Font: Plus Jakarta Sans
- Monospace: JetBrains Mono

**Spacing:** 8px base unit (`--space-sm` to `--space-5xl`)

## 📄 Current Pages

### ✅ Landing Page
- **Navbar:** Fixed navigation with scroll effects
- **Hero:** Full-screen hero with CTAs and gradient background
- **Features:** 6 feature cards in responsive grid
- **Footer:** Multi-column footer with links

## 🛠️ Tech Stack

- **React 18** - UI framework
- **Vite 5** - Build tool and dev server
- **CSS Variables** - Design system
- **Google Fonts** - Inter, Plus Jakarta Sans

## 📱 Responsive Design

The landing page is fully responsive with breakpoints at:
- Mobile: < 768px
- Tablet: 768px - 1023px
- Desktop: 1024px+

## ♿ Accessibility

- WCAG AA color contrast compliance
- Keyboard navigation support
- Focus states on all interactive elements
- Reduced motion support for animations
- Semantic HTML structure

## 🎯 Next Steps

- [ ] Add routing (React Router)
- [ ] Build onboarding flow page
- [ ] Create dashboard page
- [ ] Implement authentication
- [ ] Connect to backend API

## 📚 Development Guidelines

1. **Always reference `FRONTEND_CONFIG.md`** for design decisions
2. **Use CSS variables** instead of hardcoded values
3. **Follow component structure:** Each component in its own folder with `.jsx` and `.css`
4. **Mobile-first approach:** Design for mobile, enhance for desktop
5. **Test responsiveness** at all breakpoints

## 🔗 Related Documentation

- [Project Analysis](../brain/project_analysis.md)
- [Landing Page Design Spec](../brain/landing_page_design.md)
- [Frontend Config](../FRONTEND_CONFIG.md)

---

Built with ❤️ for graduation project 2026
