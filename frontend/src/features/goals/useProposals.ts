import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { acceptProposal, createProposal, fetchProposals, type PortfolioProposal } from './proposalsApi'

export function useProposals(goalId: string | undefined) {
    return useQuery<PortfolioProposal[]>({
        queryKey: ['proposals', goalId],
        queryFn: () => fetchProposals(goalId as string),
        enabled: !!goalId,
    })
}

/** Generate a fresh proposal for a goal; refreshes the version list. */
export function useCreateProposal(goalId: string | undefined) {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: () => createProposal(goalId as string),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['proposals', goalId] })
        },
    })
}

/** Accept a proposal; refreshes proposals, the newly opened live portfolio, and
 * the bridged portfolio the dashboard reads. */
export function useAcceptProposal(goalId: string | undefined) {
    const queryClient = useQueryClient()
    return useMutation({
        mutationFn: (proposalId: string) => acceptProposal(proposalId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['proposals', goalId] })
            queryClient.invalidateQueries({ queryKey: ['goalPortfolio', goalId] })
            queryClient.invalidateQueries({ queryKey: ['portfolio', 'me'] })
        },
    })
}
