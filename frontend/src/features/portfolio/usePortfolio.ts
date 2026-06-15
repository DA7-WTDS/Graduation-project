import { useQuery } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/client'
import type { Portfolio } from '@/types/api'
import { fetchMyPortfolio } from './portfolioApi'

/**
 * The current user's portfolio. A 404 means "no portfolio yet" (onboarding not
 * completed) and resolves to `null` rather than an error.
 */
export function usePortfolio() {
    return useQuery<Portfolio | null>({
        queryKey: ['portfolio', 'me'],
        queryFn: async () => {
            try {
                return await fetchMyPortfolio()
            } catch (err) {
                if (err instanceof ApiError && err.status === 404) return null
                throw err
            }
        },
    })
}
