import React, { useState } from 'react'
import {
    View, Text, ScrollView, StyleSheet, TextInput, Pressable,
} from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { useMutation } from '@tanstack/react-query'
import { submitQuestionnaire, type QuestionnaireAnswers } from '../api/goals'
import { useLanguage } from '../i18n/LanguageContext'
import { Eyebrow, H1, Button, OptionCard, ErrorNote, Muted } from '../components/ui'
import { colors, spacing, radius, fontSize, fonts } from '../theme/theme'

type Choice = { id: string | number | boolean; title: string; description?: string }
type Step =
    | { key: keyof QuestionnaireAnswers; kind: 'choice'; title: string; sub?: string; options: Choice[] }
    | { key: keyof QuestionnaireAnswers; kind: 'number'; title: string; sub?: string; prefix?: string; placeholder: string }

// The 10-question suitability flow (§2.1), one screen at a time. Raw answers only
// — capacity/tolerance/band are computed server-side.
const STEPS: Step[] = [
    {
        key: 'goalType', kind: 'choice', title: "What's this money for?",
        options: [
            { id: 'retirement', title: 'Retirement', description: 'Set-and-forget wealth for later in life' },
            { id: 'long_term_wealth', title: 'Long-term wealth', description: 'Grow capital over many years' },
            { id: 'medium_term_goal', title: 'A medium-term goal', description: 'Home, wedding, education — a few years out' },
            { id: 'speculation_learning', title: 'Speculation & learning', description: 'Active, higher-risk investing' },
        ],
    },
    {
        key: 'horizonYears', kind: 'choice', title: 'When will you need it?',
        options: [
            { id: 0, title: 'Under 1 year' }, { id: 1, title: '1–2 years' }, { id: 3, title: '3–4 years' },
            { id: 5, title: '5–9 years' }, { id: 10, title: '10+ years' },
        ],
    },
    { key: 'investmentAmount', kind: 'number', title: 'How much are you investing?', prefix: '$', placeholder: '10,000' },
    { key: 'monthlyContribution', kind: 'number', title: 'Monthly top-up? (optional)', sub: 'Leave 0 if none', prefix: '$', placeholder: '0' },
    {
        key: 'hasEmergencyFund', kind: 'choice', title: 'Do you have an emergency fund?',
        options: [
            { id: true, title: 'Yes', description: 'I have a separate safety cushion' },
            { id: false, title: 'No', description: 'This is most of my available cash' },
        ],
    },
    {
        key: 'incomeStability', kind: 'choice', title: 'How stable is your income?',
        options: [
            { id: 'stable', title: 'Stable', description: 'Salary or reliable recurring income' },
            { id: 'variable', title: 'Variable', description: 'Freelance, commission, seasonal' },
            { id: 'none', title: 'No income', description: 'Student, between jobs, retired' },
        ],
    },
    {
        key: 'savingsShare', kind: 'choice', title: 'What share of your savings is this?',
        options: [
            { id: 'less_than_ten_percent', title: 'Under 10%' },
            { id: 'ten_to_twenty_five_percent', title: '10–25%' },
            { id: 'twenty_five_to_fifty_percent', title: '25–50%' },
            { id: 'more_than_fifty_percent', title: 'Over 50%' },
        ],
    },
    {
        key: 'marketReaction', kind: 'choice', title: 'Markets drop 20%. You…',
        options: [
            { id: 'buy_more', title: 'Buy more', description: 'Prices are on sale — a buying opportunity' },
            { id: 'hold_steady', title: 'Hold steady', description: 'Stay calm and wait it out' },
            { id: 'sell_some', title: 'Sell some', description: 'Reduce exposure to limit further losses' },
            { id: 'sell_all', title: 'Sell everything', description: "I couldn't sleep — I'd get out" },
        ],
    },
    {
        key: 'experience', kind: 'choice', title: 'Your investing experience?',
        options: [
            { id: 'none', title: 'None', description: 'This is my first time' },
            { id: 'beginner', title: 'Beginner', description: 'Under a year of investing' },
            { id: 'intermediate', title: 'Intermediate', description: '1–5 years of investing' },
            { id: 'experienced', title: 'Experienced', description: '5+ years of active investing' },
        ],
    },
    {
        key: 'engagement', kind: 'choice', title: 'How closely will you follow it?',
        options: [
            { id: 'daily', title: 'Daily', description: 'I want signals and updates every day' },
            { id: 'monthly', title: 'Monthly', description: 'A monthly review works for me' },
            { id: 'set_and_forget', title: 'Set & forget', description: 'Only alert me when it truly matters' },
        ],
    },
    {
        key: 'usdComfort', kind: 'choice', title: 'Comfort with USD assets?',
        options: [
            { id: 'comfortable', title: 'Comfortable', description: 'I want USD assets as a hedge' },
            { id: 'neutral', title: 'No preference', description: 'Whatever fits my plan best' },
            { id: 'prefer_egp', title: 'Prefer EGP', description: 'Keep me mostly in local assets' },
        ],
    },
]

