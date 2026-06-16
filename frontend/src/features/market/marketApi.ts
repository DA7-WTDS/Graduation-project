import { apiCall } from '@/shared/api/client'

export interface MarketQuote {
    symbol: string
    current: number
    change: number
    percentChange: number
    high: number
    low: number
    open: number
    previousClose: number
}

export interface MarketSearchResult {
    symbol: string
    description: string
    type: string
}

/** GET /api/market/search?q= */
export function searchSymbols(query: string): Promise<MarketSearchResult[]> {
    return apiCall<MarketSearchResult[]>(`/api/market/search?q=${encodeURIComponent(query)}`, {
        method: 'GET',
        requireAuth: true,
    })
}

/** GET /api/market/quote?symbol= */
export function fetchQuote(symbol: string): Promise<MarketQuote> {
    return apiCall<MarketQuote>(`/api/market/quote?symbol=${encodeURIComponent(symbol)}`, {
        method: 'GET',
        requireAuth: true,
    })
}
