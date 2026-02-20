import { apiCall } from './apiConfig'

// Token management
export const getToken = () => localStorage.getItem('token')
export const setToken = (token) => localStorage.setItem('token', token)
export const removeToken = () => localStorage.removeItem('token')

// Register a new user
export const register = async (firstName, lastName, email, password) => {
    const data = await apiCall('/users/register', {
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
    const data = await apiCall('/users/login', {
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
    const data = await apiCall('/users/profile', {
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
