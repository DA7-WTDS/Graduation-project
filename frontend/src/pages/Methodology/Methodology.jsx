import React from 'react'
import { Link } from 'react-router-dom'
import { BookOpen, AlertTriangle } from 'lucide-react'
import './Methodology.css'

/**
 * The methodology page (MVP_PLAN § 5 step 2, "lead with process").
 *
 * Every figure quoted here is copied from a checked-in artifact file, named
 * inline so a reader can go and verify it: models/ranking_v1/metrics.json,
 * backtest.json, lstm_experiment.json, registry.json. Nothing is hand-typed
 * from memory, and the limitations section is not an appendix — it is the
 * reason the rest of the page is worth believing.
 *
 * These are static, versioned facts about the champion model, so they live in
 * the bundle rather than behind an endpoint. When a challenger is promoted the
 * numbers change here in the same commit as the artifacts.
 */

const METRICS = [
    {
        name: 'Information coefficient',
        value: '0.077',
        detail: 'mean daily Spearman correlation, t ≈ 9.2',
        reference: '0.064 for a naive Momentum_21 baseline',
    },
    {
        name: 'Directional hit rate',
        value: '51.5%',
        detail: 'predicted vs realized side of the universe median',
        reference: '49.7% base rate in the test window',
    },
    {
        name: 'Calibration error (ECE)',
        value: '3.8pp',
        detail: 'gap between stated and realized probability',
        reference: '11.5pp before isotonic calibration',
    },
    {
        name: 'Decile spread',
        value: '2.26%',
        detail: 'top-decile minus bottom-decile realized relative return',
        reference: '5.29% for the momentum baseline',
    },
]

const LIMITATIONS = [
    {
        title: 'The universe is survivorship-biased',
        body: `Historical rows are scored against today's large-cap constituents, so companies that
               fell out of the index are missing. This inflates the level of every series — the
               strategy and both benchmarks alike — which is why the gap between them is the honest
               signal and the absolute return is not. Fixed at the licensed-data migration, which
               brings point-in-time constituents.`,
    },
    {
        title: 'A naive baseline beats us on decile spread',
        body: `Momentum_21 used directly as a score produces a wider top-minus-bottom spread than the
               model does, even though the model wins on information coefficient and hit rate. We
               publish both rather than the flattering one.`,
    },
    {
        title: 'The backtest trails an equal-weight basket on Sharpe',
        body: `Over the out-of-sample window the strategy returned more than the S&P 500 after 25bps
               per-side costs, but an equal-weight basket of the same universe achieved a better
               risk-adjusted return. Return alone is the wrong way to read that chart.`,
    },
    {
        title: 'Hit rate is below our internal target',
        body: `51.5% against a 50% base rate is a real but small edge, and short of the 53% we set as
               the internal bar. It is reported because it is what the model does.`,
    },
    {
        title: 'Market data is not yet commercially licensed',
        body: `US prices and news come from yfinance and Finnhub, which is an interim development
               arrangement. The system is a private pilot until licensed feeds are in place, and the
               provider abstraction exists so that swap is a configuration change.`,
    },
    {
        title: 'Egypt (EGX) is scaffolded, not live',
        body: `Calendars, universe rules and per-market model directories exist and are exercised by
               the US instance, but the EGX data adapter is deliberately unimplemented pending a
               licensed end-of-day feed. Nothing on this site reflects EGX performance.`,
    },
]

