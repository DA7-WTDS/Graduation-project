import { apiCall } from '@/shared/api/client'

/** One position in a proposal — mirrors the backend ProposalPositionDto. */
export interface ProposalPosition {
    symbol: string
    sleeve: string
    weight: number
    estimatedValue: number
    rationale: string
}

/** A versioned, immutable portfolio proposal (§ 4.1). Status is one of
 * Proposed / Accepted / Superseded. */
export interface PortfolioProposal {
    id: string
    goalId: string
    version: number
    status: string
    templateKey: string
    templateName: string
    rebalanceCadence: string
    drawdownAlertPct: number
    riskBand: string
    effectiveRisk: number
    amount: number
    positions: ProposalPosition[]
    assumptions: string[]
    inputsHash: string
    createdAt: string
    acceptedAt: string | null
}

/** GET /api/goals/{goalId}/proposals — newest version first. */
export function fetchProposals(goalId: string): Promise<PortfolioProposal[]> {
    return apiCall<PortfolioProposal[]>(`/api/goals/${goalId}/proposals`, { method: 'GET', requireAuth: true })
}

/** POST /api/goals/{goalId}/proposals — runs the optimizer, persists the next version. */
export function createProposal(goalId: string): Promise<PortfolioProposal> {
    return apiCall<PortfolioProposal>(`/api/goals/${goalId}/proposals`, {
        method: 'POST',
        requireAuth: true,
        body: JSON.stringify({}),
    })
}

/** POST /api/portfolio-proposals/{proposalId}/accept — accept + supersede prior. */
export function acceptProposal(proposalId: string): Promise<PortfolioProposal> {
    return apiCall<PortfolioProposal>(`/api/portfolio-proposals/${proposalId}/accept`, {
        method: 'POST',
        requireAuth: true,
        body: JSON.stringify({}),
    })
}
