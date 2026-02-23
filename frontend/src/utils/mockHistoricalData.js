/**
 * Mock historical data generator for the Simulator.
 * Provides price history for a set of assets.
 */

export const AVAILABLE_ASSETS = [
    { symbol: 'AAPL', name: 'Apple Inc.', type: 'Stock', basePrice: 150 },
    { symbol: 'MSFT', name: 'Microsoft Corp.', type: 'Stock', basePrice: 300 },
    { symbol: 'GOOGL', name: 'Alphabet Inc.', type: 'Stock', basePrice: 140 },
    { symbol: 'TSLA', name: 'Tesla Inc.', type: 'Stock', basePrice: 200 },
    { symbol: 'BTC', name: 'Bitcoin', type: 'Crypto', basePrice: 45000 },
    { symbol: 'ETH', name: 'Ethereum', type: 'Crypto', basePrice: 2500 },
    { symbol: 'SPY', name: 'S&P 500 ETF', type: 'ETF', basePrice: 400 },
    { symbol: 'GLD', name: 'Gold Shares', type: 'ETF', basePrice: 180 },
]

/**
 * Generates mock daily price history for an asset.
 */
export const getHistoricalData = (symbol, startDate, endDate) => {
    const asset = AVAILABLE_ASSETS.find(a => a.symbol === symbol) || AVAILABLE_ASSETS[0]
    const start = new Date(startDate)
    const end = new Date(endDate)
    const data = []

    let currentPrice = asset.basePrice
    const dayMs = 24 * 60 * 60 * 1000

    for (let d = start; d <= end; d = new Date(d.getTime() + dayMs)) {
        // Simple random walk with a slight upward bias (0.05%)
        const volatility = asset.type === 'Crypto' ? 0.04 : 0.015
        const change = currentPrice * (volatility * (Math.random() - 0.48))
        currentPrice += change

        data.push({
            date: d.toISOString().split('T')[0],
            price: parseFloat(currentPrice.toFixed(2))
        })
    }

    return data
}
