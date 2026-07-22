import React, { useState } from 'react'
import { View, Text, TextInput, StyleSheet, KeyboardAvoidingView, Platform, Pressable } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { useAuth } from '../auth/AuthContext'
import { useLanguage } from '../i18n/LanguageContext'
import { Eyebrow, H1, Button, ErrorNote, Muted } from '../components/ui'
import { colors, spacing, radius, fontSize, fonts } from '../theme/theme'

export function LoginScreen({ navigation }: any) {
    const { login } = useAuth()
    const { t } = useLanguage()
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [busy, setBusy] = useState(false)

    const onSubmit = async () => {
        setError(null)
        setBusy(true)
        try {
            await login(email.trim(), password)
        } catch {
            setError(t('auth.signInFailed'))
        } finally {
            setBusy(false)
        }
    }

    return (
        <SafeAreaView style={styles.safe}>
            <KeyboardAvoidingView
                behavior={Platform.OS === 'ios' ? 'padding' : undefined}
                style={styles.flex}
            >
                <View style={styles.body}>
                    <Eyebrow>{t('app.name')}</Eyebrow>
                    <H1 style={{ marginTop: spacing.sm }}>{t('auth.welcome')}</H1>
                    <Muted style={{ marginTop: spacing.xs, marginBottom: spacing.xl }}>{t('auth.signInSub')}</Muted>

                    <Field label={t('auth.email')} value={email} onChangeText={setEmail}
                        keyboardType="email-address" autoCapitalize="none" />
                    <Field label={t('auth.password')} value={password} onChangeText={setPassword} secureTextEntry />

                    {error ? <ErrorNote message={error} /> : null}

                    <Button label={t('auth.signIn')} onPress={onSubmit} loading={busy}
                        style={{ marginTop: spacing.lg }} />

                    <Pressable onPress={() => navigation.navigate('Signup')} style={styles.link}>
                        <Text style={styles.linkText}>
                            {t('auth.noAccount')} <Text style={styles.linkAccent}>{t('auth.signUp')}</Text>
                        </Text>
                    </Pressable>
                </View>
            </KeyboardAvoidingView>
        </SafeAreaView>
    )
}

export function Field({ label, ...props }: any) {
    return (
        <View style={styles.field}>
            <Text style={styles.fieldLabel}>{label}</Text>
            <TextInput
                {...props}
                placeholderTextColor={colors.textFaint}
                style={styles.input}
            />
        </View>
    )
}

const styles = StyleSheet.create({
    safe: { flex: 1, backgroundColor: colors.ink },
    flex: { flex: 1 },
    body: { flex: 1, justifyContent: 'center', padding: spacing.xl },
    field: { marginBottom: spacing.md },
    fieldLabel: {
        color: colors.textDim, fontSize: fontSize.xs, letterSpacing: 1,
        textTransform: 'uppercase', marginBottom: spacing.xs, fontFamily: fonts.mono,
    },
    input: {
        backgroundColor: colors.panel,
        borderColor: colors.border,
        borderWidth: 1,
        borderRadius: radius.md,
        paddingHorizontal: spacing.md,
        height: 50,
        color: colors.text,
        fontSize: fontSize.base,
    },
    link: { marginTop: spacing.xl, alignItems: 'center' },
    linkText: { color: colors.textDim, fontSize: fontSize.sm },
    linkAccent: { color: colors.amber, fontWeight: '700' },
})
