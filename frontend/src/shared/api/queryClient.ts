import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './client'

/**
 * Shared TanStack Query client.
 * - Don't retry 4xx (auth/not-found/validation are terminal); retry transient errors once.
 * - 401s are already handled by the api client's interceptor (clear token + redirect).
 */
export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: (failureCount, error) => {
                if (error instanceof ApiError && error.status >= 400 && error.status < 500) {
                    return false
                }
                return failureCount < 1
            },
            refetchOnWindowFocus: false,
            staleTime: 60_000,
        },
        mutations: { retry: false },
    },
})
