import React, { useEffect, useMemo, useState } from 'react'
import { LineChart, Sparkles } from 'lucide-react'
import { usePredictions } from '@/features/learning/usePredictions'
import { LoadingState, EmptyState, ErrorState } from '@/shared/ui'
import './Simulator.css'

const fmtUSD = (n) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n)

const pct = (n) => `${n > 0 ? '+' : ''}${n.toFixed(2)}%`

const Simulator = () => {
    const { data, isLoading, isError, refetch } = usePredictions()
    const predictions = useMemo(() => data?.predictions ?? [], [data])
    const byTicker = useMemo(() => Object.fromEntries(predictions.map((p) => [p.ticker, p])), [predictions])

    const [initialCapital, setInitialCapital] = useState(10000)
    const [allocations, setAllocations] = useState([])
    const [results, setResults] = useState(null)
    const [isSimulating, setIsSimulating] = useState(false)

    // Seed a starter allocation from the highest-conviction predictions once loaded.
    useEffect(() => {
        if (predictions.length && allocations.length === 0) {
            const top = predictions.slice(0, 2)
            if (top.length === 2) {
                setAllocations([{ symbol: top[0].ticker, weight: 60 }, { symbol: top[1].ticker, weight: 40 }])
            } else if (top.length === 1) {
                setAllocations([{ symbol: top[0].ticker, weight: 100 }])
            }
        }
    }, [predictions, allocations.length])

    const totalWeight = allocations.reduce((sum, a) => sum + a.weight, 0)

    const handleAddAsset = () => {
        const next = predictions.find((p) => !allocations.some((a) => a.symbol === p.ticker))
        if (next && allocations.length < 6) {
            setAllocations([...allocations, { symbol: next.ticker, weight: 0 }])
        }
    }
    const handleRemoveAsset = (symbol) => setAllocations(allocations.filter((a) => a.symbol !== symbol))
    const handleWeightChange = (symbol, weight) =>
        setAllocations(allocations.map((a) => (a.symbol === symbol ? { ...a, weight: parseInt(weight) || 0 } : a)))
    const handleSymbolChange = (oldSymbol, newSymbol) => {
        if (allocations.some((a) => a.symbol === newSymbol)) return // no duplicate tickers
        setAllocations(allocations.map((a) => (a.symbol === oldSymbol ? { ...a, symbol: newSymbol } : a)))
    }

    const handleRun = () => {
        if (totalWeight !== 100) return
        setIsSimulating(true)
        setTimeout(() => {
            const cap = parseFloat(initialCapital) || 0
            const breakdown = allocations.map((a) => {
                const p = byTicker[a.symbol]
                const invested = (cap * a.weight) / 100
                const changePct = p ? p.changePct : 0
                const projected = invested * (1 + changePct / 100)
                return {
                    symbol: a.symbol,
                    weight: a.weight,
                    invested,
                    changePct,
                    projected,
                    gain: projected - invested,
                    direction: p?.direction,
                    riskLevel: p?.riskLevel,
                    confidence: p?.confidence ?? 0,
                }
            })
            const projectedValue = breakdown.reduce((s, b) => s + b.projected, 0)
            const totalReturn = cap > 0 ? ((projectedValue - cap) / cap) * 100 : 0
            const weightedConfidence = breakdown.reduce((s, b) => s + (b.confidence * b.weight) / 100, 0)
            setResults({ cap, breakdown, projectedValue, totalReturn, weightedConfidence })
            setIsSimulating(false)
        }, 400)
    }

    const runDate = data?.generatedAt ? new Date(data.generatedAt).toLocaleDateString() : null
    const maxGainAbs = results ? Math.max(...results.breakdown.map((b) => Math.abs(b.gain)), 1) : 1

    return (
        <div className="simulator-page">
            <div className="simulator-body">
                <div className="simulator-glass-bg"></div>

                <div className="simulator-content">
                    <div className="simulator-hero">
                        <span className="demo-badge">
                            Model-driven{runDate ? ` · run ${runDate}` : ''}
                        </span>
                        <h1 className="gradient-text">Learning Environment</h1>
                        <p>Allocate across the model's latest predictions and see the projected outcome.</p>
                    </div>

                    {isLoading ? (
                        <LoadingState label="Loading the latest model run…" />
                    ) : isError ? (
                        <ErrorState message="Couldn't load predictions." onRetry={() => refetch()} />
                    ) : !data || predictions.length === 0 ? (
                        <EmptyState
                            title="No model run yet"
                            hint="The daily pipeline hasn't produced a run. Check back once predictions are available."
                        />
                    ) : (
                        <div className="simulator-grid">
                            {/* Configuration */}
                            <div className="simulator-card config-panel">
                                <div className="panel-header">
                                    <h3>Build Allocation</h3>
                                </div>

                                <div className="config-form">
                                    <div className="form-group">
                                        <label>Initial Capital</label>
                                        <div className="input-group">
                                            <span className="input-prefix">$</span>
                                            <input
                                                type="number"
                                                value={initialCapital}
                                                onChange={(e) => setInitialCapital(e.target.value)}
                                            />
                                        </div>
                                    </div>

                                    <div className="allocation-list">
                                        <div className="allocation-header">
                                            <label>Asset Mix (from predictions)</label>
                                            <button
                                                className="text-btn"
                                                onClick={handleAddAsset}
                                                disabled={allocations.length >= 6 || allocations.length >= predictions.length}
                                            >
                                                + Add
                                            </button>
                                        </div>

                                        {allocations.map((alloc, idx) => {
                                            const p = byTicker[alloc.symbol]
                                            return (
                                                <div key={idx} className="allocation-row">
                                                    <select
                                                        value={alloc.symbol}
                                                        onChange={(e) => handleSymbolChange(alloc.symbol, e.target.value)}
                                                    >
                                                        {predictions.map((pr) => (
                                                            <option key={pr.ticker} value={pr.ticker}>
                                                                {pr.ticker} ({pct(pr.changePct)})
                                                            </option>
                                                        ))}
                                                    </select>
                                                    <div className="weight-input">
                                                        <input
                                                            type="number"
                                                            value={alloc.weight}
                                                            onChange={(e) => handleWeightChange(alloc.symbol, e.target.value)}
                                                        />
                                                        <span>%</span>
                                                    </div>
                                                    <button
                                                        className="delete-btn"
                                                        onClick={() => handleRemoveAsset(alloc.symbol)}
                                                        disabled={allocations.length <= 1}
                                                    >
                                                        ×
                                                    </button>
                                                </div>
                                            )
                                        })}

                                        <div className={`weight-total ${totalWeight === 100 ? 'valid' : 'invalid'}`}>
                                            Total: {totalWeight}%
                                        </div>
                                    </div>

                                    <button
                                        className="primary-btn run-btn"
                                        onClick={handleRun}
                                        disabled={isSimulating || totalWeight !== 100}
                                    >
                                        {isSimulating ? 'Projecting…' : 'Run Projection'}
                                    </button>
                                </div>
                            </div>

                            {/* Results */}
                            <div className="simulator-card results-panel">
                                {results ? (
                                    <div className="results-view">
                                        <div className="results-header">
                                            <h3>Projected Outcome</h3>
                                            <div className={`stat-badge ${results.totalReturn >= 0 ? 'positive' : 'negative'}`}>
                                                {pct(results.totalReturn)}
                                            </div>
                                        </div>

                                        <div className="stats-row">
                                            <div className="stat-card">
                                                <span className="label">Projected Value</span>
                                                <span className="value">{fmtUSD(results.projectedValue)}</span>
                                            </div>
                                            <div className="stat-card">
                                                <span className="label">Projected P/L</span>
                                                <span className="value">{fmtUSD(results.projectedValue - results.cap)}</span>
                                            </div>
                                            <div className="stat-card">
                                                <span className="label">Avg Confidence</span>
                                                <span className="value">{(results.weightedConfidence * 100).toFixed(0)}%</span>
                                            </div>
                                        </div>

                                        <div className="projection-breakdown">
                                            {results.breakdown.map((b) => (
                                                <div key={b.symbol} className="projection-row">
                                                    <div className="projection-meta">
                                                        <span className="projection-ticker">{b.symbol}</span>
                                                        <span className="projection-weight">{b.weight}%</span>
                                                        <span className={`projection-change ${b.changePct >= 0 ? 'up' : 'down'}`}>
                                                            {pct(b.changePct)}
                                                        </span>
                                                        {b.riskLevel && <span className={`risk-tag risk-${b.riskLevel.toLowerCase()}`}>{b.riskLevel}</span>}
                                                    </div>
                                                    <div className="projection-bar-track">
                                                        <span
                                                            className={`projection-bar ${b.gain >= 0 ? 'up' : 'down'}`}
                                                            style={{ width: `${(Math.abs(b.gain) / maxGainAbs) * 100}%` }}
                                                        />
                                                    </div>
                                                    <span className="projection-amount">
                                                        {fmtUSD(b.invested)} → {fmtUSD(b.projected)}
                                                    </span>
                                                </div>
                                            ))}
                                        </div>

                                        <div className="ai-commentary">
                                            <span className="ai-icon"><Sparkles size={22} aria-hidden="true" /></span>
                                            <p>
                                                Projection applies each ticker's model-predicted move to your allocation. It's an
                                                estimate from the latest run — not a guarantee of future performance.
                                            </p>
                                        </div>
                                    </div>
                                ) : (
                                    <div className="empty-results">
                                        <div className="icon"><LineChart size={56} strokeWidth={1.25} aria-hidden="true" /></div>
                                        <p>Set your capital and asset mix, then run the projection.</p>
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}

export default Simulator
