import { apiCall } from '@/shared/api/client'
import type { RecommendationResponse } from '@/types/api'

/** GET /api/recommendations — LLM-personalized BUY/SELL/HOLD picks for the current
 * user. `lang` asks the server for Arabic prose (§ 3.6); tickers and actions stay
 * English either way. */
export function fetchRecommendations(lang: string = 'en'): Promise<RecommendationResponse> {
    return apiCall<RecommendationResponse>(`/api/recommendations?lang=${encodeURIComponent(lang)}`, {
        method: 'GET',
        requireAuth: true,
    })
}
