import { apiCall, ApiError } from './client'

export interface Position {
    symbol: string
    sleeve: string
    weight: number
    estimatedValue: number
    rationale: string
}

export interface Proposal {
    id: string
    version: number
    templateName: string
    status: string // Proposed / Accepted / Superseded
    rebalanceCadence: string
    drawdownAlertPct: number
    positions: Position[]
    assumptions: string[]
    inputsHash: string
    createdAt: string
}

export function listProposals(goalId: string) {
    return apiCall<Proposal[]>(`/api/goals/${goalId}/proposals`, { method: 'GET', requireAuth: true })
}

export function createProposal(goalId: string) {
    return apiCall<Proposal>(`/api/goals/${goalId}/proposals`, {
        method: 'POST', requireAuth: true, body: JSON.stringify({}),
    })
}

export function acceptProposal(proposalId: string) {
    return apiCall<Proposal>(`/api/portfolio-proposals/${proposalId}/accept`, {
        method: 'POST', requireAuth: true, body: JSON.stringify({}),
    })
}

export interface LivePosition {
    symbol: string
    sleeve: string
    shares: number
    currentValue: number | null
    targetWeight: number
    actualWeight: number | null
    driftPct: number | null
}

export interface LivePortfolio {
    templateName: string
    amount: number
    nav: number
    totalReturnPct: number
    drawdownPct: number
    nextReviewDate: string | null
    pricesComplete: boolean
    positions: LivePosition[]
}

/** GET /api/goals/{id}/portfolio — 404 until a proposal is accepted (→ null). */
export async function getLivePortfolio(goalId: string): Promise<LivePortfolio | null> {
    try {
        return await apiCall<LivePortfolio>(`/api/goals/${goalId}/portfolio`, { method: 'GET', requireAuth: true })
    } catch (e) {
        if (e instanceof ApiError && e.status === 404) {
            return null
        }
        throw e
    }
}
