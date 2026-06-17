import { useQuery } from '@tanstack/react-query'
import { fetchRecommendations } from './recommendationsApi'

/**
 * Live recommendations query. The server caches the LLM result 24h (and
 * regenerates automatically on the next dashboard load after it expires), so we
 * keep a long client stale time; a 404 (no daily run / no profile) surfaces as
 * an ApiError the panel renders as an empty state.
 */
export function useRecommendations() {
    return useQuery({
        queryKey: ['recommendations'],
        queryFn: fetchRecommendations,
        staleTime: 5 * 60_000,
    })
}
