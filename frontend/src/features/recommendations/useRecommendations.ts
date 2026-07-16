import { useQuery } from '@tanstack/react-query'
import { useLanguage } from '@/shared/i18n'
import { fetchRecommendations } from './recommendationsApi'

/**
 * Live recommendations query. The server caches the LLM result 24h per language
 * (and regenerates automatically on the next dashboard load after it expires),
 * so we keep a long client stale time; a 404 (no daily run / no profile)
 * surfaces as an ApiError the panel renders as an empty state.
 *
 * Language is part of the key: switching to Arabic refetches genuinely Arabic
 * prose from the model rather than translating picks client-side.
 */
export function useRecommendations() {
    const { lang } = useLanguage()

    return useQuery({
        queryKey: ['recommendations', lang],
        queryFn: () => fetchRecommendations(lang),
        staleTime: 5 * 60_000,
    })
}
