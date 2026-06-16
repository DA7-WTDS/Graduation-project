import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createPortfolio, updatePortfolio, type PortfolioInput } from './portfolioApi'

/**
 * Persists the onboarding questionnaire. When `existingId` is provided the
 * portfolio is updated (PUT) instead of created (POST) — this is what makes the
 * "Edit Profile" / "Retake Questionnaire" flow work for users who already have
 * a portfolio (a plain create would 409 with PortfolioAlreadyExists).
 *
 * On success the cached `['portfolio', 'me']` query is invalidated so the
 * Dashboard and Portfolios pages immediately reflect the new allocation.
 */
export function useSavePortfolio(existingId?: string | null) {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (input: PortfolioInput) =>
            existingId ? updatePortfolio(existingId, input) : createPortfolio(input),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['portfolio', 'me'] })
        },
    })
}
