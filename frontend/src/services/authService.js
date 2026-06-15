import { apiCall, getToken, setToken, removeToken } from '@/shared/api/client'

// Token management (re-exported from the shared client so existing imports keep working)
export { getToken, setToken, removeToken }

// Register a new user
export const register = async (firstName, lastName, email, password) => {
    const data = await apiCall('/api/users/register', {
        method: 'POST',
        body: JSON.stringify({
            firstName,
            lastName,
            email,
            password,
        }),
    })

    return data
}

// Login user
export const login = async (email, password) => {
    const data = await apiCall('/api/users/login', {
        method: 'POST',
        body: JSON.stringify({
            email,
            password,
        }),
    })

    // Store token if login successful
    if (data?.accessToken) {
        setToken(data.accessToken)
    }

    return data
}

// Get current user profile
export const getUserProfile = async () => {
    const data = await apiCall('/api/users/profile', {
        method: 'GET',
        requireAuth: true,
    })

    return data
}

// Logout user
export const logout = () => {
    removeToken()
}

// Check if user is authenticated
export const isAuthenticated = () => {
    return !!getToken()
}
