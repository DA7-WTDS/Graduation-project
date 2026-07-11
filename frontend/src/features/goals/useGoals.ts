import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchGoals, submitQuestionnaire, type Goal, type QuestionnaireInput } from './goalsApi'

export function useGoals() {
    return useQuery<Goal[]>({
        queryKey: ['goals'],
        queryFn: fetchGoals,
    })
}

/**
 * Submits the questionnaire for server-side scoring. Invalidates both the goals
 * list and the bridged portfolio so the Dashboard picks up the new allocation.
 */
export function useSubmitQuestionnaire() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (input: QuestionnaireInput) => submitQuestionnaire(input),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['goals'] })
            queryClient.invalidateQueries({ queryKey: ['portfolio', 'me'] })
        },
    })
}
