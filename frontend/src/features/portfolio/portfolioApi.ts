import { apiCall } from '@/shared/api/client'
import type { Portfolio } from '@/types/api'

// Writes go through the questionnaire now (features/goals): the server scores
// the answers and maintains this portfolio itself. The client only reads it.

/** GET /api/portfolios/me */
export function fetchMyPortfolio(): Promise<Portfolio> {
    return apiCall<Portfolio>('/api/portfolios/me', { method: 'GET', requireAuth: true })
}
