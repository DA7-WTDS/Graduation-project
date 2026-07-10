# QuantWise — Go-to-Market & Revenue Plan (Egypt)

Companion to [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md). That doc says what we build; this one says how we get Egyptians to trust it and how it makes money.

---

## 1. The market reality we are selling into

| Fact | Consequence for us |
|---|---|
| ~110M population, but only a low single-digit % have ever touched the EGX | Huge headroom; near-zero category familiarity — we sell the *category* before the product |
| Widespread belief that stocks are **haram** | Must be answered with real Shariah compliance, not marketing copy |
| Deep distrust of "give me your money" products (pyramid/MLM/FX-scam history) | We must be structurally unable to touch user money — and say so loudly |
| Inflation 20–30%+ and repeated EGP devaluations (2016, 2022–24) | The pain we solve is *money melting*, not "getting rich" |
| Bank certificates at ~20%+ are the default "safe" choice | Our honest benchmark and biggest competitor — and, ironically, *interest-based* (see 2.1) |
| Gold is the trusted store of value | Gold is our gateway product, not our rival |
| Thndr has partially educated the market and proven FRA robo-licensing is attainable | Category momentum exists; we differentiate on *guided portfolios*, not another trading app |

**Positioning in one line:** *QuantWise is not a trading app. It is a plan to protect and grow your money — built for people who have never invested before.*

We never say "trade," "bet," or "beat the market." We say: protect from inflation, own real companies, plan for retirement/marriage/children, sleep well.

---

## 2. The three objections, answered structurally (not with ads)

### 2.1 "It's haram"
This is the #1 blocker and it deserves a product answer, a scholarly answer, and a reframe:

- **Product answer — Halal Mode (build it, certify it):**
  - A Shariah-compliant portfolio toggle: AAOIFI-style screening (excluded business activities; debt, cash-interest and impermissible-income ratios), applied as a filter in the instrument registry and sleeve rules (slots directly into IMPLEMENTATION_PLAN § 3.1/3.4).
  - Dividend purification: the app computes the small % of dividends to give to charity and tells the user — a feature no local competitor does well.
  - **Certified by a recognized Shariah supervisory board** — a named scholar/board on the website, renewed annually. This is a cost line, not a nice-to-have; without a named authority the toggle has no credibility.
- **Scholarly answer:** Dar Al-Iftaa and mainstream scholarship permit stock ownership in permissible businesses under screening conditions. We cite rulings, host scholars in our content, and never argue theology ourselves — we platform the people users already trust.
- **The reframe (our sharpest marketing angle):** a stock is **ownership in a real company** — a share of Juhayna's milk or Abou Ghaly's cars. The 27% bank certificate that feels "safe" is *interest* — the thing classical scholarship actually prohibits. For the audience that worries about halal, **screened stocks + gold are the compliant option and the bank CD is the questionable one.** Said respectfully, this flips the entire objection.

### 2.2 "It's too risky / gambling"
- **Lead with the risk they're already taking:** cash in EGP lost roughly half its purchasing power in a few years. Content formula: "Your 100,000 EGP from 2021 buys X today." Inaction is the risk; we quantify it relentlessly.
- **Gold as the gateway:** Egyptians already trust gold. First-time users can start with a gold-ETF-heavy stability portfolio — "digital gold, no shop premium, no storage fear" — then the app *gradually* introduces the equity sleeves as they get comfortable. The strategy-template engine already supports this as a "Starter" template.
- **Show, don't promise:** the public track record page (IMPLEMENTATION_PLAN § 5) with honest comparisons vs the deposit rate and vs inflation. We publish when we underperform too — in this market, admitting a bad month is a trust weapon no incumbent uses.
- **The simulator is a marketing asset:** the existing learning environment becomes "practice with fake money for a month" — a zero-fear on-ramp we can advertise heavily because it asks for nothing.

### 2.3 "I'm not giving you my money"
- **Structural answer:** we are advisory. User money sits at a licensed broker/custodian **in the user's own name**; QuantWise cannot withdraw, cannot touch, cannot run away with it. This gets a permanent, prominent explainer — diagram, video, one sentence on the landing page: *"فلوسك في حسابك، مش عندنا"* ("Your money stays in your account, not with us").
- **License badges:** FRA authorization displayed like a food-safety certificate. Egyptians check.
- **Humans on demand:** WhatsApp support with real people, in Egyptian Arabic. A distrustful market needs to know someone answers.
- **Flat, visible pricing** (see § 5): "We take a fixed subscription. Not a percentage of your money. No hidden cuts." — directly aimed at MLM/scam pattern-matching.

