import { apiCall } from '@/shared/api/client'

export interface PredictionItem {
    ticker: string
    direction: string // UP / DOWN
    /** Model score. Read `scoreScale` before rendering: under 'relative' this is
     *  expected out/under-performance vs the universe median in percentage points,
     *  NOT a predicted price move — never multiply it by a cash amount. */
    changePct: number
    confidence: number
    signal: string // POSITIVE / NEUTRAL / NEGATIVE
    riskLevel: string // LOW / MEDIUM / HIGH
    convictionScore: number
    rationale: string
}

export interface PredictionsResponse {
    generatedAt: string
    predictions: PredictionItem[]
    /** 'relative' (trees champion, vs universe median) | 'absolute' (legacy hybrid,
     *  30-day return). Decides how every changePct above may be presented. */
    scoreScale: 'relative' | 'absolute'
}

/** GET /api/predictions — latest pipeline run, market-wide (not personalized). */
export function fetchPredictions(): Promise<PredictionsResponse> {
    return apiCall<PredictionsResponse>('/api/predictions', { method: 'GET', requireAuth: true })
}
