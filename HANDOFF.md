# QuantWise — Frontend Refactor & Redesign · Handoff

> Handoff for continuing the frontend refactor in a new chat. Read this top-to-bottom first.
> **Status: planning complete & approved. Nothing implemented yet.** Next action = Phase 0.
> Full plan file (more detail): `C:\Users\siso2\.claude\plans\bright-exploring-puffin.md`

---

## 0. TL;DR of what we're doing

Refactor + redesign the React frontend in `frontend/` to:
1. **Re-skin** it into a distinctive **"Quant Terminal"** aesthetic (dark, mono, phosphor-amber, signal-coded) — killing the current generic "AI-slop" look (Inter + purple→teal gradient on white).
2. **Re-architect** to **TypeScript + TanStack Query**, a single shared app shell, and reusable UI primitives.
3. **Wire the real product** — the LLM recommendations endpoint that currently never reaches the UI.
4. Add **scroll animations on the homepage** (Framer Motion). **No 3D / WebGL** (decided against — see §6).

---

## 1. What QuantWise is

Decision-support stock advisory (NOT auto-trading). A daily batch produces **market-wide, risk-graded stock predictions**; at serve time an LLM (Google Gemini) **personalizes** BUY/WATCH/AVOID picks + allocation per user using their risk profile.

Polyglot system: Python ML + a JS risk-rules node + n8n orchestration + .NET 10 modular-monolith backend + Gemini.

**⚠️ This repo contains ONLY two pieces:** the **.NET 10 backend** (`Backend/`) and the **React + Vite frontend** (`frontend/`). The Python ML services, JS risk-rules node, and n8n workflows live **outside this repo** — they push the daily batch in via the Recommendations *ingest* endpoint. Don't look for them here.

---

## 2. Backend (the part the frontend talks to)

Clean **modular monolith**. Modules: **Users, Portfolio, Notifications, Recommendations**. Each split into `Domain / Application / Infrastructure / Presentation / IntegrationEvents / PublicApi`. Stack: MediatR (CQRS), FluentResults, EF Core (one DbContext per module), Outbox/Inbox + RabbitMQ/MassTransit, Redis HybridCache, JWT, minimal-API `IEndpoint` registration.

**Core flow** ([GetRecommendationsQueryHandler.cs](Backend/src/Modules/Recommendations/Project.Modules.Recommendations.Application/Recommendations/GetRecommendations/GetRecommendationsQueryHandler.cs)):
1. External batch → ingest → stores a `DailyRun` with market-wide `StockPrediction`s (direction, confidence, sentiment, risk level, conviction, risk flags…).
2. `GET /api/recommendations` → loads latest run + the user's `Portfolio` risk profile → builds a constrained prompt → Gemini returns schema-constrained JSON `{summary, picks[]}` (retried up to 3× on parse failure) → cached 12h in Redis → returned.

### API contract the frontend must model
- `GET /api/recommendations` → `{ summary: string; generated_at: string; picks: { ticker: string; action: "BUY"|"WATCH"|"AVOID"; allocation_pct: number; reason: string; risk_note: string; fit: string }[] }`
- Auth: `POST /users/register`, `POST /users/login` → `{ accessToken }`, `GET /users/profile`
- Portfolio: `GET /portfolios/me`, `POST /portfolios`, `PUT /portfolios/{id}` (questionnaire + allocations + `riskProfile`)
- Notifications: `GET /notifications?page=&pageSize=`, `GET /notifications/unread-count`, `PUT /notifications/{id}/read`, `PUT /notifications/read-all`, `POST /notifications/test` (debug)

Backend runs at **http://localhost:5000** (`dotnet run` from `Backend/src/API/Project.Api`). Infra via `docker-compose up -d` (Postgres 18, Redis, RabbitMQ, Mailpit, pgAdmin). `RiskProfile` enum = `Conservative | Moderate | Aggressive`.

---

## 3. Frontend — current state (the refactor target)

