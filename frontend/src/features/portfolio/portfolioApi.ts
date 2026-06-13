import { apiCall } from '@/shared/api/client'
import type { Portfolio } from '@/types/api'

export interface PortfolioInput {
    primaryGoal: string
    timeHorizon: string
    riskTolerance: number
    marketReaction: string
    investmentExperience: string
    stocksPercentage: number
    bondsPercentage: number
    etfsPercentage: number
    cashPercentage: number
    riskProfile: string
}

/** GET /api/portfolios/me */
export function fetchMyPortfolio(): Promise<Portfolio> {
    return apiCall<Portfolio>('/api/portfolios/me', { method: 'GET', requireAuth: true })
}

/** POST /api/portfolios */
export function createPortfolio(input: PortfolioInput): Promise<{ id: string }> {
    return apiCall<{ id: string }>('/api/portfolios', {
        method: 'POST',
        requireAuth: true,
        body: JSON.stringify(input),
    })
}

/** PUT /api/portfolios/{id} */
export function updatePortfolio(id: string, input: PortfolioInput): Promise<void> {
    return apiCall<void>(`/api/portfolios/${id}`, {
        method: 'PUT',
        requireAuth: true,
        body: JSON.stringify(input),
    })
}
