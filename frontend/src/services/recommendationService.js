import { apiCall } from './apiConfig'

/**
 * Get LLM-generated recommendations for the current user.
 * Returns RecommendationResponse: { summary, picks, generated_at }
 * Each pick: { ticker, action, allocation_pct, reason, risk_note, fit }
 */
export const getRecommendations = async () => {
    return await apiCall('/api/recommendations', {
        method: 'GET',
        requireAuth: true,
    })
}
