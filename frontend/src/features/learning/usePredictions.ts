import { useQuery } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/client'
import { fetchPredictions, type PredictionsResponse } from './predictionsApi'

/**
 * The latest pipeline run's predictions. A 404 means "no run yet" and resolves
 * to `null` rather than an error.
 */
export function usePredictions() {
    return useQuery<PredictionsResponse | null>({
        queryKey: ['predictions', 'latest'],
        queryFn: async () => {
            try {
                return await fetchPredictions()
            } catch (err) {
                if (err instanceof ApiError && err.status === 404) return null
                throw err
            }
        },
        staleTime: 5 * 60_000,
    })
}
