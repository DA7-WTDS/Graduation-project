import { useQuery } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/client'
import { fetchGoalPortfolio, type GoalPortfolio } from './goalPortfolioApi'

/**
 * The goal's live portfolio. A 404 means "nothing accepted yet" — a normal
 * state, not an error, so it resolves to null.
 */
export function useGoalPortfolio(goalId: string | undefined) {
    return useQuery<GoalPortfolio | null>({
        queryKey: ['goalPortfolio', goalId],
        enabled: !!goalId,
        queryFn: async () => {
            try {
                return await fetchGoalPortfolio(goalId as string)
            } catch (err) {
                if (err instanceof ApiError && err.status === 404) return null
                throw err
            }
        },
    })
}
