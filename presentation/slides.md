---
theme: default
title: 'QuantWise — Final Thesis Defence'
titleTemplate: '%s'
colorSchema: light
aspectRatio: 16/9
canvasWidth: 980
fonts:
  sans: Inter
  serif: Spectral
  mono: IBM Plex Mono
  # avoid pulling local fonts so the build is reproducible
  local: ''
mdc: true
drawings:
  persist: false
transition: slide-left
layout: cover
---

<div class="cover">

  <header class="cover-logos">
    <div class="logos-left">
      <img src="/msa-university.png" alt="MSA University" />
      <img class="seal" src="/msa-seal.png" alt="Faculty of Computer Science" />
    </div>
    <img class="logo-greenwich" src="/greenwich.png" alt="University of Greenwich — London | Egypt" />
  </header>

  <main class="cover-main">
    <p class="kicker">Graduation Project · 2025 / 2026</p>
    <h1 class="title">QuantWise</h1>
    <p class="subtitle">An AI-Powered Decision-Support Stock Advisory Platform</p>
    <p class="tagline">Personalised, Risk-Graded Investment Recommendations for Retail Investors</p>
  </main>

  <footer class="cover-footer">
    <div class="meta">
      <div class="col">
        <span class="label">Team</span>
        <p><strong>Seif ElDein Mostafa</strong><span class="id">235057 · SE</span></p>
        <p><strong>Yahia Ahmed</strong><span class="id">235161 · SE</span></p>
      </div>
      <div class="col">
        <span class="label">Supervision</span>
        <p><strong>Dr. Marwa Solayman</strong><span class="id">Supervisor</span></p>
        <p><strong>Eng. Farah Darwish</strong><span class="id">TA Supervisor</span></p>
      </div>
    </div>
    <div class="sdg">
      <span class="label">Aligned SDGs</span>
      <div class="chips">
        <span class="chip chip-9"><b>9</b><span>Industry, Innovation &amp; Infrastructure</span></span>
        <span class="chip chip-10"><b>10</b><span>Reduced Inequalities</span></span>
      </div>
    </div>
  </footer>

</div>

<style>
.slidev-layout.cover { padding: 0; }

.cover {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  background: #FCFBF9;
  color: #1B1C1E;
  font-family: 'Inter', system-ui, sans-serif;
}

/* ---- logos ---- */
.cover-logos {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 36px 56px 0;
}
.logos-left { display: flex; align-items: center; gap: 26px; }
.logos-left img { height: 60px; width: auto; }
.logos-left .seal { height: 70px; }
.logo-greenwich { height: 52px; width: auto; }

/* ---- main ---- */
.cover-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 0 64px;
}
.kicker {
  font-family: 'IBM Plex Mono', monospace;
  text-transform: uppercase;
  letter-spacing: 0.22em;
  font-size: 12px;
  color: #B5701A;
  margin: 0 0 20px;
}
.title {
  font-family: 'Spectral', serif;
  font-weight: 600;
  font-size: 92px;
  line-height: 1;
  letter-spacing: -0.01em;
  margin: 0;
  color: #17181B;
}
.subtitle {
  font-family: 'Inter', sans-serif;
  font-weight: 600;
  font-size: 23px;
  color: #2A2D34;
  margin: 20px 0 0;
}
.tagline {
  font-family: 'Spectral', serif;
  font-style: italic;
  font-size: 16px;
  color: #6B6B6B;
  margin: 12px 0 0;
}

