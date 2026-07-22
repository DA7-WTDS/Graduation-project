import React from 'react'
import { NavigationContainer, DarkTheme, type Theme } from '@react-navigation/native'
import { createNativeStackNavigator } from '@react-navigation/native-stack'
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs'
import { View } from 'react-native'
import { useAuth } from '../auth/AuthContext'
import { useLanguage } from '../i18n/LanguageContext'
import { Loading } from '../components/ui'
import { colors, fontSize } from '../theme/theme'
import { useQuery } from '@tanstack/react-query'
import { getGoals } from '../api/goals'
import { LoginScreen } from '../screens/LoginScreen'
import { SignupScreen } from '../screens/SignupScreen'
import { OnboardingScreen } from '../screens/OnboardingScreen'
import { DashboardScreen } from '../screens/DashboardScreen'
import { PlanScreen } from '../screens/PlanScreen'
import { ProfileScreen } from '../screens/ProfileScreen'

const Stack = createNativeStackNavigator()
const Tabs = createBottomTabNavigator()

const navTheme: Theme = {
    ...DarkTheme,
    colors: {
        ...DarkTheme.colors,
        background: colors.ink,
        card: colors.panel,
        text: colors.text,
        primary: colors.amber,
        border: colors.border,
    },
}

function TabDot({ color }: { color: string }) {
    return <View style={{ width: 6, height: 6, borderRadius: 3, backgroundColor: color }} />
}

function MainTabs() {
    const { t } = useLanguage()
    return (
        <Tabs.Navigator
            screenOptions={{
                headerShown: false,
                tabBarStyle: { backgroundColor: colors.panel, borderTopColor: colors.border, height: 62, paddingTop: 6 },
                tabBarActiveTintColor: colors.amber,
                tabBarInactiveTintColor: colors.textFaint,
                tabBarLabelStyle: { fontSize: fontSize.xs, letterSpacing: 0.5 },
                tabBarIcon: ({ color }) => <TabDot color={color} />,
            }}
        >
            <Tabs.Screen name="Dashboard" component={DashboardScreen} options={{ tabBarLabel: t('nav.dashboard') }} />
            <Tabs.Screen name="Plan" component={PlanScreen} options={{ tabBarLabel: t('nav.plan') }} />
            <Tabs.Screen name="Profile" component={ProfileScreen} options={{ tabBarLabel: t('nav.profile') }} />
        </Tabs.Navigator>
    )
}

function AuthedStack() {
    // A returning user (already has a profiled goal) lands on the dashboard; a
    // brand-new user starts in onboarding.
    const { data: goals, isLoading } = useQuery({ queryKey: ['goals'], queryFn: getGoals })

    if (isLoading) {
        return <Loading />
    }

    const onboarded = (goals ?? []).some((g) => g.profile != null)

    return (
        <Stack.Navigator
            initialRouteName={onboarded ? 'Main' : 'Onboarding'}
            screenOptions={{ headerShown: false, contentStyle: { backgroundColor: colors.ink } }}
        >
            <Stack.Screen name="Onboarding" component={OnboardingScreen} />
            <Stack.Screen name="Main" component={MainTabs} />
        </Stack.Navigator>
    )
}

export function RootNavigator() {
    const { isAuthenticated, loading } = useAuth()

    if (loading) {
        return <Loading />
    }

    return (
        <NavigationContainer theme={navTheme}>
            {isAuthenticated ? (
                <AuthedStack />
            ) : (
                <Stack.Navigator screenOptions={{ headerShown: false, contentStyle: { backgroundColor: colors.ink } }}>
                    <Stack.Screen name="Login" component={LoginScreen} />
                    <Stack.Screen name="Signup" component={SignupScreen} />
                </Stack.Navigator>
            )}
        </NavigationContainer>
    )
}
