import { apiCall } from '@/shared/api/client'
import type { RecommendationResponse } from '@/types/api'

/** GET /api/recommendations — LLM-personalized BUY/SELL/HOLD picks for the current user. */
export function fetchRecommendations(): Promise<RecommendationResponse> {
    return apiCall<RecommendationResponse>('/api/recommendations', {
        method: 'GET',
        requireAuth: true,
    })
}
