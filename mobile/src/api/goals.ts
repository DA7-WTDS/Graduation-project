import { apiCall } from './client'

export interface InvestorProfile {
    riskBand: string
    effectiveRisk: number
    capacity: number
    tolerance: number
    engagement: string
    speculativeUnlocked: boolean
}

export interface Goal {
    id: string
    type: string
    horizonYears: number
    investmentAmount: number
    profile: InvestorProfile | null
}

/** The raw answers the questionnaire posts — scoring happens server-side (§2). */
export interface QuestionnaireAnswers {
    goalId: string | null
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

export function submitQuestionnaire(answers: QuestionnaireAnswers) {
    return apiCall<Goal>('/api/goals/questionnaire', {
        method: 'POST',
        requireAuth: true,
        body: JSON.stringify(answers),
    })
}

export function getGoals() {
    return apiCall<Goal[]>('/api/goals', { method: 'GET', requireAuth: true })
}
