import { apiCall } from './client'

export interface Pick {
    ticker: string
    action: string // BUY / HOLD / SELL
    allocation_pct: number
    reason: string
    risk_note: string
    fit: string
}

export interface Recommendations {
    summary: string
    picks: Pick[]
    generated_at: string
}

/** GET /api/recommendations — Gemini prose over the latest published run (§3.6). */
export function fetchRecommendations(lang: 'en' | 'ar' = 'en') {
    return apiCall<Recommendations>(`/api/recommendations?lang=${lang}`, { method: 'GET', requireAuth: true })
}

export interface WindowStats {
    windowDays: number
    count: number
    hitRatePct: number
    avgRealizedReturnPct: number
}

export interface TrackRecord {
    totalScored: number
    windows: WindowStats[]
}

/** GET /api/track-record — realized hit-rate, unedited (§0.3). Public. */
export function fetchTrackRecord() {
    return apiCall<TrackRecord>('/api/track-record', { method: 'GET' })
}
