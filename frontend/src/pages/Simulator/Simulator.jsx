import React, { useState } from 'react'
import { AVAILABLE_ASSETS } from '../../utils/mockHistoricalData'
import { runSimulation } from '../../utils/simulatorEngine'
import './Simulator.css'

const Simulator = () => {
    // Config State
    const [initialCapital, setInitialCapital] = useState(10000)
    const [startDate, setStartDate] = useState('2023-01-01')
    const [endDate, setEndDate] = useState(new Date().toISOString().split('T')[0])
    const [allocations, setAllocations] = useState([
        { symbol: 'AAPL', weight: 60 },
        { symbol: 'BTC', weight: 40 }
    ])

    // Results State
    const [results, setResults] = useState(null)
    const [isSimulating, setIsSimulating] = useState(false)

    const handleAddAsset = () => {
        const available = AVAILABLE_ASSETS.find(a => !allocations.find(existing => existing.symbol === a.symbol))
        if (available && allocations.length < 5) {
            setAllocations([...allocations, { symbol: available.symbol, weight: 0 }])
        }
    }

    const handleRemoveAsset = (symbol) => {
        setAllocations(allocations.filter(a => a.symbol !== symbol))
    }

    const handleWeightChange = (symbol, weight) => {
        setAllocations(allocations.map(a =>
            a.symbol === symbol ? { ...a, weight: parseInt(weight) || 0 } : a
        ))
    }

    const handleSymbolChange = (oldSymbol, newSymbol) => {
        setAllocations(allocations.map(a =>
            a.symbol === oldSymbol ? { ...a, symbol: newSymbol } : a
        ))
    }

    const handleRunSimulation = () => {
        const totalWeight = allocations.reduce((sum, a) => sum + a.weight, 0)
        if (totalWeight !== 100) {
            alert(`Total weight must be 100%. Current: ${totalWeight}%`)
            return
        }

        setIsSimulating(true)
        setTimeout(() => {
            const simulationResults = runSimulation({
                initialCapital,
                allocations,
                startDate,
                endDate
            })
            setResults(simulationResults)
            setIsSimulating(false)
        }, 800)
    }

    const totalWeight = allocations.reduce((sum, a) => sum + a.weight, 0)

    return (
        <div className="simulator-page">
            <div className="simulator-body">
                <div className="simulator-glass-bg"></div>

                <div className="simulator-content">
                    <div className="simulator-hero">
                        <h1 className="gradient-text">Learning Environment</h1>
                        <p>Master investment strategies using risk-free historical simulations.</p>
                    </div>

                    <div className="simulator-grid">
                        {/* Configuration */}
                        <div className="simulator-card config-panel">
                            <div className="panel-header">
                                <h3>Setup Simulation</h3>
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

                                <div className="form-row">
                                    <div className="form-group">
                                        <label>Start date</label>
                                        <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
                                    </div>
                                    <div className="form-group">
                                        <label>End date</label>
                                        <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
                                    </div>
                                </div>

                                <div className="allocation-list">
                                    <div className="allocation-header">
                                        <label>Asset Mix</label>
                                        <button className="text-btn" onClick={handleAddAsset} disabled={allocations.length >= 5}>+ Add</button>
                                    </div>

                                    {allocations.map((alloc, idx) => (
                                        <div key={idx} className="allocation-row">
                                            <select
                                                value={alloc.symbol}
                                                onChange={(e) => handleSymbolChange(alloc.symbol, e.target.value)}
                                            >
                                                {AVAILABLE_ASSETS.map(asset => (
                                                    <option key={asset.symbol} value={asset.symbol}>{asset.symbol}</option>
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
                                            <button className="delete-btn" onClick={() => handleRemoveAsset(alloc.symbol)} disabled={allocations.length <= 1}>×</button>
                                        </div>
                                    ))}

                                    <div className={`weight-total ${totalWeight === 100 ? 'valid' : 'invalid'}`}>
                                        Total: {totalWeight}%
                                    </div>
                                </div>

                                <button
                                    className="primary-btn run-btn"
                                    onClick={handleRunSimulation}
                                    disabled={isSimulating || totalWeight !== 100}
                                >
                                    {isSimulating ? 'Processing...' : 'Run Simulation'}
                                </button>
                            </div>
                        </div>

                        {/* Results */}
                        <div className="simulator-card results-panel">
                            {results ? (
                                <div className="results-view">
                                    <div className="results-header">
                                        <h3>Simulation Stats</h3>
                                        <div className="stat-badge positive">+{results.stats.totalReturn}%</div>
                                    </div>

                                    <div className="stats-row">
                                        <div className="stat-card">
                                            <span className="label">Final Value</span>
                                            <span className="value">${results.stats.finalValue.toLocaleString()}</span>
                                        </div>
                                        <div className="stat-card">
                                            <span className="label">Volatility</span>
                                            <span className="value">{results.stats.volatility}%</span>
                                        </div>
                                        <div className="stat-card">
                                            <span className="label">Sharpe Ratio</span>
                                            <span className="value">{results.stats.sharpeRatio}</span>
                                        </div>
                                    </div>

                                    <div className="chart-wrapper">
                                        {/* Simplified visual representation of the data points */}
                                        <div className="mock-chart">
                                            <svg viewBox="0 0 400 120" preserveAspectRatio="none">
                                                <path
                                                    d="M0,100 Q50,80 100,90 T200,50 T300,60 T400,20"
                                                    fill="none"
                                                    stroke="var(--color-primary-purple)"
                                                    strokeWidth="3"
                                                />
                                            </svg>
                                            <div className="chart-labels">
                                                <span>{startDate}</span>
                                                <span>Portfolio Performance</span>
                                                <span>{endDate}</span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="ai-commentary">
                                        <span className="ai-icon">🤖</span>
                                        <p>
                                            {results.stats.totalReturn > 0
                                                ? "Your selection outperformed the benchmark! The crypto exposure provided significant alpha during this period."
                                                : "Defensive position protected downside, but missed the broader market rally. Consider increasing equity weight."}
                                        </p>
                                    </div>
                                </div>
                            ) : (
                                <div className="empty-results">
                                    <div className="icon">📈</div>
                                    <p>Ready to simulate. Enter your portfolio details and click run.</p>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Simulator