const initialAnswers: QuestionnaireAnswers = {
    goalId: null, goalType: '', horizonYears: -1, investmentAmount: 0, monthlyContribution: 0,
    hasEmergencyFund: undefined as any, incomeStability: '', savingsShare: '', marketReaction: '',
    experience: '', engagement: '', usdComfort: '', affordLossConfirmed: false,
}

export function OnboardingScreen({ navigation }: any) {
    const { t } = useLanguage()
    const [step, setStep] = useState(0)
    const [answers, setAnswers] = useState<QuestionnaireAnswers>(initialAnswers)

    const mutation = useMutation({
        mutationFn: submitQuestionnaire,
        onSuccess: () => navigation.reset({ index: 0, routes: [{ name: 'Main' }] }),
    })

    const current = STEPS[step]
    const isLast = step === STEPS.length - 1

    const value = answers[current.key]
    const answered =
        current.kind === 'number'
            ? true // amount can be 0 (e.g. no monthly top-up)
            : value !== '' && value !== -1 && value !== undefined

    const set = (v: any) => setAnswers((a) => ({ ...a, [current.key]: v }))

    const next = () => {
        if (isLast) {
            mutation.mutate(answers)
        } else {
            setStep((s) => s + 1)
        }
    }

    return (
        <SafeAreaView style={styles.safe}>
            <View style={styles.progressTrack}>
                <View style={[styles.progressFill, { width: `${((step + 1) / STEPS.length) * 100}%` }]} />
            </View>

            <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
                <Eyebrow>{`Step ${step + 1} of ${STEPS.length}`}</Eyebrow>
                <H1 style={styles.title}>{current.title}</H1>
                {current.sub ? <Muted style={{ marginBottom: spacing.lg }}>{current.sub}</Muted> : <View style={{ height: spacing.lg }} />}

                {current.kind === 'choice' ? (
                    current.options.map((o) => (
                        <OptionCard
                            key={String(o.id)}
                            title={o.title}
                            description={o.description}
                            selected={value === o.id}
                            onPress={() => set(o.id)}
                        />
                    ))
                ) : (
                    <View style={styles.amountRow}>
                        {current.prefix ? <Text style={styles.amountPrefix}>{current.prefix}</Text> : null}
                        <TextInput
                            style={styles.amountInput}
                            keyboardType="numeric"
                            placeholder={current.placeholder}
                            placeholderTextColor={colors.textFaint}
                            value={value ? String(value) : ''}
                            onChangeText={(txt) => set(Number(txt.replace(/[^0-9]/g, '')) || 0)}
                        />
                    </View>
                )}
            </ScrollView>

            <View style={styles.foot}>
                {step > 0 ? (
                    <Pressable onPress={() => setStep((s) => s - 1)} style={styles.backBtn}>
                        <Text style={styles.backText}>{t('common.back')}</Text>
                    </Pressable>
                ) : <View style={{ flex: 1 }} />}
                <Button
                    label={isLast ? t('common.continue') : t('common.continue')}
                    onPress={next}
                    disabled={!answered}
                    loading={mutation.isPending}
                    style={styles.nextBtn}
                />
            </View>

            {mutation.isError ? <ErrorNote message="Could not save your answers. Try again." /> : null}
        </SafeAreaView>
    )
}

const styles = StyleSheet.create({
    safe: { flex: 1, backgroundColor: colors.ink },
    progressTrack: { height: 3, backgroundColor: colors.panel2, marginHorizontal: spacing.xl, marginTop: spacing.sm, borderRadius: 2 },
    progressFill: { height: 3, backgroundColor: colors.amber, borderRadius: 2 },
    body: { padding: spacing.xl, paddingBottom: spacing.xxl },
    title: { fontSize: fontSize.h2, marginTop: spacing.sm },
    amountRow: {
        flexDirection: 'row', alignItems: 'center',
        backgroundColor: colors.panel, borderColor: colors.border, borderWidth: 1,
        borderRadius: radius.md, paddingHorizontal: spacing.lg,
    },
    amountPrefix: { color: colors.textDim, fontSize: fontSize.h2, fontFamily: fonts.mono, marginRight: spacing.sm },
    amountInput: { flex: 1, color: colors.text, fontSize: fontSize.h2, fontFamily: fonts.mono, height: 64 },
    foot: { flexDirection: 'row', alignItems: 'center', padding: spacing.xl, gap: spacing.md },
    backBtn: { flex: 1, height: 52, alignItems: 'center', justifyContent: 'center' },
    backText: { color: colors.textDim, fontSize: fontSize.base, fontWeight: '600' },
    nextBtn: { flex: 2 },
})
