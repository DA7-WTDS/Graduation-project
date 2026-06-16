import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ApiError } from '@/shared/api/client'
import { fetchQuote, searchSymbols, type MarketQuote, type MarketSearchResult } from './marketApi'

/** Debounce a fast-changing value (e.g. a search box) by `delay` ms. */
export function useDebounced<T>(value: T, delay = 350): T {
    const [debounced, setDebounced] = useState(value)
    useEffect(() => {
        const id = setTimeout(() => setDebounced(value), delay)
        return () => clearTimeout(id)
    }, [value, delay])
    return debounced
}

/** Symbol search — only fires once the (debounced) query is non-empty. */
export function useSymbolSearch(query: string) {
    return useQuery<MarketSearchResult[]>({
        queryKey: ['market', 'search', query],
        queryFn: () => searchSymbols(query),
        enabled: query.trim().length >= 1,
        staleTime: 60_000,
    })
}

/** Live quote for one symbol. Polls every 60s; a 404 (unknown symbol) → null. */
export function useQuote(symbol: string) {
    return useQuery<MarketQuote | null>({
        queryKey: ['market', 'quote', symbol],
        queryFn: async () => {
            try {
                return await fetchQuote(symbol)
            } catch (err) {
                if (err instanceof ApiError && err.status === 404) return null
                throw err
            }
        },
        staleTime: 30_000,
        refetchInterval: 60_000,
    })
}