React 18 + Vite + react-router v6, **plain JS**, hand-rolled `fetch` wrapper, Context auth, one CSS file per page. Pages: Landing, Login, Signup, Onboarding (4-step questionnaire), Dashboard, Portfolios (placeholder), Simulator (mock), Market (mock), Profile.

### Critical problems found
| # | Problem | Evidence |
|---|---------|----------|
| 1 | **Product not wired up.** `Dashboard.jsx` runs on hard-coded `MOCK_DATA`; `GET /api/recommendations` (the whole point) never reaches the UI. No recommendations service exists. | [Dashboard.jsx:8](frontend/src/pages/Dashboard/Dashboard.jsx) |
| 2 | **Branding mismatch** — product is "QuantWise" but every screen hardcodes "SmartInvest AI". | global |
| 3 | **App shell duplicated inline** across Dashboard, Profile, Market, Simulator, Portfolios (logo + nav + notifications). No shared layout. | 5 files |
| 4 | **Risk grading duplicated client-side** in `Onboarding.jsx` (calculateRiskScore/generatePortfolio) — computes allocation %s + RiskProfile in JS, then POSTs them. | [Onboarding.jsx:76](frontend/src/pages/Onboarding/Onboarding.jsx) |
| 5 | Market & Simulator are fully mock (`utils/mockHistoricalData.js`, `utils/simulatorEngine.js`). | `utils/` |
| 6 | No 401→logout interceptor, no loading/error/empty primitives, inline styles, `window.triggerTestNotification` debug hook left in Dashboard. | various |
| 7 | **API route prefix inconsistent**: Recommendations uses `/api/recommendations`; Users/Portfolio/Notifications have NO prefix. Frontend handles both by passing full paths. (Flag to backend as a follow-up; out of scope.) | backend endpoints |

Other notes: README says ".NET 8" but it's actually **.NET 10**. "Remember me" checkbox on Login is non-functional.

---

## 4. Locked decisions (agreed with the user)

- **Language:** migrate to **TypeScript** (`.tsx`/`.ts`).
- **Data/server state:** **TanStack Query** (`@tanstack/react-query`).
- **Aesthetic:** **Direction A — "Quant Terminal"** (dark, mono-forward, phosphor amber, dense terminal density on data surfaces). Chosen over an "Editorial Broadsheet" (light/serif) and a "Risk Cartography" (teal/contour) option.
- **3D:** **NO live 3D / WebGL / React Three Fiber.** Dropped after discussion — it fights the flat terminal aesthetic and has poor cost/benefit. (See §6.)
- **Motion:** **Framer Motion** for page-load orchestration + micro-interactions, and **scroll-driven animations on the homepage** (user explicitly wants these).
- **Signature visual:** the amber "risk surface" ridgeline is an **animated SVG/CSS** element, NOT WebGL.
- **Higgsfield renders:** optional/post-hoc only; not a build dependency.
- **Market & Simulator:** keep, but clearly **badge as "Demo / Learning"** (stay mock-driven for now).
- **Fonts:** self-host via `@fontsource` (no CDN in prod).

---

## 5. Design system — "Quant Terminal"

Concept: the UI is an *instrument*, not a brochure. Dark plotting-desk where the daily risk-graded run is the readout. Replace the entire `:root` in `frontend/src/index.css` (delete the purple/Inter system) and update `FRONTEND_CONFIG.md`.

### Tokens
```
--qw-ink:        #0B0E11   /* canvas */
--qw-panel:      #12161B   /* raised surfaces */
--qw-panel-2:    #171C22
--qw-grid:       rgba(255,255,255,.04)   /* faint plotting grid */
--qw-amber:      #FFB000   /* dominant accent / brand */
--qw-amber-dim:  #B57A00
--qw-text:       #E8EAED
--qw-text-dim:   #8A9099
--qw-text-faint: #5A6068
/* signal system — drives BUY/WATCH/AVOID + risk level everywhere */
--qw-buy:  #3DDC84   --qw-watch: #FFB000   --qw-avoid: #FF5247
--qw-low:  #3DDC84   --qw-med:   #FFB000   --qw-high:  #FF5247
```