---

## 3. Marketing strategy

### 3.1 Education-first funnel (the whole strategy in one picture)

```
TikTok/Reels/YouTube (Egyptian Arabic, money-pain content)
        │  free value, no ask
        ▼
Free tier: learning env + simulator + "inflation calculator"
        │  email/phone captured, habit formed
        ▼
First real portfolio (start from ~500 EGP, gold-heavy Starter template)
        │  small money, low fear
        ▼
Subscription upgrade (full goal portfolios, monitoring, halal mode)
        │
        ▼
Referral loop ("invite family, both get a free month")
```

### 3.2 Content engine (primary CAC channel — cheap and compounding)
- **Format:** 30–60s Egyptian-Arabic videos. Not finance-bro content — kitchen-table money talk.
- **Recurring formats:**
  - *"بكرة أغلى" (Tomorrow is more expensive)* — weekly inflation reality checks on groceries/gold/school fees → the inflation calculator.
  - *"حلال ولا حرام؟"* — scholar-guest episodes on money questions (huge organic search demand, zero good supply).
  - *"محفظة ماما"* — building a real retirement portfolio for a relatable persona, updated monthly, wins and losses shown.
  - Myth-busting: "the EGX is not a casino — here's what owning a share actually means."
- **Finfluencer partnerships:** Egyptian personal-finance creators, paid *transparently* (audience trust is the asset; undisclosed promos destroy it in this market especially).
- **SEO/Arabic content site:** "هل الأسهم حلال؟", "إزاي أحمي فلوسي من التضخم؟" — high-volume queries with weak current answers.

