// API Configuration
export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001'

// Common headers for API requests
export const getHeaders = (includeAuth = false) => {
    const headers = {
        'Content-Type': 'application/json',
    }

    if (includeAuth) {
        const token = localStorage.getItem('token')
        if (token) {
            headers['Authorization'] = `Bearer ${token}`
        }
    }

    return headers
}

// Helper function for API calls
export const apiCall = async (endpoint, options = {}) => {
    const url = `${API_BASE_URL}${endpoint}`

    try {
        const response = await fetch(url, {
            ...options,
            headers: {
                ...getHeaders(options.requireAuth),
                ...options.headers,
            },
        })

        // Parse response
        const data = await response.json().catch(() => null)

        if (!response.ok) {
            // Extract error message from response
            const errorMessage = data?.detail || data?.title || data?.message || `HTTP ${response.status}: ${response.statusText}`
            throw new Error(errorMessage)
        }

        return data
    } catch (error) {
        console.error('API call failed:', error)
        throw error
    }
}