### Typography (self-hosted)
- Display / wordmark = **Martian Mono** (700, tracked, all-caps for labels — used sparingly)
- Data / figures = **Spline Sans Mono** (tabular)
- Body / UI = **Hanken Grotesk**
- **No Inter, no Space Grotesk anywhere.**

### Atmosphere & density
- Layered background: `--qw-ink` base + faint repeating plotting grid + one soft radial amber glow per view + the animated SVG risk-surface behind the hero.
- Dense terminal rows on data surfaces (one line per pick + 11px reason); spacious only on hero/empty/auth.

### Signature elements (already prototyped as mockups in chat)
- **Risk-surface banner** = amber wireframe/ridgeline (stacked sine-ish `<path>` polylines, opacity ramp from dim to bright amber) animated with CSS keyframes (slow drift + line draw-on). Behind the hero and as a dashboard accent.
- **Recommendation rows** = grid `[ticker | signal pill | conviction bar | conf | alloc%]` + 11px muted reason line. Signal pills use the buy/watch/avoid colors with 0.12-alpha fills + matching border. Conviction bars animate width on mount.
- **Metric tiles** = small `--qw-panel` cards, 10px mono label + 21px figure.

> We rendered two reference mockups in chat via the visualize tool: (a) the three design directions, (b) a full Quant Terminal **dashboard** comp (window chrome → nav with bell+avatar → "TODAY'S RUN" header → risk-surface banner → 4 metric tiles → AI RECOMMENDATIONS feed with NVDA buy / MSFT buy / AAPL watch / TSLA avoid → target-mix allocation strip → "not financial advice" footer). The dashboard comp is the visual target for the build.

### Motion
- Per-route **orchestrated load**: grid fade-in → wordmark reveal → rows stagger (`staggerChildren`) → conviction bars fill → figures count up.
- Micro-interactions: scanline hover on rows, signal-pill pulse, number count-ups.
- **Homepage scroll animations** (the showcase): `whileInView` section reveals (`viewport={{ once: true, margin: "-15%" }}`), staggered feature/step grids, subtle hero `RiskSurface` parallax via `useScroll`/`useTransform`, scroll-progress accent on nav.
- **Everything gated behind `prefers-reduced-motion`** (reveal instantly, no parallax, freeze SVG).

---

## 6. Why we dropped 3D (for the next chat's context)

