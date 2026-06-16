import React from 'react'
import ReactDOM from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { MotionConfig } from 'motion/react'
import App from './App.jsx'
import './shared/styles/fonts'
import './index.css'
import { AuthProvider } from './context/AuthContext'
import { queryClient } from './shared/api/queryClient'
import { ToastProvider } from './shared/ui'

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <QueryClientProvider client={queryClient}>
            <MotionConfig reducedMotion="user">
                <AuthProvider>
                    <ToastProvider>
                        <App />
                    </ToastProvider>
                </AuthProvider>
            </MotionConfig>
        </QueryClientProvider>
    </React.StrictMode>,
)
