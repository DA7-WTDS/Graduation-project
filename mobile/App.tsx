import React from 'react'
import { StatusBar } from 'expo-status-bar'
import { SafeAreaProvider } from 'react-native-safe-area-context'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from './src/api/queryClient'
import { AuthProvider } from './src/auth/AuthContext'
import { LanguageProvider } from './src/i18n/LanguageContext'
import { RootNavigator } from './src/navigation/RootNavigator'

export default function App() {
    return (
        <SafeAreaProvider>
            <QueryClientProvider client={queryClient}>
                <LanguageProvider>
                    <AuthProvider>
                        <StatusBar style="light" />
                        <RootNavigator />
                    </AuthProvider>
                </LanguageProvider>
            </QueryClientProvider>
        </SafeAreaProvider>
    )
}
