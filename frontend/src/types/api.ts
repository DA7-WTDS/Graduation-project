/**
 * Shared API DTOs mirroring the .NET backend contracts.
 * (Frontend source of truth for the shapes the UI renders.)
 */

export type SignalAction = 'BUY' | 'SELL' | 'HOLD'
export type RiskLevel = 'LOW' | 'MEDIUM' | 'HIGH'
export type RiskProfile = 'Conservative' | 'Moderate' | 'Aggressive'

/** GET /api/recommendations */
export interface RecommendationItem {
    ticker: string
    action: SignalAction
    allocation_pct: number
    reason: string
    risk_note: string
    fit: string
}

export interface RecommendationResponse {
    summary: string
    picks: RecommendationItem[]
    generated_at: string
}

/** GET /api/portfolios/me */
export interface Portfolio {
    id: string
    userId: string
    primaryGoal: string
    timeHorizon: string
    riskTolerance: number
    marketReaction: string
    investmentExperience: string
    stocksPercentage: number
    bondsPercentage: number
    etfsPercentage: number
    cashPercentage: number
    riskProfile: RiskProfile
    investmentAmount: number
    createdAt: string
    updatedAt?: string | null
}

/** GET /api/users/profile */
export interface UserProfile {
    id: string
    firstName: string
    lastName: string
    email: string
    role?: string
    createdAt?: string
}

export type NotificationKind = 'Info' | 'Warning' | 'Success' | (string & {})

/** GET /api/notifications */
export interface AppNotification {
    id: string
    title: string
    message: string
    type: NotificationKind
    isRead: boolean
    createdAt: string
}