The flat terminal aesthetic signals precision/seriousness; glossy cinematic 3D reads as marketing gloss and risks the exact "AI slop" we're avoiding. A WebGL wireframe surface's payoff over a good animated SVG is marginal, and it adds a heavy dep + mobile/perf/maintenance cost. Scroll animations + animated SVG deliver the "premium designed" feel without the downside. **Only exception worth revisiting later:** a *data-bound* 3D constellation (today's picks plotted by risk×conviction, rotatable) — functional, not decorative — could be added as an isolated post-v1 enhancement. Not in scope now.

---

## 7. New dependencies & target structure

**Add:** `@tanstack/react-query`, `motion` (Framer Motion), `@fontsource/martian-mono`, `@fontsource/spline-sans-mono`, `@fontsource/hanken-grotesk`. Plus TS toolchain (`typescript`, `@types/*`, `tsconfig.json`, `vite-env.d.ts`, Vite `@/` alias). **Do NOT add** `three` / `@react-three/*`.

```
frontend/src/
  app/            App.tsx, router, providers (QueryClient, Auth), AppShell layout route
  shared/
    api/          client.ts (fetch + 401 interceptor), queryClient.ts, hooks
    ui/           Button, Card, SignalPill, ConvictionBar, StatTile, Loading/Empty/Error states
    motion/       variants.ts, useInView, scroll helpers
    visuals/      RiskSurface.tsx (animated SVG/CSS), grid/glow backdrops
    styles/       tokens.css, fonts.ts, global.css
  features/
    auth/         Login, Signup, hooks, authApi
    onboarding/   stepper (questionnaire)
    portfolio/    portfolioApi, hooks, types
    recommendations/  RecommendationsPanel, useRecommendations, types  ← NEW, the product
    notifications/    NotificationBell, useNotifications  ← extracted from Dashboard
    market/ simulator/   demo pages (labelled "Demo / Learning")
  types/          api.ts (shared DTOs mirroring backend)
```

---

## 8. Implementation phases

- **Phase 0 — Tooling & foundation:** add TS + tsconfig + `@/` alias; install deps; self-host fonts; write `shared/styles/tokens.css` (palette above) + rewrite `index.css` reset, deleting the purple/Inter system; update `FRONTEND_CONFIG.md`; global rename **"SmartInvest AI" → "QuantWise"**; harden `shared/api/client.ts` (typed `apiCall`, central 401→clear token+redirect to /login, normalized errors); remove `window.triggerTestNotification`.
- **Phase 1 — App shell, primitives, motion:** `AppShell` layout route (nav + `NotificationBell` + avatar) wrapping authed routes via react-router nested layout → deletes the 5× duplicated headers. Build `shared/ui` primitives + `shared/motion/variants.ts`.
- **Phase 2 — Data layer + wire the product:** `QueryClientProvider` at root; convert auth/portfolio/notification services to typed query/mutation hooks; build `features/recommendations` (`useRecommendations()` → `GET /api/recommendations`, `RecommendationsPanel`); **replace Dashboard `MOCK_DATA` entirely** with live recs + real portfolio + notifications; extract `NotificationBell` + `useNotifications`.
- **Phase 3 — Visuals + motion system:** `RiskSurface.tsx` (animated SVG) + grid/glow backdrops; `shared/motion/` page-load variants + `useInView` + scroll helpers.
- **Phase 4 — Page redesigns:** Landing (scroll-animated showcase), Auth (terminal-card, fix "remember me", real error states), Onboarding (re-skin; treat backend as source of truth for risk grading), Dashboard (live product + SVG risk-surface accent), Profile (real risk profile + allocation), Market/Simulator (re-skin + "Demo/Learning" badge).
- **Phase 5 — Polish/a11y/perf:** skeletons, focus states, WCAG-AA contrast on dark palette, `prefers-reduced-motion` audit, route code-splitting, `tsc --noEmit` + `vite build` pass.

---

## 9. Verification
- `npm run dev` → Landing: fonts load (Martian/Spline/Hanken, **no Inter**), SVG `RiskSurface` animates, scroll triggers section reveals/parallax; reduced-motion shows everything instantly, no parallax.
- Backend up (`docker-compose up -d` + `dotnet run`) and one daily run ingested → log in → Dashboard fetches **real** `/api/recommendations` (verify in Network tab, not mock) and renders signal-coded picks.
- Notifications bell polls + marks read across pages from the shared shell.
- `npx tsc --noEmit` clean; `vite build` succeeds; 401 interceptor redirects to /login on expiry.

---

## 10. Cost / model guidance (the user is on Claude Pro)

- Pro is **not** metered in tokens — it's rolling 5-hour + weekly usage limits; check with `/usage` in Claude Code. This whole refactor is large enough to span multiple windows on Opus.
- Rough estimate for the full plan: ~150k–350k **output** tokens; ~8M–20M **total processed** (mostly cached input).
- **Recommendation:** run the bulk/mechanical phases (TS conversions, CSS, boilerplate services) on **Sonnet 4.6** ($3/$15) and reserve **Opus 4.8** for the trickier bits (motion/scroll system, architecture). **Fable 5** is 2× Opus's per-token price and won't help limits — avoid for this.
- Work in phases so you can stop/resume across usage windows.

---

## 11. Current status / immediate next step

- ✅ Codebase fully explored; ✅ plan written & approved; ✅ design direction locked; ✅ 3D dropped, scroll animations in.
- ❌ **No code written yet.**
- **Next:** start **Phase 0**. (Branch first — currently on `main`.)
- Memory files for this project live at `C:\Users\siso2\.claude\projects\D--Grad-Backend-Graduation-project\memory\` (`project-overview.md`, `frontend-gotchas.md`).
