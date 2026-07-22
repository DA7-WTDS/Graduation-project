import React from 'react'
import { View, Text, ScrollView, StyleSheet } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { useQuery } from '@tanstack/react-query'
import { getGoals } from '../api/goals'
import { useAuth } from '../auth/AuthContext'
import { useLanguage } from '../i18n/LanguageContext'
import { Card, Eyebrow, H1, Label, Muted, StatTile, Button, LangToggle } from '../components/ui'
import { colors, spacing, fontSize, fonts } from '../theme/theme'

export function ProfileScreen() {
    const { user, logout } = useAuth()
    const { t, lang, toggle } = useLanguage()
    const goalsQ = useQuery({ queryKey: ['goals'], queryFn: getGoals })
    const profile = goalsQ.data?.find((g) => g.profile != null)?.profile ?? null

    return (
        <SafeAreaView style={styles.safe} edges={['top']}>
            <ScrollView contentContainerStyle={styles.body}>
                <Eyebrow>{t('nav.profile')}</Eyebrow>
                <H1 style={{ fontSize: fontSize.h2 }}>{user ? `${user.firstName} ${user.lastName}` : ''}</H1>
                {user?.email ? <Muted style={{ marginTop: spacing.xs }}>{user.email}</Muted> : null}

                {profile ? (
                    <Card style={{ marginTop: spacing.xl }}>
                        <Label>{t('profile.riskProfile')}</Label>
                        <View style={[styles.tiles, { marginTop: spacing.md }]}>
                            <StatTile label={t('profile.effectiveRisk')} value={`${profile.effectiveRisk}/100`} valueColor={colors.amber} />
                            <StatTile label={t('profile.capacity')} value={`${profile.capacity}/100`} />
                            <StatTile label={t('profile.tolerance')} value={`${profile.tolerance}/100`} />
                            <StatTile label={t('profile.riskProfile')} value={t(`band.${profile.riskBand}`)} />
                        </View>
                    </Card>
                ) : null}

                <View style={styles.row}>
                    <Text style={styles.rowLabel}>Language</Text>
                    <LangToggle label={t('common.langSwitch')} onPress={toggle} />
                </View>

                <Button label={t('profile.signOut')} variant="secondary" onPress={logout} style={{ marginTop: spacing.xl }} />
            </ScrollView>
        </SafeAreaView>
    )
}

const styles = StyleSheet.create({
    safe: { flex: 1, backgroundColor: colors.ink },
    body: { padding: spacing.xl, paddingBottom: spacing.xxl },
    tiles: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.md },
    row: {
        flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
        marginTop: spacing.xl, paddingVertical: spacing.md,
        borderTopColor: colors.border, borderTopWidth: 1,
    },
    rowLabel: { color: colors.textDim, fontSize: fontSize.base },
})
