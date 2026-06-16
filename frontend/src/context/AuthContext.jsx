import React, { createContext, useState, useContext, useEffect } from 'react'
import * as authService from '../services/authService'

const AuthContext = createContext(null)

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null)
    const [loading, setLoading] = useState(true)
    const [isAuthenticated, setIsAuthenticated] = useState(false)

    // Load user on mount if token exists
    useEffect(() => {
        loadUser()
    }, [])

    const loadUser = async () => {
        try {
            if (authService.isAuthenticated()) {
                const userData = await authService.getUserProfile()
                setUser(userData)
                setIsAuthenticated(true)
            }
        } catch (error) {
            // Token is invalid or expired - silently clear it
            authService.removeToken()
            setUser(null)
            setIsAuthenticated(false)
        } finally {
            setLoading(false)
        }
    }

    const register = async (firstName, lastName, email, password) => {
        const data = await authService.register(firstName, lastName, email, password)
        // The register endpoint does not return a token, so log the user in
        // right away. This keeps the post-signup flow (→ /onboarding, a
        // PrivateRoute) from bouncing the brand-new user back to /login.
        await login(email, password)
        return data
    }

    const login = async (email, password) => {
        const data = await authService.login(email, password)

        // Load user profile after successful login
        if (data?.accessToken) {
            await loadUser()
        }

        return data
    }

    const logout = () => {
        authService.logout()
        setUser(null)
        setIsAuthenticated(false)
    }

    const value = {
        user,
        loading,
        isAuthenticated,
        register,
        login,
        logout,
        loadUser,
    }

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// Custom hook to use auth context
export const useAuth = () => {
    const context = useContext(AuthContext)
    if (!context) {
        throw new Error('useAuth must be used within AuthProvider')
    }
    return context
}
