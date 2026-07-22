import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './client'

/** Shared TanStack Query client — mirrors the web app's retry policy. */
export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: (failureCount, error) => {
                if (error instanceof ApiError && error.status >= 400 && error.status < 500) {
                    return false
                }
                return failureCount < 1
            },
            staleTime: 60_000,
        },
        mutations: { retry: false },
    },
})
