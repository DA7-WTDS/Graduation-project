import React from 'react'
import { View, Text, ScrollView, StyleSheet } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getGoals } from '../api/goals'
import {
    listProposals, createProposal, acceptProposal, getLivePortfolio,
    type Proposal, type LivePortfolio,
} from '../api/plan'
import { useLanguage } from '../i18n/LanguageContext'
import { Card, Eyebrow, H1, Label, Muted, StatTile, Button, Loading } from '../components/ui'
import { colors, spacing, radius, fontSize, fonts } from '../theme/theme'

const money = (n: number) => `$${Number(n).toLocaleString(undefined, { maximumFractionDigits: 0 })}`
const pct = (n: number) => `${(Number(n) * 100).toFixed(1)}%`
const signedPct = (n: number) => `${n >= 0 ? '+' : ''}${(Number(n) * 100).toFixed(1)}%`

const SLEEVE_COLORS: Record<string, string> = {
    core: colors.amber,
    tactical: colors.amberDim,
    stability: colors.textDim,
    speculative: colors.textFaint,
}

export function PlanScreen() {
    const { t } = useLanguage()
    const qc = useQueryClient()

    const goalsQ = useQuery({ queryKey: ['goals'], queryFn: getGoals })
    const goal = goalsQ.data?.find((g) => g.profile != null) ?? null
    const goalId = goal?.id

    const proposalsQ = useQuery({
        queryKey: ['proposals', goalId], queryFn: () => listProposals(goalId!), enabled: !!goalId,
    })
    const portfolioQ = useQuery({
        queryKey: ['live-portfolio', goalId], queryFn: () => getLivePortfolio(goalId!), enabled: !!goalId,
    })

    const shown: Proposal | null = proposalsQ.data?.[0] ?? null
    const accepted = proposalsQ.data?.find((p) => p.status === 'Accepted') ?? null

    const genM = useMutation({
        mutationFn: () => createProposal(goalId!),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['proposals', goalId] }),
    })
    const acceptM = useMutation({
        mutationFn: (id: string) => acceptProposal(id),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['proposals', goalId] })
            qc.invalidateQueries({ queryKey: ['live-portfolio', goalId] })
        },
    })

    if (goalsQ.isLoading) {
        return <Loading label={t('common.loading')} />
    }

    const profile = goal?.profile

    return (
        <SafeAreaView style={styles.safe} edges={['top']}>
            <ScrollView contentContainerStyle={styles.body}>
                <Eyebrow>{t('plan.title')}</Eyebrow>
                <H1 style={{ fontSize: fontSize.h2 }}>{goal ? t(`goal.${goal.type}`) : t('plan.title')}</H1>
                {profile ? (
                    <Muted style={{ marginTop: spacing.xs }}>
                        {goal!.horizonYears} {t('plan.years')} · {t(`engagement.${profile.engagement}`)}
                    </Muted>
                ) : null}

                {profile ? (
                    <View style={[styles.tiles, { marginTop: spacing.lg }]}>
                        <StatTile label={t('profile.riskProfile')} value={t(`band.${profile.riskBand}`)} valueColor={colors.amber} />
                        <StatTile label={t('profile.effectiveRisk')} value={`${profile.effectiveRisk}/100`} />
                        <StatTile label={t('profile.capacity')} value={`${profile.capacity}/100`} />
                        <StatTile label={t('profile.tolerance')} value={`${profile.tolerance}/100`} />
                    </View>
                ) : null}

                {/* Live portfolio (only after acceptance) */}
                {portfolioQ.data ? <LivePortfolioCard portfolio={portfolioQ.data} t={t} /> : null}

                {/* Proposal */}
                <Card style={{ marginTop: spacing.lg }}>
                    <View style={styles.cardHead}>
                        <Label>{t('plan.proposal')}</Label>
                    </View>

                    {proposalsQ.isLoading ? (
                        <View style={{ paddingVertical: spacing.lg }}><Loading /></View>
                    ) : shown ? (
                        <ProposalView proposal={shown} t={t} />
                    ) : (
                        <>
                            <Muted style={{ marginTop: spacing.sm, marginBottom: spacing.lg }}>{t('plan.noProposalHint')}</Muted>
                        </>
                    )}

                    <Button
                        label={genM.isPending ? t('plan.generating') : shown ? t('plan.regenerate') : t('plan.generate')}
                        onPress={() => genM.mutate()}
                        loading={genM.isPending}
                        variant={shown ? 'secondary' : 'primary'}
                        style={{ marginTop: spacing.lg }}
                    />

                    {shown && shown.status !== 'Accepted' && shown.status !== 'Superseded' ? (
                        <Button
                            label={acceptM.isPending ? t('plan.accepting') : t('plan.accept')}
                            onPress={() => acceptM.mutate(shown.id)}
                            loading={acceptM.isPending}
                            style={{ marginTop: spacing.md }}
                        />
                    ) : null}

                    {accepted ? (
                        <Text style={styles.acceptedFlag}>✓ v{accepted.version} · {t('plan.accepted')}</Text>
                    ) : null}
                </Card>
            </ScrollView>
        </SafeAreaView>
    )
}

