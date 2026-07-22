import React, { createContext, useContext, useEffect, useState, useCallback } from 'react'
import { apiCall, setToken, removeToken, getToken } from '../api/client'

export interface User {
    id: string
    email: string
    firstName: string
    lastName: string
}

interface AuthValue {
    user: User | null
    loading: boolean
    isAuthenticated: boolean
    login: (email: string, password: string) => Promise<void>
    register: (firstName: string, lastName: string, email: string, password: string) => Promise<void>
    logout: () => Promise<void>
}

const AuthContext = createContext<AuthValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = useState<User | null>(null)
    const [loading, setLoading] = useState(true)

    const loadUser = useCallback(async () => {
        try {
            if (await getToken()) {
                const profile = await apiCall<User>('/api/users/profile', { requireAuth: true })
                setUser(profile)
            }
        } catch {
            await removeToken()
            setUser(null)
        } finally {
            setLoading(false)
        }
    }, [])

    useEffect(() => {
        loadUser()
    }, [loadUser])

    const login = useCallback(async (email: string, password: string) => {
        const data = await apiCall<{ accessToken?: string }>('/api/users/login', {
            method: 'POST',
            body: JSON.stringify({ email, password }),
        })
        if (data?.accessToken) {
            await setToken(data.accessToken)
            await loadUser()
        }
    }, [loadUser])

    const register = useCallback(async (firstName: string, lastName: string, email: string, password: string) => {
        await apiCall('/api/users/register', {
            method: 'POST',
            body: JSON.stringify({ firstName, lastName, email, password }),
        })
        // Register returns no token — log in immediately so the new user lands
        // straight in onboarding.
        await login(email, password)
    }, [login])

    const logout = useCallback(async () => {
        await removeToken()
        setUser(null)
    }, [])

    return (
        <AuthContext.Provider
            value={{ user, loading, isAuthenticated: !!user, login, register, logout }}
        >
            {children}
        </AuthContext.Provider>
    )
}

export function useAuth(): AuthValue {
    const ctx = useContext(AuthContext)
    if (!ctx) {
        throw new Error('useAuth must be used within AuthProvider')
    }
    return ctx
}
