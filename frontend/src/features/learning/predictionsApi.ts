import { apiCall } from '@/shared/api/client'

export interface PredictionItem {
    ticker: string
    direction: string // UP / DOWN
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
}

/** GET /api/predictions — latest pipeline run, market-wide (not personalized). */
export function fetchPredictions(): Promise<PredictionsResponse> {
    return apiCall<PredictionsResponse>('/api/predictions', { method: 'GET', requireAuth: true })
}
