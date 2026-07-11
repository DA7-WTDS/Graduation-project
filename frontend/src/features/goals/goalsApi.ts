import { apiCall } from '@/shared/api/client'

/** Raw questionnaire answers (§ 2.1). The client computes nothing — the server
 * scores capacity/tolerance and derives the profile + allocation. */
export interface QuestionnaireInput {
    goalId?: string | null
    goalType: string
    horizonYears: number
    investmentAmount: number
    monthlyContribution: number
    hasEmergencyFund: boolean
    incomeStability: string
    savingsShare: string
    marketReaction: string
    experience: string
    engagement: string
    usdComfort: string
    affordLossConfirmed: boolean
}

export interface QuestionnaireResult {
    goalId: string
    profileId: string
    profileVersion: number
    scoringVersion: string
    capacity: number
    tolerance: number
    effectiveRisk: number
    riskBand: string
    speculativeUnlocked: boolean
    engagement: string
    usdComfort: string
    portfolioId: string
    stocksPercentage: number
    bondsPercentage: number
    etfsPercentage: number
    cashPercentage: number
}

export interface GoalProfile {
    profileId: string
    version: number
    scoringVersion: string
    capacity: number
    tolerance: number
    effectiveRisk: number
    riskBand: string
    engagement: string
    usdComfort: string
    speculativeUnlocked: boolean
    createdAt: string
}

export interface Goal {
    id: string
    type: string
    horizonYears: number
    createdAt: string
    updatedAt: string | null
    profile: GoalProfile | null
}

/** GET /api/goals */
export function fetchGoals(): Promise<Goal[]> {
    return apiCall<Goal[]>('/api/goals', { method: 'GET', requireAuth: true })
}

/** POST /api/goals/questionnaire — pass goalId to retake (new profile version). */
export function submitQuestionnaire(input: QuestionnaireInput): Promise<QuestionnaireResult> {
    return apiCall<QuestionnaireResult>('/api/goals/questionnaire', {
        method: 'POST',
        requireAuth: true,
        body: JSON.stringify(input),
    })
}
