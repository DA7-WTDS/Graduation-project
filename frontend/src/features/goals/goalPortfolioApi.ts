import { apiCall } from '@/shared/api/client'

/** One live position. currentPrice/actualWeight/driftPct are null when the
 * registry has no price for the symbol right now. */
export interface LivePosition {
    symbol: string
    sleeve: string
    shares: number
    entryPrice: number
    currentPrice: number | null
    currentValue: number | null
    targetWeight: number
    actualWeight: number | null
    driftPct: number | null
}

/** The goal's accepted portfolio, marked to market (§ 4.4). */
export interface GoalPortfolio {
    goalId: string
    proposalId: string
    templateKey: string
    templateName: string
    rebalanceCadence: string
    amount: number
    inceptionDate: string
    nextReviewDate: string
    nav: number
    highWaterMarkNav: number
    drawdownPct: number
    totalReturnPct: number
    valuedAt: string | null
    pricesComplete: boolean
    drawdownThreshold: number
    drawdownAlertActive: boolean
    driftAlertActive: boolean
    positions: LivePosition[]
}

/** GET /api/goals/{goalId}/portfolio — 404 until a proposal is accepted. */
export function fetchGoalPortfolio(goalId: string): Promise<GoalPortfolio> {
    return apiCall<GoalPortfolio>(`/api/goals/${goalId}/portfolio`, { method: 'GET', requireAuth: true })
}