const Methodology = () => (
    <div className="mt-page">
        <header className="mt-hero">
            <span className="mt-eyebrow">
                <BookOpen size={14} aria-hidden="true" /> Methodology
            </span>
            <h1>How the numbers are produced</h1>
            <p>
                Every figure on this page comes from a versioned artifact file in the repository,
                named beside it. Nothing here is estimated, rounded up, or reconstructed from memory.
            </p>
            <Link className="mt-back" to="/track-record">
                ← Back to the track record
            </Link>
        </header>

        <section className="mt-section">
            <h2>What the model predicts</h2>
            <p>
                It ranks stocks against each other, and it does not forecast prices. The target is
                each stock's 21-trading-day return <em>minus the median return of the universe on
                that same date</em>. Subtracting the median removes the market move that hits every
                name at once — the part no model predicts — and leaves the only comparison a
                portfolio actually needs: which of these names is likely to do better than the rest.
            </p>
            <p>
                A consequence worth stating plainly: because the label is defined against the median,
                roughly half of all names beat it <em>by construction</em>. A hit rate near 50% is the
                null result, not a good one. The edge is the distance above 50%.
            </p>
            <p>
                An earlier version predicted absolute 30-day returns. It was abandoned: at that
                horizon returns are mostly market noise, and the fit was not distinguishable from it.
            </p>
        </section>

        <section className="mt-section">
            <h2>Measured skill</h2>
            <p className="mt-source">
                Held-out slice 2024-12-31 → 2026-06-09, 35,515 rows, never seen in training.
                Source: <code>models/ranking_v1/metrics.json</code>
            </p>
            <div className="mt-metrics">
                {METRICS.map((m) => (
                    <div className="mt-metric" key={m.name}>
                        <span className="mt-metric-name">{m.name}</span>
                        <span className="mt-metric-value">{m.value}</span>
                        <span className="mt-metric-detail">{m.detail}</span>
                        <span className="mt-metric-ref">vs {m.reference}</span>
                    </div>
                ))}
            </div>
        </section>

        <section className="mt-section">
            <h2>How we avoid fooling ourselves</h2>
            <ul className="mt-list">
                <li>
                    <strong>Chronological splits with a purge gap.</strong> Train, validation and test
                    are split by date, 70/15/15, with a 45-day gap between segments so a forward
                    return window can never straddle a boundary and leak.
                </li>
                <li>
                    <strong>Every feature is backward-looking.</strong> Moving averages are shifted a
                    day before being compared to the close, so no feature can see its own bar.
                </li>
                <li>
                    <strong>Calibration is fitted on validation only.</strong> The isotonic map that
                    turns a raw score into a probability never sees the test set.
                </li>
                <li>
                    <strong>A naive baseline is scored every run.</strong> Momentum_21 used directly
                    as the signal is reported alongside the model, so "better than nothing" has to be
                    demonstrated rather than assumed.
                </li>
                <li>
                    <strong>Costs are charged.</strong> The backtest and the live model portfolios
                    both deduct 25 basis points per side on traded value, using the same arithmetic.
                </li>
            </ul>
        </section>

        <section className="mt-section">
            <h2>What runs every night</h2>
            <ul className="mt-list">
                <li>
                    <strong>Data-quality gates.</strong> Coverage, price staleness against the market
                    calendar, prediction-magnitude sanity and feature completeness are checked before
                    a run is publishable. A failing run is still stored, flagged{' '}
                    <code>quarantined</code>, and is never shown to anyone or fed to the optimizer.
                </li>
                <li>
                    <strong>Outcome scoring.</strong> Predictions from published runs are marked to
                    market once their 30-day horizon elapses. Quarantined runs are excluded, so the
                    track record reflects what users were actually served.
                </li>
                <li>
                    <strong>A drift alarm.</strong> If the rolling 90-day hit rate crosses below 45%,
                    an alert is raised the night it happens.
                </li>
                <li>
                    <strong>Audit snapshots.</strong> Every prediction stores the exact feature vector
                    it was computed from plus a content hash of the model artifacts, so any past
                    prediction can be replayed and checked against what was served.
                </li>
                <li>
                    <strong>Monthly champion/challenger retraining.</strong> A new model ships only if
                    it matches or beats the incumbent on the same fresh out-of-sample slice. Every
                    decision, promoted or not, is appended to <code>models/registry.json</code>.
                </li>
            </ul>
        </section>

        <section className="mt-section">
            <h2>Model portfolios</h2>
            <p>
                Each strategy template is run as a fixed-notional paper portfolio from the day it
                exists, revalued nightly and rebalanced on its own cadence. These are hypothetical
                results with costs simulated — not the returns of any real client account, because
                there are none. They exist so that a track record is accumulating honestly rather than
                being assembled after the fact.
            </p>
        </section>

        <section className="mt-section mt-limits">
            <h2>
                <AlertTriangle size={17} aria-hidden="true" /> Known limitations
            </h2>
            <p className="mt-source">
                These are the reasons to discount what is above. They are listed here rather than
                buried because a performance page without them is not evidence of anything.
            </p>
            {LIMITATIONS.map((l) => (
                <div className="mt-limit" key={l.title}>
                    <h3>{l.title}</h3>
                    <p>{l.body}</p>
                </div>
            ))}
        </section>

        <p className="mt-footer">
            Informational only. Not financial advice, and not a solicitation to buy or sell any
            security. Past performance does not guarantee future results.
        </p>
    </div>
)

export default Methodology
