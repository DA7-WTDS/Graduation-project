import { getHistoricalData } from './mockHistoricalData'

/**
 * Runs a portfolio simulation based on user allocations.
 */
export const runSimulation = ({ initialCapital, allocations, startDate, endDate }) => {
    // 1. Fetch data for all assets
    const assetData = {}
    allocations.forEach(alloc => {
        assetData[alloc.symbol] = getHistoricalData(alloc.symbol, startDate, endDate)
    })

    // 2. Fetch benchmark data (SPY)
    const benchmarkData = getHistoricalData('SPY', startDate, endDate)

    // 3. Calculate daily portfolio value
    const dailyValues = []
    const numDays = benchmarkData.length

    for (let i = 0; i < numDays; i++) {
        let dailyTotal = 0
        const date = benchmarkData[i].date

        allocations.forEach(alloc => {
            const history = assetData[alloc.symbol]
            const price = history[i] ? history[i].price : history[history.length - 1].price
            const initialPrice = history[0].price

            // Value of this allocation today
            const allocationValue = (initialCapital * (alloc.weight / 100))
            const currentAllocationValue = allocationValue * (price / initialPrice)
            dailyTotal += currentAllocationValue
        })

        const benchmarkPrice = benchmarkData[i].price
        const initialBenchmarkPrice = benchmarkData[0].price
        const benchmarkValue = initialCapital * (benchmarkPrice / initialBenchmarkPrice)

        dailyValues.push({
            date,
            portfolioValue: parseFloat(dailyTotal.toFixed(2)),
            benchmarkValue: parseFloat(benchmarkValue.toFixed(2))
        })
    }

    // 4. Calculate Stats
    const finalValue = dailyValues[dailyValues.length - 1].portfolioValue
    const totalReturn = ((finalValue - initialCapital) / initialCapital) * 100

    // Simple volatility calculation (standard deviation of daily returns)
    const dailyReturns = []
    for (let i = 1; i < dailyValues.length; i++) {
        const ret = (dailyValues[i].portfolioValue - dailyValues[i - 1].portfolioValue) / dailyValues[i - 1].portfolioValue
        dailyReturns.push(ret)
    }

    const meanReturn = dailyReturns.reduce((a, b) => a + b, 0) / dailyReturns.length
    const variance = dailyReturns.reduce((a, b) => a + Math.pow(b - meanReturn, 2), 0) / dailyReturns.length
    const stdDev = Math.sqrt(variance)

    // Annualized Volatility (assuming 252 trading days)
    const annualizedVol = stdDev * Math.sqrt(252) * 100

    return {
        dailyValues,
        stats: {
            initialCapital: parseFloat(initialCapital),
            finalValue,
            totalReturn: parseFloat(totalReturn.toFixed(2)),
            volatility: parseFloat(annualizedVol.toFixed(2)),
            sharpeRatio: parseFloat((totalReturn / (annualizedVol || 1)).toFixed(2)) // Simplified
        }
    }
}
