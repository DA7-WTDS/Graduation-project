import { apiCall } from './apiConfig'

// Create a new portfolio after onboarding questionnaire
export const createPortfolio = async ({
    primaryGoal,
    timeHorizon,
    riskTolerance,
    marketReaction,
    investmentExperience,
    stocksPercentage,
    bondsPercentage,
    etfsPercentage,
    cashPercentage,
    riskProfile,
}) => {
    return await apiCall('/portfolios', {
        method: 'POST',
        requireAuth: true,
        body: JSON.stringify({
            primaryGoal,
            timeHorizon,
            riskTolerance,
            marketReaction,
            investmentExperience,
            stocksPercentage,
            bondsPercentage,
            etfsPercentage,
            cashPercentage,
            riskProfile,
        }),
    })
}

// Get the portfolio for the currently authenticated user
export const getMyPortfolio = async () => {
    return await apiCall('/portfolios/me', {
        method: 'GET',
        requireAuth: true,
    })
}

// Get a portfolio by its ID
export const getPortfolioById = async (id) => {
    return await apiCall(`/portfolios/${id}`, {
        method: 'GET',
        requireAuth: true,
    })
}

// Update an existing portfolio
export const updatePortfolio = async (id, {
    primaryGoal,
    timeHorizon,
    riskTolerance,
    marketReaction,
    investmentExperience,
    stocksPercentage,
    bondsPercentage,
    etfsPercentage,
    cashPercentage,
    riskProfile,
}) => {
    return await apiCall(`/portfolios/${id}`, {
        method: 'PUT',
        requireAuth: true,
        body: JSON.stringify({
            primaryGoal,
            timeHorizon,
            riskTolerance,
            marketReaction,
            investmentExperience,
            stocksPercentage,
            bondsPercentage,
            etfsPercentage,
            cashPercentage,
            riskProfile,
        }),
    })
}
