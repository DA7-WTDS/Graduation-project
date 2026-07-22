import React, { useState } from 'react'
import { View, Text, StyleSheet, KeyboardAvoidingView, Platform, Pressable, ScrollView } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { useAuth } from '../auth/AuthContext'
import { useLanguage } from '../i18n/LanguageContext'
import { Eyebrow, H1, Button, ErrorNote, Muted } from '../components/ui'
import { Field } from './LoginScreen'
import { colors, spacing, fontSize } from '../theme/theme'

export function SignupScreen({ navigation }: any) {
    const { register } = useAuth()
    const { t } = useLanguage()
    const [firstName, setFirstName] = useState('')
    const [lastName, setLastName] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [busy, setBusy] = useState(false)

    const onSubmit = async () => {
        setError(null)
        setBusy(true)
        try {
            await register(firstName.trim(), lastName.trim(), email.trim(), password)
        } catch {
            setError(t('auth.signUpFailed'))
        } finally {
            setBusy(false)
        }
    }

    return (
        <SafeAreaView style={styles.safe}>
            <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={styles.flex}>
                <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
                    <Eyebrow>{t('app.name')}</Eyebrow>
                    <H1 style={{ marginTop: spacing.sm }}>{t('auth.createTitle')}</H1>
                    <Muted style={{ marginTop: spacing.xs, marginBottom: spacing.xl }}>{t('auth.createSub')}</Muted>

                    <Field label={t('auth.firstName')} value={firstName} onChangeText={setFirstName} />
                    <Field label={t('auth.lastName')} value={lastName} onChangeText={setLastName} />
                    <Field label={t('auth.email')} value={email} onChangeText={setEmail}
                        keyboardType="email-address" autoCapitalize="none" />
                    <Field label={t('auth.password')} value={password} onChangeText={setPassword} secureTextEntry />

                    {error ? <ErrorNote message={error} /> : null}

                    <Button label={t('auth.signUp')} onPress={onSubmit} loading={busy} style={{ marginTop: spacing.lg }} />

                    <Pressable onPress={() => navigation.navigate('Login')} style={styles.link}>
                        <Text style={styles.linkText}>
                            {t('auth.haveAccount')} <Text style={styles.linkAccent}>{t('auth.signIn')}</Text>
                        </Text>
                    </Pressable>
                </ScrollView>
            </KeyboardAvoidingView>
        </SafeAreaView>
    )
}

const styles = StyleSheet.create({
    safe: { flex: 1, backgroundColor: colors.ink },
    flex: { flex: 1 },
    body: { flexGrow: 1, justifyContent: 'center', padding: spacing.xl },
    link: { marginTop: spacing.xl, alignItems: 'center' },
    linkText: { color: colors.textDim, fontSize: fontSize.sm },
    linkAccent: { color: colors.amber, fontWeight: '700' },
})