/* ---- footer band ---- */
.cover-footer {
  background: #ECEAE6;
  padding: 20px 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 32px;
}
.label {
  display: block;
  font-family: 'IBM Plex Mono', monospace;
  text-transform: uppercase;
  letter-spacing: 0.18em;
  font-size: 10px;
  color: #8A857C;
  margin-bottom: 9px;
}
.meta { display: flex; gap: 56px; }
.col p {
  margin: 0 0 8px;
  font-family: 'IBM Plex Mono', monospace;
  font-size: 12.5px;
  color: #2B2B2B;
  line-height: 1.2;
}
.col p:last-child { margin-bottom: 0; }
.col p strong { font-weight: 600; }
.col .id { display: block; font-size: 10.5px; color: #8A857C; }

/* ---- SDG chips ---- */
.sdg { text-align: left; }
.chips { display: flex; gap: 10px; }
.chip {
  display: flex;
  align-items: center;
  gap: 9px;
  border-radius: 8px;
  padding: 8px 13px;
  max-width: 188px;
  color: #fff;
}
.chip b {
  font-family: 'Inter', sans-serif;
  font-weight: 800;
  font-size: 24px;
  line-height: 1;
}
.chip span {
  font-family: 'Inter', sans-serif;
  font-weight: 700;
  font-size: 9px;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  line-height: 1.15;
}
.chip-9  { background: #FD6925; }
.chip-10 { background: #DD1367; }
</style>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Introduction · 1 / 3</p>
  <h1 class="qw-title">Background</h1>

  <p class="qw-lead">Commission-free apps — Robinhood, eToro, and Egypt's Thndr — have put the stock market in everyone's pocket. The knowledge to use it well did not arrive with them.</p>

  <div class="qw-stats">
    <div class="qw-stat">
      <div class="num">~23%</div>
      <div class="lbl">of US equity trading volume is now retail — up from under 15% in 2019</div>
    </div>
    <div class="qw-stat">
      <div class="num">&gt;30%</div>
      <div class="lbl">growth in US retail brokerage accounts between 2019 and 2022</div>
    </div>
  </div>

  <div class="qw-points">
    <div class="qw-point"><span class="n">01</span><span class="t"><b>The domain.</b> Decision-support investing — turning prices, forecasts, news sentiment, and risk profiling into a clear "what should I do?" — <em>not</em> automated trading.</span></div>
    <div class="qw-point"><span class="n">02</span><span class="t"><b>The gap.</b> That guidance traditionally needs financial expertise or costly advisory services most first-time investors cannot reach.</span></div>
    <div class="qw-point"><span class="n">03</span><span class="t"><b>The result.</b> New investors face information overload and fall back on generic robo-advice or guesswork — leading to avoidable losses.</span></div>
  </div>

  <p class="qw-cite">Sources: FINRA — growth in U.S. retail brokerage participation (2019–2022); Bloomberg Intelligence — retail share of U.S. equity trading volume (2023).</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Introduction · 2 / 3</p>
  <h1 class="qw-title">Motivation</h1>

  <blockquote class="qw-quote"><span class="mark">"</span>As a Thndr user, I watched friends download the app — then freeze. They had the account, but no idea what to buy or how to start.<span class="mark">"</span></blockquote>

  <p class="qw-lead" style="margin-top:20px">That gap is the reason QuantWise exists: to guide everyday retail investors who know nothing about the market toward safe, informed entry.</p>

  <div class="qw-compare">
    <div class="box bad">
      <div class="bx-label">Leaving it in the bank</div>
      <div class="bx-head">Returns below inflation</div>
      <div class="bx-sub">Savings interest often fails to beat the yearly inflation rate — real money quietly loses value over time.</div>
    </div>
    <div class="box good">
      <div class="bx-label">Low-risk equity investing</div>
      <div class="bx-head">Safer than it looks — higher real returns</div>
      <div class="bx-sub">Diversified, risk-graded stock exposure has historically outpaced bank savings, with risk kept deliberately low.</div>
    </div>
  </div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Introduction · 3 / 3</p>
  <h1 class="qw-title">Challenges</h1>

  <p class="qw-lead">Four limitations stood out across the research — and became the problems QuantWise set out to tackle.</p>

  <div class="qw-cards">
    <div class="qw-card">
      <h3>Quant forecasts alone</h3>
      <p>Deep-learning and tree models predict price direction well, but carry no sentiment, no risk grading, and no personalization.</p>
      <p class="tackle">→ Pair forecasts with sentiment + a risk engine</p>
    </div>
    <div class="qw-card">
      <h3>Sentiment-only models</h3>
      <p>Improve signal reliability, yet don't produce equity-level, explainable, per-user recommendations.</p>
      <p class="tackle">→ Fuse sentiment into one graded signal</p>
    </div>
    <div class="qw-card">
      <h3>Ungrounded LLMs</h3>
      <p>Generative models can read markets from text, but hallucinate — unsafe for direct financial advice.</p>
      <p class="tackle">→ Constrain the LLM to pre-validated signals</p>
    </div>
    <div class="qw-card">
      <h3>Generic robo-advisors</h3>
      <p>Personalize only at the asset-class level: no per-stock calls, no rationale, static between rebalances.</p>
      <p class="tackle">→ Per-stock, risk-personalized, explained picks</p>
    </div>
  </div>

  <p class="qw-cite">Research basis: Fischer &amp; Krauss (2018); Araci — FinBERT (2019); Lopez-Lira &amp; Tang (2023); Betterment / Wealthfront robo-advisory.</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Problem Statement</p>
  <h1 class="qw-title">The questions we set out to answer</h1>

  <p class="qw-lead">Non-expert retail investors lack guidance that is at once personalised, risk-aware, grounded, and interpretable. We frame that gap as four questions — each with the improvement QuantWise targets.</p>

  <div class="qw-rq">
    <div class="qw-rq-item">
      <div class="qn">Q1</div>
      <div class="q">How can market-wide ML forecasts become per-user, risk-graded picks a beginner can act on at a glance?</div>
      <div class="qw-tag down">↓ Decision time</div>
    </div>
    <div class="qw-rq-item">
      <div class="qn">Q2</div>
      <div class="q">Can a generative LLM personalise advice without inventing prices, tickers, or numbers?</div>
      <div class="qw-tag up">↑ Reliability</div>
    </div>
    <div class="qw-rq-item">
      <div class="qn">Q3</div>
      <div class="q">Can quantitative predictions and market sentiment be fused into one trustworthy signal?</div>
      <div class="qw-tag up">↑ Accuracy</div>
    </div>
    <div class="qw-rq-item">
      <div class="qn">Q4</div>
      <div class="q">Can personalised AI advice be served instantly and cost-effectively, every trading day?</div>
      <div class="qw-tag down">↓ Latency &amp; cost</div>
    </div>
  </div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Objective</p>
  <h1 class="qw-title">What QuantWise sets out to achieve</h1>

  <p class="qw-vision">Make professional-grade, <b>risk-aware investing</b> genuinely accessible — so anyone can start with confidence, regardless of their financial background.</p>

  <div class="qw-aims">
    <div class="qw-aim">
      <span class="mk">◆</span>
      <div>
        <h4>Personalised plans</h4>
        <p>A BUY / SELL / HOLD plan with allocations and plain-language reasons, tailored to each user's risk profile.</p>
      </div>
    </div>
    <div class="qw-aim">
      <span class="mk">◆</span>
      <div>
        <h4>Trustworthy AI</h4>
        <p>Advice users can rely on — the model only synthesises validated signals; it never forecasts or invents numbers.</p>
      </div>
    </div>
    <div class="qw-aim">
      <span class="mk">◆</span>
      <div>
        <h4>Clarity over noise</h4>
        <p>Turn a daily flood of prices, news, and signals into one confident decision in seconds.</p>
      </div>
    </div>
    <div class="qw-aim">
      <span class="mk">◆</span>
      <div>
        <h4>Investing for everyone</h4>
        <p>Help first-time investors beat inflation safely and close the gap with institutional players (SDG 9 &amp; 10).</p>
      </div>
    </div>
  </div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Related Work · 1 / 3 — Prediction</p>
  <h1 class="qw-title">LSTM for financial market prediction</h1>
  <p class="qw-rw-byline">Fischer &amp; Krauss · European Journal of Operational Research · 2018</p>

  <div class="qw-rw-grid">
    <div class="qw-rw-cell">
      <span class="lab">Model</span>
      <p>A Long Short-Term Memory (LSTM) recurrent neural network — a deep sequence model with gated memory cells for time-series.</p>
    </div>
    <div class="qw-rw-cell">
      <span class="lab">Architecture</span>
      <p>Trained on the full S&amp;P 500 constituent history; sequences of past returns feed the LSTM to predict which stocks beat the cross-sectional median next day.</p>
    </div>
    <div class="qw-rw-cell">
      <span class="lab">Results</span>
      <p><span class="metric">+0.46% / day</span>Daily returns of ~0.46% before costs (Sharpe ≈ 5.8), outperforming random forests, deep nets, and logistic regression.</p>
    </div>
    <div class="qw-rw-cell limit">
      <span class="lab">Limitation</span>
      <p>Uses price data only — no news sentiment — and the excess returns decay after 2010 as markets grow more efficient.</p>
      <p class="seg">→ QuantWise fuses sentiment + risk grading onto the forecast.</p>
    </div>
  </div>

  <p class="qw-cite">Fischer, T. &amp; Krauss, C. (2018). Deep learning with long short-term memory networks for financial market predictions. European Journal of Operational Research, 270(2), 654–669.</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Related Work · 2 / 3 — Sentiment</p>
  <h1 class="qw-title">FinBERT: domain-tuned financial sentiment</h1>
  <p class="qw-rw-byline">Araci · arXiv:1908.10063 · 2019</p>

  <div class="qw-rw-grid">
    <div class="qw-rw-cell">
      <span class="lab">Model</span>
      <p>BERT — a transformer language model — further pre-trained on a large financial-news corpus, then fine-tuned for sentiment.</p>
    </div>
    <div class="qw-rw-cell">
      <span class="lab">Architecture</span>
      <p>Stacked self-attention encoders produce context-aware embeddings; a classification head labels financial text as positive / negative / neutral.</p>
    </div>
    <div class="qw-rw-cell">
      <span class="lab">Results</span>
      <p><span class="metric">0.86 accuracy</span>State of the art on the Financial PhraseBank, beating prior lexicon and ML methods even with little labelled data.</p>
    </div>
    <div class="qw-rw-cell limit">
      <span class="lab">Limitation</span>
      <p>Classifies sentiment only — it says nothing about price direction or magnitude, so a positive headline can contradict a falling forecast.</p>
      <p class="seg">→ QuantWise cross-validates FinBERT against the quant signal.</p>
    </div>
  </div>

  <p class="qw-cite">Araci, D. T. (2019). FinBERT: Financial Sentiment Analysis with Pre-trained Language Models. arXiv:1908.10063. QuantWise uses the ProsusAI/finbert implementation.</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Related Work · 3 / 3 — Personalization</p>
  <h1 class="qw-title">LLMs forecasting from financial news</h1>
  <p class="qw-rw-byline">Lopez-Lira &amp; Tang · arXiv:2304.07619 · 2023</p>

  <div class="qw-rw-grid">
    <div class="qw-rw-cell">
      <span class="lab">Model</span>
      <p>A large language model (ChatGPT / GPT-class) prompted to read news headlines and rate their sentiment for a stock.</p>
    </div>
    <div class="qw-rw-cell">
      <span class="lab">Architecture</span>
      <p>Zero-shot prompting: each headline becomes an LLM sentiment score, aggregated into a daily signal used to rank stocks long/short.</p>
    </div>
    <div class="qw-rw-cell">
      <span class="lab">Results</span>
      <p><span class="metric">Significant α</span>LLM news sentiment showed statistically significant predictive power for next-day returns; the long–short strategy was profitable.</p>
    </div>
    <div class="qw-rw-cell limit">
      <span class="lab">Limitation</span>
      <p>The model reasons about markets ungrounded — it can hallucinate, carries no user risk profile, and isn't anchored to validated signals.</p>
      <p class="seg">→ QuantWise constrains the LLM to synthesise pre-graded signals only.</p>
    </div>
  </div>

  <p class="qw-cite">Lopez-Lira, A. &amp; Tang, Y. (2023). Can ChatGPT Forecast Stock Price Movements? Return Predictability and Large Language Models. arXiv:2304.07619.</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Dataset · 1 / 2 — Published Paper</p>
  <h1 class="qw-title">14 US equities · 2010 – 2017</h1>

  <div class="qw-ds-spec">
    <div><span class="k">Task</span><span class="v">Regression</span></div>
    <div><span class="k">Output</span><span class="v">Continuous return → up / down</span></div>
    <div><span class="k">Modality</span><span class="v">Numerical time-series</span></div>
    <div><span class="k">Granularity</span><span class="v">Daily OHLCV</span></div>
  </div>

  <div class="qw-ds-cols">
    <div>
      <span class="qw-lab">Features</span>
      <div class="qw-ds-feat">
        <p>One trading day per stock is a record. Raw <b>Open-High-Low-Close-Volume</b> prices feed <b>14 hand-crafted technical indicators</b> (RSI, MACD, SMA/EMA ratios, momentum, volatility).</p>
        <p>The LSTM reads a <b>60-day window</b>; its embedding is stacked with the indicators into a <b>78-feature</b> vector for the XGBoost head.</p>
        <p class="qw-ds-note">Target — forward return at 30 / 90 / 252 / 365-day horizons (continuous; direction derived as a binary up/down label). Not a classification dataset.</p>
      </div>
    </div>
    <div>
      <span class="qw-lab">Sample — AAPL, daily OHLCV · 2017</span>
      <table class="qw-ds-table">
        <thead><tr><th>Date</th><th>Open</th><th>High</th><th>Low</th><th>Close</th><th>Vol</th></tr></thead>
        <tbody>
          <tr><td>2017-01-03</td><td>115.80</td><td>116.33</td><td>114.76</td><td>116.15</td><td>28.8M</td></tr>
          <tr><td>2017-01-04</td><td>115.85</td><td>116.51</td><td>115.75</td><td>116.02</td><td>21.1M</td></tr>
          <tr><td>2017-01-05</td><td>115.92</td><td>116.86</td><td>115.81</td><td>116.61</td><td>22.2M</td></tr>
          <tr><td>2017-01-06</td><td>116.78</td><td>118.16</td><td>116.47</td><td>117.91</td><td>31.8M</td></tr>
        </tbody>
      </table>
      <span class="qw-lab" style="margin-top:12px">Target distribution — forward return (≈ normal)</span>
      <div class="qw-hist">
        <div class="bar" style="height:6%"></div><div class="bar" style="height:14%"></div><div class="bar" style="height:26%"></div><div class="bar" style="height:44%"></div><div class="bar" style="height:68%"></div><div class="bar" style="height:92%"></div><div class="bar" style="height:78%"></div><div class="bar" style="height:52%"></div><div class="bar" style="height:30%"></div><div class="bar" style="height:16%"></div><div class="bar" style="height:8%"></div>
      </div>
      <div class="qw-hist-axis"><span>− return</span><span>0</span><span>+ return</span></div>
    </div>
  </div>

  <p class="qw-cite">Data: Marjanovic, B. (2017). Price-Volume Data for All US Stocks &amp; ETFs (Kaggle), 2010–2017 — 14 US large-caps, 6 sectors.</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Dataset · 2 / 2 — Deployed Hybrid Model</p>
  <h1 class="qw-title">~100 US large-caps · 2015 – 2025</h1>

  <div class="qw-ds-spec">
    <div><span class="k">Task</span><span class="v">Regression</span></div>
    <div><span class="k">Output</span><span class="v">30-day return → up / down</span></div>
    <div><span class="k">Modality</span><span class="v">Numerical time-series</span></div>
    <div><span class="k">Granularity</span><span class="v">Daily OHLCV</span></div>
  </div>

  <div class="qw-ds-cols">
    <div>
      <span class="qw-lab">Features &amp; collection</span>
      <div class="qw-ds-feat">
        <p><b>Manually assembled</b> via the <b>yfinance</b> API: daily OHLCV for ~100 diverse US large-caps (2015–2025), deliberately including decliners (BA, INTC, PYPL) to avoid "bull-market survivor" bias.</p>
        <p>Each ticker is downloaded, features engineered in-code (5 sequential inputs + 14 indicators, 60-day look-back), then split <b>chronologically 70 / 15 / 15</b> with train-only target normalisation.</p>
        <p class="qw-ds-note">Target — forward 30-day cumulative return, Z-normalised (mean 0.019, std 0.105). Continuous regression; direction derived as up/down.</p>
      </div>
    </div>
    <div>
      <span class="qw-lab">Sample — AAPL, daily OHLCV · 2024 (split-adjusted)</span>
      <table class="qw-ds-table">
        <thead><tr><th>Date</th><th>Open</th><th>High</th><th>Low</th><th>Close</th><th>Vol</th></tr></thead>
        <tbody>
          <tr><td>2024-06-03</td><td>191.24</td><td>193.32</td><td>190.87</td><td>192.36</td><td>50.1M</td></tr>
          <tr><td>2024-06-04</td><td>192.97</td><td>193.64</td><td>191.37</td><td>192.68</td><td>47.5M</td></tr>
          <tr><td>2024-06-05</td><td>193.72</td><td>195.21</td><td>193.20</td><td>194.19</td><td>54.2M</td></tr>
          <tr><td>2024-06-06</td><td>194.01</td><td>194.81</td><td>192.50</td><td>192.81</td><td>41.2M</td></tr>
        </tbody>
      </table>
      <span class="qw-lab" style="margin-top:12px">Target distribution — 30-day return, Z-normalised</span>
      <div class="qw-hist">
        <div class="bar" style="height:7%"></div><div class="bar" style="height:15%"></div><div class="bar" style="height:28%"></div><div class="bar" style="height:47%"></div><div class="bar" style="height:72%"></div><div class="bar" style="height:95%"></div><div class="bar" style="height:80%"></div><div class="bar" style="height:55%"></div><div class="bar" style="height:32%"></div><div class="bar" style="height:17%"></div><div class="bar" style="height:8%"></div>
      </div>
      <div class="qw-hist-axis"><span>−2σ</span><span>0</span><span>+2σ</span></div>
    </div>
  </div>

  <p class="qw-cite">Data: Yahoo Finance (yfinance API) — collected 2015–2025, ~100 US large-cap tickers; analyst &amp; news data via Finnhub.</p>
</div>

---
layout: default
class: diagram
---

<div class="qw-dia"><p class="qw-kicker">System Architecture</p><h1 class="qw-title">Container view — modular monolith + AI pipeline</h1></div>

```mermaid {theme: 'neutral', scale: 0.4}
flowchart TB
  subgraph Client["Client Layer"]
    Frontend["React Frontend<br/>React · TS · Vite"]
  end
  subgraph API[".NET 10 API Monolith"]
    Gateway["ASP.NET Minimal API Gateway"]
    subgraph Modules["Domain Modules"]
      UsersMod["Users"]
      PortfolioMod["Portfolio"]
      RecsMod["Recommendations"]
      NotifMod["Notifications"]
    end
  end
  subgraph MQ["Message Broker"]
    RabbitMQ["RabbitMQ · MassTransit"]
  end
  subgraph Data["Storage Layer"]
    PostgreSQL[("PostgreSQL 18")]
    Redis[("Redis Cache")]
  end
  subgraph AI["AI Pipeline"]
    FastAPI["FastAPI Service<br/>PyTorch · XGBoost · FinBERT"]
  end
  subgraph Ext["External APIs"]
    StockAPI["yFinance · Finnhub"]
    GeminiAPI["Google Gemini"]
  end
  Frontend <-->|REST · JWT| Gateway
  Gateway --> UsersMod
  Gateway --> PortfolioMod
  Gateway --> RecsMod
  Gateway --> NotifMod
  UsersMod --> PostgreSQL
  PortfolioMod --> PostgreSQL
  RecsMod --> PostgreSQL
  NotifMod --> PostgreSQL
  RecsMod <-->|HybridCache 24h| Redis
  UsersMod -.->|Outbox| RabbitMQ
  RecsMod -.->|Outbox| RabbitMQ
  RabbitMQ -.->|Inbox| NotifMod
  RecsMod -->|Risk Profile API| PortfolioMod
  FastAPI -->|Prices · sentiment| StockAPI
  FastAPI -->|Ingest daily run| Gateway
  RecsMod <-->|Personalized picks| GeminiAPI
```

---
layout: default
class: diagram
---

<div class="qw-dia"><p class="qw-kicker">Design</p><h1 class="qw-title">Use-Case Diagram</h1></div>

```mermaid {theme: 'neutral', scale: 0.38}
flowchart LR
  User["Retail User"]
  Pipeline["FastAPI Pipeline"]
  Gemini["Google Gemini API"]
  subgraph QuantWise["QuantWise Platform"]
    direction TB
    subgraph U["Authentication & Users"]
      UC_Register(["Register Account"])
      UC_Login(["Log In"])
      UC_Profile(["View Profile"])
    end
    subgraph P["Portfolio Management"]
      UC_CreatePortfolio(["Create Portfolio"])
      UC_ViewPortfolio(["View Portfolio & Allocation"])
      UC_UpdatePortfolio(["Update Allocation & Risk Settings"])
    end
    subgraph R["AI Recommendations"]
      UC_ViewRecs(["View Daily Recommendations"])
      UC_PersonalizeRecs(["Personalize Recommendations (LLM)"])
      UC_ViewPredictions(["View Raw Predictions (Simulator)"])
      UC_IngestResults(["Ingest Daily ML Scoring Run"])
    end
    subgraph N["Notifications"]
      UC_ViewNotifications(["View Notifications"])
      UC_MarkRead(["Mark Notification as Read"])
      UC_MarkAllRead(["Mark All Read"])
      UC_TestNotification(["Trigger Test Notification"])
    end
  end
  User --> UC_Register
  User --> UC_Login
  User --> UC_Profile
  User --> UC_CreatePortfolio
  User --> UC_ViewPortfolio
  User --> UC_UpdatePortfolio
  User --> UC_ViewRecs
  User --> UC_ViewPredictions
  User --> UC_ViewNotifications
  User --> UC_MarkRead
  User --> UC_MarkAllRead
  User --> UC_TestNotification
  Pipeline --> UC_IngestResults
  UC_ViewRecs -.->|"«include»"| UC_PersonalizeRecs
  UC_PersonalizeRecs -.->|"«use»"| Gemini
```

---
layout: default
---

<div class="qw-fig">
  <div class="cap">Design · Sequence — Login (JWT)</div>
  <div class="imgwrap"><img src="/seq-login.png" alt="Login sequence diagram" /></div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">System Process · 1 / 4</p>
  <h1 class="qw-title">Phase 1 — Data Acquisition</h1>
  <div class="qw-io">
    <div class="qw-io-col in"><span class="qw-io-h">Input</span><div class="qw-io-list"><div class="it"><span>~100 US large-cap <b>ticker universe</b></span></div><div class="it"><span>Date range <b>2015 – 2025</b>, daily</span></div></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-proc"><span class="qw-io-h">Process</span><div class="qw-step"><div class="t"><span class="n">1</span> Resolve universe</div><span class="s">yFinance top-100 screener · hardcoded fallback</span></div><div class="qw-step"><div class="t"><span class="n">2</span> Batch download OHLCV</div><span class="s">one bulk yFinance call · rate-limited throttle</span></div><div class="qw-step"><div class="t"><span class="n">3</span> Fetch analyst &amp; news</div><span class="s">Finnhub ratings + headlines · 14-day window</span></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-col out"><span class="qw-io-h">Output</span><div class="qw-io-list"><div class="it"><span>Raw <b>daily OHLCV</b> per ticker</span></div><div class="it"><span>Analyst ratings + <b>news headlines</b></span></div></div></div>
  </div>
  <div class="qw-handoff"><span class="ar">→</span> Output becomes the input to <b>Phase 2 · Pre-processing</b></div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">System Process · 2 / 4</p>
  <h1 class="qw-title">Phase 2 — Pre-processing</h1>
  <div class="qw-io">
    <div class="qw-io-col in"><span class="qw-io-h">Input · from Phase 1</span><div class="qw-io-list"><div class="it"><span>Raw <b>daily OHLCV</b> per ticker</span></div><div class="it"><span>News headlines per ticker</span></div></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-proc"><span class="qw-io-h">Process</span><div class="qw-step"><div class="t"><span class="n">1</span> Feature engineering</div><span class="s">5 sequential features + 14 technical indicators</span></div><div class="qw-step"><div class="t"><span class="n">2</span> Scaling</div><span class="s">global MinMax · fit on train split only</span></div><div class="qw-step"><div class="t"><span class="n">3</span> Windowing</div><span class="s">60-day look-back sequences</span></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-col out"><span class="qw-io-h">Output</span><div class="qw-io-list"><div class="it"><span>Scaled <b>60-day windows</b> (LSTM-ready)</span></div><div class="it"><span>14-indicator vectors + <b>relevant headlines</b></span></div></div></div>
  </div>
  <div class="qw-handoff"><span class="ar">→</span> Output becomes the input to <b>Phase 3 · Model</b></div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">System Process · 3 / 4</p>
  <h1 class="qw-title">Phase 3 — Hybrid Model</h1>
  <div class="qw-io">
    <div class="qw-io-col in"><span class="qw-io-h">Input · from Phase 2</span><div class="qw-io-list"><div class="it"><span>Scaled <b>60-day windows</b></span></div><div class="it"><span>Indicator vectors + headlines</span></div></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-proc"><span class="qw-io-h">Process</span><div class="qw-step"><div class="t"><span class="n">1</span> LSTM encoder</div><span class="s">60-day window → 64-dim embedding · MC-Dropout ×30</span></div><div class="qw-step"><div class="t"><span class="n">2</span> XGBoost head</div><span class="s">78-dim → return z-score → denormalise</span></div><div class="qw-step"><div class="t"><span class="n">3</span> FinBERT sentiment</div><span class="s">headlines → weighted composite signal</span></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-col out"><span class="qw-io-h">Output</span><div class="qw-io-list"><div class="it"><span>Per-ticker <b>direction · change % · confidence</b></span></div><div class="it"><span>Composite <b>sentiment score</b> per ticker</span></div></div></div>
  </div>
  <div class="qw-handoff"><span class="ar">→</span> Output becomes the input to <b>Phase 4 · Decision</b></div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">System Process · 4 / 4</p>
  <h1 class="qw-title">Phase 4 — Risk Grading &amp; Decision</h1>
  <div class="qw-io">
    <div class="qw-io-col in"><span class="qw-io-h">Input · from Phase 3</span><div class="qw-io-list"><div class="it"><span>Predictions + <b>sentiment</b> per ticker</span></div><div class="it"><span>User <b>risk profile</b> + allocation</span></div></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-proc"><span class="qw-io-h">Process</span><div class="qw-step"><div class="t"><span class="n">1</span> Risk-Rules engine</div><span class="s">agreement · flags · risk level · conviction</span></div><div class="qw-step"><div class="t"><span class="n">2</span> Gemini personalisation</div><span class="s">grounded · schema-JSON · per risk profile</span></div><div class="qw-step"><div class="t"><span class="n">3</span> Cache &amp; deliver</div><span class="s">Redis 24 h · BUY / SELL / HOLD</span></div></div>
    <div class="qw-arrow">→</div>
    <div class="qw-io-col out"><span class="qw-io-h">Output</span><div class="qw-io-list"><div class="it"><span>Risk-graded signals <b>LOW / MED / HIGH</b></span></div><div class="it"><span>Personalised <b>BUY / SELL / HOLD</b> + allocation + reason</span></div></div></div>
  </div>
  <div class="qw-handoff"><span class="ar">→</span> Delivered to the <b>React dashboard</b> — the user's personalised recommendation</div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">AI Pipeline · Output &amp; Scoring</p>
  <h1 class="qw-title">What the pipeline produces — and how it's weighted</h1>
  <div class="qw-out-grid">
    <div>
      <span class="qw-lab">Sample output · 1 of 100 records · run 2026-06-17</span>
      <div class="qw-json"><div><span class="k">ticker</span>: <span class="s">"MSFT"</span></div><div><span class="k">direction</span>: <span class="s">"UP"</span>  ·  <span class="k">change_pct</span>: <span class="n">+7.66</span></div><div><span class="k">confidence</span>: <span class="n">0.72</span>  ·  <span class="k">signal</span>: <span class="s">"POSITIVE"</span></div><div><span class="k">sentiment_score</span>: <span class="n">0.54</span></div><div><span class="k">analyst_rating</span>: <span class="n">4.27</span> (Buy)  ·  <span class="k">pt_upside</span>: <span class="n">+42.6%</span></div><div><span class="k">agreement</span>: <span class="s">"CONFIRMED"</span></div><span class="hi"><span class="k">risk_level</span>: <span class="badge low">LOW</span>   <span class="k">conviction</span>: <span class="n">0.72</span></span><div><span class="k">risk_flags</span>: [<span class="s">"signal_confirmed"</span>]</div></div>
    </div>
    <div>
      <span class="qw-lab">Weights behind each phase</span>
      <div class="qw-w"><div class="qw-wblock"><span class="wh">① Prediction → confidence</span><div class="wf">√( signal_strength × stability ) × data_quality</div><div class="ws">stability from MC-Dropout ×30 · data_quality = share of inputs in range</div></div><div class="qw-wblock"><span class="wh">② Sentiment → composite score</span><div class="qw-wrow"><span class="lab">Consensus</span><span class="track"><span class="fill" style="width:40%"></span></span><span class="pct">40%</span></div><div class="qw-wrow"><span class="lab">News</span><span class="track"><span class="fill" style="width:25%"></span></span><span class="pct">25%</span></div><div class="qw-wrow"><span class="lab">Price target</span><span class="track"><span class="fill" style="width:20%"></span></span><span class="pct">20%</span></div><div class="qw-wrow"><span class="lab">Up/downgrades</span><span class="track"><span class="fill" style="width:15%"></span></span><span class="pct">15%</span></div></div><div class="qw-wblock"><span class="wh">③ Risk → conviction</span><div class="wf">0.5 · confidence + 0.3 · |sentiment| ± 0.2</div><div class="ws">+0.2 if quant &amp; sentiment agree · −0.2 if they contradict</div></div></div>
    </div>
  </div>
  <p class="qw-cite">Deterministic, user-independent scoring — the LLM only ever sees pre-graded signals.</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Implementation · LLM Layer</p>
  <h1 class="qw-title">Grounding the generative model</h1>
  <div class="qw-llm-grid">
    <div>
      <span class="qw-lab">Constrained synthesiser, not a predictor</span>
      <div class="qw-llm-rules"><div class="it"><span>Uses <b>only the day's risk-graded data</b> — never invents tickers, prices, or numbers.</span></div><div class="it"><span>Respects the grading — <b>no HIGH-risk or contradicted picks</b> for Conservative users.</span></div><div class="it"><span>Output forced to <b>schema-valid JSON</b> (native responseSchema) · <b>3× retry</b> on parse fail.</span></div><div class="it"><span>Allocations must <b>sum to 100%</b>; result <b>cached 24 h</b> per user in Redis.</span></div></div>
    </div>
    <div>
      <span class="qw-lab">Illustrative output · { summary, picks[] }</span>
      <div class="qw-json"><div><span class="k">ticker</span>: <span class="s">"MSFT"</span>  ·  <span class="k">action</span>: <span class="badge low">BUY</span></div><div><span class="k">allocation_pct</span>: <span class="n">18</span></div><div><span class="k">reason</span>:</div><div><span class="s">"Confirmed UP signal with strong analyst</span></div><div><span class="s">backing and +42% target upside."</span></div><div><span class="k">risk_note</span>: <span class="s">"LOW risk · core holding."</span></div><div><span class="k">fit</span>: <span class="s">"Anchors a Moderate portfolio."</span></div></div>
    </div>
  </div>
  <p class="qw-cite">Why it matters: the model synthesises and explains pre-validated signals — sidestepping the hallucination risk of asking an LLM to forecast from scratch.</p>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Results · Prediction Model</p>
  <h1 class="qw-title">Hybrid vs standalone baselines</h1>
  <div class="qw-res-grid">
    <div>
      <span class="qw-lab">30-day test RMSE — lower is better</span>
      <div class="qw-rmse"><div class="row"><div class="lab"><span>Standalone LSTM</span><b>≈ 0.285</b></div><div class="track"><span class="fill bad" style="width:100%"></span></div></div><div class="row"><div class="lab"><span>Hybrid · LSTM → XGBoost</span><b>0.0949</b></div><div class="track"><span class="fill good" style="width:33%"></span></div></div></div>
      <p class="qw-ds-note" style="margin-top:12px">Hybrid error is <b>≈ one-third</b> of the standalone LSTM, and <b>matches or surpasses the XGBoost-only</b> baseline across the majority of stocks.</p>
    </div>
    <div class="qw-note">
      <div class="nh">Directional accuracy</div>
      <p>Rises with horizon — <span class="big">97.6%</span> at 365 days.</p>
      <p>But that largely tracks the high <b>base rate of positive long-horizon returns</b> — the model adds little over an always-up predictor at long horizons, so the <b>30-day RMSE is the more honest signal</b>.</p>
    </div>
  </div>
  <p class="qw-cite">Mostafa, Ahmed, Darwish &amp; Solayman (2026) — A Hybrid LSTM–XGBoost Framework · 14 US equities, multi-horizon. Deployed QuantWise model = single 30-day variant.</p>
</div>

---
layout: default
class: pub
---

<div class="qw-pub">
  <p class="qw-kicker">Publication</p>

  <div class="qw-pub-logo">
    <img src="/conference-logo.png" alt="Conference" />
  </div>

  <span class="qw-pub-badge">Accepted · pending publication</span>

  <h1 class="qw-pub-title">"A Hybrid LSTM–XGBoost Framework for Multi-Horizon Stock Return Prediction Across Diversified Equity Portfolios"</h1>

  <p class="qw-pub-status">Accepted at <b class="ph">IMSA</b> &nbsp;·&nbsp; IEEE Egypt Section</p>

  <div class="qw-pub-authors">
    <span class="qw-pub-lab">Authors</span>
    <div class="names">
      <span><b>Seif ElDein Mostafa</b></span>
      <span class="dot">·</span>
      <span><b>Yahia Ahmed</b></span>
    </div>
  </div>

  <p class="qw-pub-cat">IEEE Catalog 979-8-3315-8488-7 / 26 · © 2026 IEEE</p>
</div>

<style>
.slidev-layout.pub { background: #FCFBF9; }
.qw-pub {
  position: absolute; inset: 0;
  display: flex; flex-direction: column;
  align-items: center; justify-content: center;
  text-align: center;
  padding: 0 80px;
  font-family: 'Inter', sans-serif;
}
.qw-pub .qw-kicker {
  font-family: 'IBM Plex Mono', monospace;
  text-transform: uppercase; letter-spacing: 0.22em;
  font-size: 11px; color: #B5701A; margin: 0 0 22px;
}
.qw-pub-logo { margin-bottom: 18px; }
.qw-pub-logo img { height: 66px; width: auto; }
.qw-pub-badge {
  display: inline-block;
  font-family: 'IBM Plex Mono', monospace;
  text-transform: uppercase; letter-spacing: 0.08em;
  font-size: 11px; font-weight: 600;
  color: #1F8A4C;
  background: #E7F3EC;
  border: 1px solid #BFE0CB;
  border-radius: 999px;
  padding: 6px 14px;
  margin-bottom: 18px;
}
.qw-pub-title {
  font-family: 'Spectral', serif;
  font-weight: 600; font-style: italic;
  font-size: 27px; line-height: 1.3;
  letter-spacing: -0.01em;
  color: #17181B;
  max-width: 820px;
  margin: 0 0 22px;
}
.qw-pub-status {
  font-size: 15px; color: #2C2F36; margin: 0 0 28px;
}
.qw-pub-status b { font-weight: 600; }
.qw-pub-status .ph {
  color: #B5701A;
  border-bottom: 1.5px dashed #D9B583;
  padding-bottom: 1px;
}
.qw-pub-authors {
  border-top: 1px solid #ECE8E1;
  padding-top: 20px;
}
.qw-pub-lab {
  display: block;
  font-family: 'IBM Plex Mono', monospace;
  text-transform: uppercase; letter-spacing: 0.18em;
  font-size: 10px; color: #8A857C; margin-bottom: 10px;
}
.qw-pub-authors .names {
  display: flex; align-items: center; gap: 14px;
  font-size: 17px; color: #17181B;
}
.qw-pub-authors .names .dot { color: #B5701A; font-weight: 700; }
.qw-pub-cat {
  font-family: 'IBM Plex Mono', monospace;
  font-size: 10px; color: #8A857C;
  margin: 30px 0 0;
}
</style>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Technologies Used</p>
  <h1 class="qw-title">The stack, by layer</h1>
  <div class="qw-tech">
    <div class="qw-tech-card"><h3>Frontend</h3><div class="qw-tech-chips"><span class="c"><logos-react class="ci" />React</span><span class="c"><logos-typescript-icon class="ci" />TypeScript</span><span class="c"><logos-vitejs class="ci" />Vite</span><span class="c">TanStack Query</span><span class="c">Framer Motion</span></div></div>
    <div class="qw-tech-card"><h3>Backend · .NET</h3><div class="qw-tech-chips"><span class="c"><logos-dotnet class="ci" />ASP.NET Core 10</span><span class="c">MediatR · CQRS</span><span class="c">EF Core</span><span class="c">FluentValidation</span><span class="c">JWT</span></div></div>
    <div class="qw-tech-card"><h3>Messaging &amp; Cache</h3><div class="qw-tech-chips"><span class="c"><logos-rabbitmq-icon class="ci" />RabbitMQ</span><span class="c">MassTransit</span><span class="c"><logos-redis class="ci" />Redis</span></div></div>
    <div class="qw-tech-card"><h3>ML Pipeline</h3><div class="qw-tech-chips"><span class="c"><logos-python class="ci" />Python</span><span class="c"><logos-fastapi-icon class="ci" />FastAPI</span><span class="c"><logos-pytorch class="ci" />PyTorch · LSTM</span><span class="c">XGBoost</span><span class="c">FinBERT</span></div></div>
    <div class="qw-tech-card"><h3>Data &amp; AI</h3><div class="qw-tech-chips"><span class="c"><logos-postgresql class="ci" />PostgreSQL 18</span><span class="c">yFinance</span><span class="c">Finnhub</span><span class="c"><logos-google-icon class="ci" />Google Gemini</span></div></div>
    <div class="qw-tech-card"><h3>DevOps &amp; Testing</h3><div class="qw-tech-chips"><span class="c"><logos-docker-icon class="ci" />Docker Compose</span><span class="c">xUnit</span><span class="c">Testcontainers</span><span class="c">Playwright</span><span class="c">k6</span></div></div>
  </div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Feature List</p>
  <h1 class="qw-title">What QuantWise offers</h1>
  <div class="qw-feat">
    <div class="qw-feat-item"><carbon-security class="ic" /><h3>Secure accounts</h3><p>JWT login, hashed passwords.</p></div>
    <div class="qw-feat-item"><carbon-user-profile class="ic" /><h3>Risk profiling</h3><p>Questionnaire → Conservative / Moderate / Aggressive.</p></div>
    <div class="qw-feat-item"><carbon-recommend class="ic" /><h3>Personalised recommendations</h3><p>BUY / SELL / HOLD + allocation + reasons.</p></div>
    <div class="qw-feat-item"><carbon-portfolio class="ic" /><h3>Holdings-aware advice</h3><p>HOLD or SELL on what you already own.</p></div>
    <div class="qw-feat-item"><carbon-chart-pie class="ic" /><h3>Portfolio &amp; target mix</h3><p>Allocation, amount, per-pick dollar split.</p></div>
    <div class="qw-feat-item"><carbon-notification class="ic" /><h3>Notification centre</h3><p>Welcome &amp; daily-run alerts, unread badges.</p></div>
    <div class="qw-feat-item"><carbon-chart-candlestick class="ic" /><h3>Live market hub</h3><p>Real-time quotes and symbol search.</p></div>
    <div class="qw-feat-item"><carbon-machine-learning-model class="ic" /><h3>Learning environment</h3><p>Allocate across picks, see projected outcome.</p></div>
    <div class="qw-feat-item"><carbon-user-settings class="ic" /><h3>Profile management</h3><p>View and update account &amp; risk profile.</p></div>
  </div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Evaluation · Testing</p>
  <h1 class="qw-title">How we tested the system</h1>
  <div class="qw-eval-cards">
    <div class="qw-eval-card"><div class="h">Unit</div><div class="tool">xUnit · NSubstitute</div><p>CQRS handlers and domain rules in isolation, with substituted dependencies.</p><span class="res">55 tests · all pass</span></div>
    <div class="qw-eval-card"><div class="h">API · Integration</div><div class="tool">xUnit + WebApplicationFactory</div><p>Hits the real API over real Postgres &amp; Redis (Testcontainers); Swagger / Postman for manual checks.</p><span class="res">13 tests · all pass</span></div>
    <div class="qw-eval-card"><div class="h">GUI · System</div><div class="tool">Playwright</div><p>Drives the live React app end-to-end like a real user — the role Selenium plays elsewhere.</p><span class="res">12 black-box cases · all pass</span></div>
    <div class="qw-eval-card"><div class="h">Model</div><div class="tool">RMSE · MAE · Dir. accuracy</div><p>Hybrid scored against standalone LSTM &amp; XGBoost baselines (see Results).</p><span class="res alt">reported &amp; benchmarked</span></div>
    <div class="qw-eval-card"><div class="h">Load</div><div class="tool">k6</div><p>50 virtual users hammering the read paths for two minutes.</p><span class="res">0 errors · p95 6.9 ms</span></div>
    <div class="qw-eval-card"><div class="h">Coverage</div><div class="tool">47.3% / 54.8% core</div><p>Line coverage merged across 6 projects via coverlet + ReportGenerator.</p><span class="res alt">68 automated tests</span></div>
  </div>
</div>

---
layout: default
---

<div class="qw">
  <p class="qw-kicker">Evaluation · Test Cases</p>
  <h1 class="qw-title">Black-box test cases</h1>
  <table class="qw-tc-table">
    <thead><tr><th>ID</th><th>Area</th><th>Test</th><th>Expected result</th><th>Status</th></tr></thead>
    <tbody>
      <tr><td class="id">TC-01</td><td>Auth</td><td>Register a new account</td><td>201 · account created</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-02</td><td>Auth</td><td>Login — valid / invalid</td><td>Token issued · 401 generic on bad creds</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-03</td><td>Onboarding</td><td>Complete the questionnaire</td><td>Risk profile · allocation sums to 100</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-04</td><td>Security</td><td>Read another user's data</td><td>HTTP 403 forbidden</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-05</td><td>Recommendations</td><td>Open the dashboard</td><td>Personalised BUY picks + reasons</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-06</td><td>Portfolio</td><td>View allocation in dollars</td><td>$5,000 of $10,000 shown per pick</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-07</td><td>Notifications</td><td>Open the bell after registering</td><td>One unread "Welcome" notification</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-09</td><td>Market</td><td>Open the Market page</td><td>Live quotes for 8 tickers</td><td><span class="qw-tc-pass">PASS</span></td></tr>
      <tr><td class="id">TC-11</td><td>Learning</td><td>Open the Learning view</td><td>Latest pipeline predictions shown</td><td><span class="qw-tc-pass">PASS</span></td></tr>
    </tbody>
  </table>
  <p class="qw-cite">12 / 12 black-box system cases passed · each traced to a functional requirement · run on the live stack (frontend :3000 → backend :5000).</p>
</div>

---
layout: default
class: diagram
---

<div class="qw-dia"><p class="qw-kicker">Project Management</p><h1 class="qw-title">Project Schedule (Gantt)</h1></div>

```plantuml
@startgantt fig-3-2-gantt
' QuantWise — Figure 3.2 Project Schedule (Gantt)
' Kanban flow framed across milestones M0-M4, Feb-Jun 2026.
' The offline ML-training phase overlaps the inter-semester gap and was done first.
project starts 2026-02-01

[M0 Offline ML training (LSTM/XGBoost/FinBERT, risk rules)] as [M0] lasts 56 days
[M1 Backend foundation (modular monolith, Users + Portfolio, outbox/inbox)] as [M1] lasts 28 days
[M2 Pipeline service + ingest (FastAPI /api/score, Recommendations, Quartz)] as [M2] lasts 21 days
[M3 Recommendations + frontend (Gemini, 24h cache, fan-out, Quant Terminal)] as [M3] lasts 21 days
[M4 Testing, hardening + write-up (68 tests, coverage, perf, security)] as [M4] lasts 24 days

[M1] starts at [M0]'s end
[M2] starts at [M1]'s end
[M3] starts at [M2]'s end
[M4] starts at [M3]'s end

@endgantt
```

---
layout: cover
---

<div class="cover">
  <main class="cover-main">
    <h1 class="title">Thank You</h1>
    <p class="subtitle">We welcome any questions.</p>
  </main>
</div>