function ProposalView({ proposal, t }: { proposal: Proposal; t: (k: string) => string }) {
    const positions = [...proposal.positions].sort((a, b) => b.weight - a.weight)
    return (
        <View style={{ marginTop: spacing.md }}>
            <Text style={styles.tplName}>{proposal.templateName}</Text>
            <Text style={styles.tplSub}>v{proposal.version} · {t(`status.${proposal.status}`)}</Text>

            {/* Allocation bar */}
            <View style={styles.allocBar}>
                {positions.map((p) => (
                    <View key={p.symbol} style={{ flex: p.weight, backgroundColor: SLEEVE_COLORS[p.sleeve] ?? colors.textFaint }} />
                ))}
            </View>

            {positions.map((p) => (
                <View key={p.symbol} style={styles.posRow}>
                    <View style={[styles.dot, { backgroundColor: SLEEVE_COLORS[p.sleeve] ?? colors.textFaint }]} />
                    <Text style={styles.posSym}>{p.symbol}</Text>
                    <Text style={styles.posSleeve}>{t(`sleeve.${p.sleeve}`)}</Text>
                    <Text style={styles.posWeight}>{pct(p.weight)}</Text>
                    <Text style={styles.posValue}>{money(p.estimatedValue)}</Text>
                </View>
            ))}

            <Text style={styles.hash}>{t('plan.audit')} #{proposal.inputsHash.slice(0, 10)}</Text>
        </View>
    )
}

function LivePortfolioCard({ portfolio, t }: { portfolio: LivePortfolio; t: (k: string) => string }) {
    const atHigh = portfolio.drawdownPct <= 0.0001
    return (
        <Card style={{ marginTop: spacing.lg }}>
            <Label>{t('plan.portfolio')}</Label>
            <View style={[styles.tiles, { marginTop: spacing.md }]}>
                <StatTile label={t('plan.value')} value={money(portfolio.nav)} valueColor={colors.amber} />
                <StatTile
                    label={t('plan.totalReturn')}
                    value={signedPct(portfolio.totalReturnPct)}
                    valueColor={portfolio.totalReturnPct >= 0 ? colors.buy : colors.sell}
                />
                <StatTile
                    label={t('plan.fromHigh')}
                    value={atHigh ? t('plan.atHigh') : `-${pct(portfolio.drawdownPct)}`}
                    valueColor={atHigh ? colors.textDim : colors.sell}
                />
                <StatTile label={t('nav.plan')} value={portfolio.templateName} />
            </View>
        </Card>
    )
}

const styles = StyleSheet.create({
    safe: { flex: 1, backgroundColor: colors.ink },
    body: { padding: spacing.xl, paddingBottom: spacing.xxl },
    tiles: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.md },
    cardHead: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    tplName: { color: colors.text, fontSize: fontSize.base, fontWeight: '700' },
    tplSub: { color: colors.textFaint, fontSize: fontSize.xs, marginTop: 2, fontFamily: fonts.mono },
    allocBar: { flexDirection: 'row', height: 8, borderRadius: 4, overflow: 'hidden', marginVertical: spacing.md, gap: 1 },
    posRow: { flexDirection: 'row', alignItems: 'center', paddingVertical: spacing.sm, gap: spacing.sm },
    dot: { width: 8, height: 8, borderRadius: 4 },
    posSym: { color: colors.text, fontSize: fontSize.sm, fontWeight: '700', fontFamily: fonts.mono, width: 48 },
    posSleeve: { color: colors.textFaint, fontSize: fontSize.xs, flex: 1 },
    posWeight: { color: colors.text, fontSize: fontSize.sm, fontFamily: fonts.mono, width: 52, textAlign: 'right' },
    posValue: { color: colors.textDim, fontSize: fontSize.sm, fontFamily: fonts.mono, width: 72, textAlign: 'right' },
    hash: { color: colors.textFaint, fontSize: fontSize.xs, fontFamily: fonts.mono, marginTop: spacing.md },
    acceptedFlag: { color: colors.buy, fontSize: fontSize.sm, fontWeight: '700', marginTop: spacing.md, textAlign: 'center' },
})