### 3.3 Trust & community (secondary, slower, deeper)
- University partnerships (finance/CS student ambassadors — we came from a university project; that's a story, not a weakness: "built by Egyptian engineers, published at IEEE").
- Offline "أول استثمار" (First Investment) workshops in Cairo/Alex — small, free, converts skeptics who will never convert from an ad, and produces testimonial content.
- Employer channel: "financial wellness" sessions at companies — reaches salaried users (our best demographic: steady income, inflation-anxious).
- Telegram/WhatsApp community with strict no-hype rules — moderated, educational, scam-free by policy.

### 3.4 Launch sequence (mirrors the implementation phases)
| Stage | Gate | Marketing motion |
|---|---|---|
| **Private beta** | v1 (US-market, Starter+Retirement templates) | 500 hand-picked users from waitlist; obsess over activation + testimonials |
| **Public free tier** | Simulator + education polished | Content engine at full volume; inflation calculator as viral hook |
| **Paid launch** | Track record ≥ 3 months live, FRA status clear | Subscription on; halal mode certified; referral program |
| **EGX activation** | Licensed data lands (IMPL § 0.1 migration) | "Invest in Egypt, in Egyptian" campaign; EGX names people know (Talaat Moustafa, CIB, Juhayna) |
| **Scale** | Unit economics proven | Broker partnership bundles, B2B (see § 5.3) |

### 3.5 What we deliberately do NOT do
- No get-rich imagery, no Lambos, no screenshots of gains — attracts the wrong users and the regulator's ire.
- No "guaranteed returns" language ever (illegal + it's the scammer's vocabulary; our differentiation is honesty).
- No paid ads promising specific percentages.
- No crypto content adjacency — different audience, contaminates the trust position.

---

## 4. Target segments (in order of attack)

1. **The inflation-anxious salaried professional (25–40).** Has savings rotting in EGP or locked in CDs; smartphone-native; religiously cautious. Core subscription buyer. → Retirement/Balanced templates + halal mode.
2. **The gold buyer.** Already "invests," just inefficiently. → Gold-ETF gateway, then graduation.
3. **Young starters / students (18–25).** Small money, high curiosity, TikTok-native. Low revenue now, cheap to acquire, high lifetime value and the loudest referrers. → Simulator + 500-EGP starter.
4. **The diaspora angle (later):** Egyptians abroad wanting EGX/EG exposure for family — USD income, EGP anxieties reversed.

---

## 5. Revenue models — two scenarios

Two viable structures, depending on whether execution happens **inside our platform** (Thndr-style broker integration) or **outside it** (advisory-only, users execute at a partner broker). They differ in revenue mechanics, regulatory weight, and time-to-launch.

### Scenario A — Trading through our platform (broker-integrated, Thndr-style)

**What it requires first:** FRA brokerage licensing (or acquiring/deep-partnering with a licensed broker), custody via MCDR, meaningful regulatory capital, a compliance & operations team, and a longer runway before launch. This is a *different company size*, not just a feature.

**Revenue streams (in order of expected contribution):**

| # | Stream | Mechanics | Notes |
|---|--------|-----------|-------|
| A1 | **FX conversion spread** (EGP→USD) | 0.5–1.5% on every conversion funding US-stock allocations | The quiet workhorse of every Egyptian app offering US stocks; recurs on every top-up. Must be *disclosed as a visible rate* to stay consistent with § 2.3 trust positioning |
| A2 | **Execution commissions** | ~0.1–0.25% per EGX trade; flat/zero-commission US trades subsidized by A1 | Template-driven rebalancing means *low* turnover by design — commissions are steady, not casino-volume |
| A3 | **Subscriptions** (lighter than Scenario B) | Free / Plus tiers for advisory depth: halal mode, multi-goal, monitoring, tactical & IPO sleeves | Execution being in-house makes the free tier monetizable via A1/A2, so subscription paywalls can be softer |
| A4 | **Idle-cash yield** | Interest earned on uninvested client cash | ⚠️ **Direct conflict with halal positioning** — either forgo it, or structure as money-market-fund sweep with user opt-in and purification accounting. Decide deliberately; this is where trust dies quietly if mishandled |
| A5 | (Later) Securities lending, IPO distribution fees | Standard broker economics | Year 3+, same halal caution |

**Unit economics sketch (validate in beta):**
- Average funded user: ~50k EGP portfolio, ~40% US allocation → ~20k EGP converted at ~1% = **~200 EGP one-time + spread on every future top-up**; plus ~4–8 template rebalance trades/yr → ~50–150 EGP/yr commissions; plus 5–8% of users on a Plus tier.
- **Every funded user generates revenue** (not just the 4–7% who subscribe) → ARPU is 3–5× Scenario B, and revenue scales with AUM growth automatically.
- Break-even shifts from "10k subscribers" to "~15–25k *funded* users" — a different but achievable shape.

**Costs & risks specific to A:** licensing capital + 12–24 months to operational; conflict-of-interest optics (we advise AND profit from execution — mitigated structurally by template-fixed rebalance cadence, and that must be the loud public answer); operational risk (settlement failures are now *our* failures); halal tension on A4.

### Scenario B — Advisory-only (no in-app execution; users trade at partner brokers)

**What it requires first:** FRA robo-advisory authorization only. No custody, no client money, no brokerage capital. Launchable on the current implementation plan.

**Revenue streams:**

| # | Stream | Mechanics | Notes |
|---|--------|-----------|-------|
| B1 | **Subscriptions (primary)** | Tiers below | The business *is* the subscription; paywalls must be firm |
| B2 | **Introducing-broker revenue share** | Disclosed referral share of partner-broker commissions from users we route | "The broker pays us a referral fee. You pay the same either way. We never touch a % of your money" — disclosure as trust feature |
| B3 | **B2B2C white-label (year 2+)** | License the strategy-engine + questionnaire + monitoring stack to banks/brokers ("powered by QuantWise") | One mid-size bank deal can exceed retail revenue for a year; the consumer app is the living demo |

| Tier | Price (EGP/mo, annual ~2 months free) | What it unlocks |
|---|---|---|
| **Free** | 0 | Education, simulator, inflation calculator, 1 goal with the Starter (gold-heavy) template, quarterly digest |
| **Plus** | ~99–149 | Full goal templates (Retirement/Balanced), halal mode, monitoring & drawdown alerts, rebalancing guidance, track-record detail |
| **Pro** | ~249–349 | Multiple goals, Active/tactical sleeve, IPO & catalyst watchlist (gated per suitability), priority WhatsApp support |

- Anchoring: Plus ≈ a Netflix/Spotify Egypt price; Pro ≈ one restaurant dinner. Billing via cards + wallets (Vodafone Cash/InstaPay).
- Free tier stays genuinely useful forever; conversion driver is capability (monitoring, halal certification, multi-goal), not nagging.

**Unit economics sketch:**
- Only ~4–7% of actives pay → ARPU low, but CAC payback is clean and the cost base is a fraction of Scenario A's.
- B2 adds ~20–80 EGP/funded-user/yr depending on broker terms — meaningful at scale, never the headline.
- Break-even sanity check: at 129 EGP Plus, ~10k paying subscribers ≈ 1.3M EGP/mo gross — covers a lean team + infra + data licensing.

**Costs & risks specific to B:** execution friction (user must open a broker account and place trades themselves → the single biggest activation killer; mitigated by deep-linked partner onboarding and step-by-step "copy this order" UX); revenue ceiling until B3 lands; dependence on partner-broker quality we don't control.

### A vs B — decision view

| Dimension | A (integrated) | B (advisory-only) |
|---|---|---|
| Time to launch | 12–24 months (licensing) | On current plan (~months) |
| Capital needed | High (regulatory + ops) | Low |
| Who generates revenue | Every funded user | Paying subscribers (+IB share) |
| ARPU | 3–5× B | Baseline |
| Activation friction | Low (one app) | High (external broker step) |
| Trust story | Harder (we profit from trades) — needs structural guardrails | Cleanest ("we never touch your money") |
| Halal consistency | A4 needs careful structuring | No tension |
| Regulatory surface | Brokerage + advisory | Advisory only |

**Recommended path: B → A.** Launch as Scenario B on the current implementation plan (fast, cheap, trust-cleanest — and the trust story is our whole § 2). Use the IB partnership (B2) to learn execution flows and prove demand. Raise/partner into Scenario A once traction justifies the licensing investment — the strategy engine, templates, and track record carry over unchanged; A is additive infrastructure, not a rebuild. The B2B door (B3) also stays open as an alternative endgame if brokerage economics never justify the capital.

### 5.4 What we will not monetize (both scenarios)
- Selling user data — never (and say so publicly; it's a differentiator here).
- Payment for order flow — not applicable/appropriate in this market.
- Hidden spreads or "free but marked-up" products — any FX spread in Scenario A is a *published rate*.

### 5.5 Shared unit-economics targets (validate in beta)
- CAC via content/referral: target < 150 EGP blended (content-led acquisition is the whole reason for § 3.2).
- Free → paid conversion: 4–7% (robo/fintech norm) — Egypt-specific unknown, measure in beta.
- Target LTV:CAC ≥ 3 within 12 months of paid launch; churn watched monthly (high-inflation economies churn subscriptions when budgets squeeze — annual plans and family bundles are the mitigation).

---

## 6. Metrics that matter (per funnel stage)

| Stage | Metric | Early target |
|---|---|---|
| Content | view→install rate; cost per install | establish baseline |
| Activation | % installing who complete questionnaire + fund first portfolio | > 15% |
| Trust | simulator→real-money conversion; time-to-first-deposit | shrinking monthly |
| Revenue | free→paid %; MRR; churn | 4–7%; churn < 4%/mo |
| Advocacy | referral share of new users; NPS | > 25% referred |
| Integrity | complaints, FRA issues, "is this a scam?" sentiment in socials | zero tolerance / actively monitored |

---

## 7. Risks specific to this plan

- **Religious controversy risk:** a public scholar disputing our certification. Mitigation: board with recognized standing, conservative screening, purification feature, never argue theology in our own voice.
- **CD-rate risk:** if bank certificates stay near ~25%+, "why not the bank?" stays hard. Mitigation: halal framing (CDs are interest), devaluation framing (CDs are EGP-only; our portfolios hold USD assets + gold), and honest comparison content — we win the *long-horizon* and *halal-sensitive* segments first, not the CD-maximizer.
- **A market crash early in our public track record.** Mitigation: drawdown communication engine (IMPL § 3.5) is *the product working*, publicly; retirement-template users hearing "hold, here's why" during a crash — and it being right later — is the single best trust event we can have.
- **Thndr moves into guided portfolios.** Mitigation: speed on halal certification depth + goal-based engine; possible outcome is partnership/acquisition interest rather than pure competition — B2B posture (§ 5.3) keeps that door open.

---

*Working doc — revisit pricing and CAC targets with real beta data. Regulatory review (FRA) gates all public claims, the track-record page format, and the IB disclosure wording.*
