import { apiCall } from '@/shared/api/client'

export interface RiskBucketStats {
    riskLevel: string
    count: number
    hitRatePct: number
    avgRealizedReturnPct: number
}

export interface WindowStats {
    windowDays: number
    count: number
    hitRatePct: number
    avgRealizedReturnPct: number
    byRiskLevel: RiskBucketStats[]
}

/** Rolling realized-outcome metrics (§ 0.3) — how right the predictions actually were. */
export interface TrackRecord {
    totalScored: number
    firstRunAt: string | null
    lastRunAt: string | null
    windows: WindowStats[]
}

/** GET /api/track-record — public, aggregates only. */
export function fetchTrackRecord(): Promise<TrackRecord> {
    return apiCall<TrackRecord>('/api/track-record', { method: 'GET' })
}
