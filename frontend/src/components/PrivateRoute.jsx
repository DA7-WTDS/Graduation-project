import React from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { LoadingState } from '../shared/ui'

const PrivateRoute = ({ children }) => {
    const { isAuthenticated, loading } = useAuth()

    // Show loading state while checking authentication
    if (loading) {
        return (
            <div style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                minHeight: '100vh',
                background: 'var(--qw-ink)'
            }}>
                <LoadingState label="Loading…" />
            </div>
        )
    }

    // Redirect to login if not authenticated
    if (!isAuthenticated) {
        return <Navigate to="/login" replace />
    }

    // Render children if authenticated
    return children
}

export default PrivateRoute
