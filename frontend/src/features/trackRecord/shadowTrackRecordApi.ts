import { apiCall } from '@/shared/api/client'

/** One day of a model portfolio's NAV history. */
export interface ShadowNavPoint {
    date: string
    nav: number
    dailyReturn: number
    /** True on days the portfolio traded to new target weights (costs charged). */
    rebalanced: boolean
}

export interface ShadowSeries {
    templateKey: string
    templateName: string
    riskBand: string
    rebalanceCadence: string
    notional: number
    inceptionDate: string
    currentNav: number
    totalReturn: number
    annualizedReturn: number
    maxDrawdown: number
    days: number
    series: ShadowNavPoint[]
}

export interface ShadowTrackRecord {
    /** FRA-safe wording written by the backend. Render it verbatim, always. */
    disclaimer: string
    portfolios: ShadowSeries[]
}

/**
 * GET /api/shadow-track-record — public, anonymous (§ 6.1).
 *
 * Each strategy template run daily as a fixed-notional paper portfolio since
 * inception, with the backtester's transaction-cost model. Hypothetical results,
 * never the returns of a real client account.
 */
export function fetchShadowTrackRecord(): Promise<ShadowTrackRecord> {
    return apiCall<ShadowTrackRecord>('/api/shadow-track-record', { method: 'GET' })
}
