import { useGoals } from './useGoals'
import type { Goal, GoalProfile } from './goalsApi'

/**
 * The user's current goal plus its scored profile — the single source of truth
 * for "who is this investor" now that the legacy portfolio row is gone (§ 4.7).
 * v1 is single-goal; the multi-goal schema is already in place behind this.
 */
export function useActiveGoal() {
    const { data, isLoading, isError, refetch } = useGoals()

    const goal: Goal | null = data?.[0] ?? null
    const profile: GoalProfile | null = goal?.profile ?? null

    return {
        goal,
        profile,
        investmentAmount: goal?.investmentAmount ?? 0,
        /** Onboarding is complete once a goal has a scored profile. */
        isOnboarded: !!profile,
        isLoading,
        isError,
        refetch,
    }
}
