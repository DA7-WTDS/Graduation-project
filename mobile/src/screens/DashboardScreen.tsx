import React from 'react'
import { View, Text, ScrollView, StyleSheet } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { useQuery } from '@tanstack/react-query'
import { getGoals } from '../api/goals'
import { fetchRecommendations, fetchTrackRecord, type Pick } from '../api/recommendations'
import { useLanguage } from '../i18n/LanguageContext'
import { Card, Eyebrow, H1, Label, Muted, StatTile, Loading, Button, LangToggle } from '../components/ui'
import { colors, spacing, radius, fontSize, fonts, signalColor } from '../theme/theme'

const pct = (n: number) => `${Number(n).toFixed(1)}%`
const signed = (n: number) => `${n >= 0 ? '+' : ''}${Number(n).toFixed(2)}%`

export function DashboardScreen({ navigation }: any) {
    const { t, lang, toggle } = useLanguage()
    const goalsQ = useQuery({ queryKey: ['goals'], queryFn: getGoals })
    const goal = goalsQ.data?.find((g) => g.profile != null) ?? null
    const profile = goal?.profile ?? null

    const recsQ = useQuery({
        queryKey: ['recommendations', lang],
        queryFn: () => fetchRecommendations(lang),
        enabled: !!profile,
    })
    const trackQ = useQuery({ queryKey: ['track-record'], queryFn: fetchTrackRecord })
    const window = trackQ.data?.windows?.find((w) => w.windowDays === 90) ?? trackQ.data?.windows?.[0]

    if (goalsQ.isLoading) {
        return <Loading label={t('common.loading')} />
    }

    return (
        <SafeAreaView style={styles.safe} edges={['top']}>
            <View style={styles.header}>
                <View>
                    <Eyebrow>{t('dash.eyebrow')}</Eyebrow>
                    <H1 style={{ fontSize: fontSize.h2 }}>{t('dash.title')}</H1>
                </View>
                <LangToggle label={t('common.langSwitch')} onPress={toggle} />
            </View>

            <ScrollView contentContainerStyle={styles.body}>
                {!profile ? (
                    <Card>
                        <Label>{t('dash.noGoal')}</Label>
                        <Muted style={{ marginTop: spacing.sm, marginBottom: spacing.lg }}>{t('dash.noGoalCopy')}</Muted>
                        <Button label={t('dash.startOnboarding')} onPress={() => navigation.navigate('Onboarding')} />
                    </Card>
                ) : (
                    <View style={styles.tiles}>
                        <StatTile label={t('profile.riskProfile')} value={t(`band.${profile.riskBand}`)} valueColor={colors.amber} />
                        <StatTile label={t('profile.effectiveRisk')} value={`${profile.effectiveRisk}/100`} />
                        <StatTile label={t('profile.goal')} value={t(`goal.${goal!.type}`)} />
                        <StatTile label={t('profile.horizon')} value={`${goal!.horizonYears} ${t('plan.years')}`} />
                    </View>
                )}

                {/* AI recommendations */}
                {profile ? (
                    <Card style={{ marginTop: spacing.lg }}>
                        <Label>{t('dash.recommendations')}</Label>
                        {recsQ.isLoading ? (
                            <View style={{ paddingVertical: spacing.xl }}><Loading /></View>
                        ) : recsQ.data?.picks?.length ? (
                            <>
                                <Muted style={styles.recSummary}>{recsQ.data.summary}</Muted>
                                {recsQ.data.picks.slice(0, 6).map((p) => <PickRow key={p.ticker} pick={p} />)}
                            </>
                        ) : (
                            <Muted style={{ marginTop: spacing.md }}>{t('dash.noRecs')}</Muted>
                        )}
                    </Card>
                ) : null}

                {/* Honest track record */}
                {window && window.count > 0 ? (
                    <Card style={{ marginTop: spacing.lg }}>
                        <Label>{t('track.title')}</Label>
                        <View style={[styles.tiles, { marginTop: spacing.md }]}>
                            <StatTile label={t('track.hitRate')} value={pct(window.hitRatePct)} valueColor={colors.amber} />
                            <StatTile
                                label={t('track.avgReturn')}
                                value={signed(window.avgRealizedReturnPct)}
                                valueColor={window.avgRealizedReturnPct >= 0 ? colors.buy : colors.sell}
                            />
                            <StatTile label={t('track.scored')} value={String(window.count)} />
                            <StatTile label={t('track.allTime')} value={String(trackQ.data?.totalScored ?? 0)} />
                        </View>
                        <Muted style={styles.trackNote}>{t('track.note')}</Muted>
                    </Card>
                ) : null}
            </ScrollView>
        </SafeAreaView>
    )
}

function PickRow({ pick }: { pick: Pick }) {
    return (
        <View style={styles.pick}>
            <View style={styles.pickHead}>
                <Text style={styles.pickTicker}>{pick.ticker}</Text>
                <View style={styles.pickRight}>
                    <Text style={[styles.pickAction, { color: signalColor(pick.action) }]}>{pick.action}</Text>
                    <Text style={styles.pickAlloc}>{pick.allocation_pct}%</Text>
                </View>
            </View>
            <Text style={styles.pickReason}>{pick.reason}</Text>
        </View>
    )
}

const styles = StyleSheet.create({
    safe: { flex: 1, backgroundColor: colors.ink },
    header: {
        flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
        paddingHorizontal: spacing.xl, paddingTop: spacing.md, paddingBottom: spacing.sm,
    },
    body: { padding: spacing.xl, paddingTop: spacing.md, paddingBottom: spacing.xxl },
    tiles: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.md },
    recSummary: { marginTop: spacing.sm, marginBottom: spacing.md, fontSize: fontSize.sm, lineHeight: 20 },
    pick: { borderTopColor: colors.border, borderTopWidth: 1, paddingVertical: spacing.md },
    pickHead: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    pickTicker: { color: colors.text, fontSize: fontSize.base, fontWeight: '700', fontFamily: fonts.mono },
    pickRight: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
    pickAction: { fontSize: fontSize.xs, fontWeight: '700', letterSpacing: 1 },
    pickAlloc: { color: colors.textDim, fontSize: fontSize.sm, fontFamily: fonts.mono },
    pickReason: { color: colors.textFaint, fontSize: fontSize.sm, marginTop: 4, lineHeight: 18 },
    trackNote: { marginTop: spacing.md, fontSize: fontSize.xs, lineHeight: 16, color: colors.textFaint },
})
