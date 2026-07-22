import React from 'react'
import {
    View, Text, Pressable, ActivityIndicator, StyleSheet,
    type ViewStyle, type TextStyle, type StyleProp,
} from 'react-native'
import { colors, radius, spacing, fontSize, fonts } from '../theme/theme'

export function Card({ children, style }: { children: React.ReactNode; style?: StyleProp<ViewStyle> }) {
    return <View style={[styles.card, style]}>{children}</View>
}

export function Eyebrow({ children }: { children: React.ReactNode }) {
    return <Text style={styles.eyebrow}>{children}</Text>
}

export function H1({ children, style }: { children: React.ReactNode; style?: StyleProp<TextStyle> }) {
    return <Text style={[styles.h1, style]}>{children}</Text>
}

export function Label({ children }: { children: React.ReactNode }) {
    return <Text style={styles.label}>{children}</Text>
}

export function Muted({ children, style }: { children: React.ReactNode; style?: StyleProp<TextStyle> }) {
    return <Text style={[styles.muted, style]}>{children}</Text>
}

interface ButtonProps {
    label: string
    onPress: () => void
    variant?: 'primary' | 'secondary' | 'ghost'
    disabled?: boolean
    loading?: boolean
    style?: StyleProp<ViewStyle>
}

export function Button({ label, onPress, variant = 'primary', disabled, loading, style }: ButtonProps) {
    const isPrimary = variant === 'primary'
    return (
        <Pressable
            onPress={onPress}
            disabled={disabled || loading}
            style={({ pressed }) => [
                styles.btn,
                isPrimary ? styles.btnPrimary : variant === 'secondary' ? styles.btnSecondary : styles.btnGhost,
                (disabled || loading) && styles.btnDisabled,
                pressed && !disabled && styles.btnPressed,
                style,
            ]}
        >
            {loading ? (
                <ActivityIndicator color={isPrimary ? colors.ink : colors.amber} />
            ) : (
                <Text style={[styles.btnText, isPrimary ? styles.btnTextPrimary : styles.btnTextSecondary]}>
                    {label}
                </Text>
            )}
        </Pressable>
    )
}

export function StatTile({ label, value, valueColor }: { label: string; value: string; valueColor?: string }) {
    return (
        <View style={styles.tile}>
            <Text style={styles.tileLabel}>{label}</Text>
            <Text style={[styles.tileValue, valueColor ? { color: valueColor } : null]}>{value}</Text>
        </View>
    )
}

interface OptionCardProps {
    title: string
    description?: string
    selected: boolean
    onPress: () => void
}

export function OptionCard({ title, description, selected, onPress }: OptionCardProps) {
    return (
        <Pressable
            onPress={onPress}
            style={({ pressed }) => [
                styles.option,
                selected && styles.optionSelected,
                pressed && styles.btnPressed,
            ]}
        >
            <View style={styles.optionRadio}>
                {selected ? <View style={styles.optionDot} /> : null}
            </View>
            <View style={styles.optionBody}>
                <Text style={[styles.optionTitle, selected && { color: colors.text }]}>{title}</Text>
                {description ? <Text style={styles.optionDesc}>{description}</Text> : null}
            </View>
        </Pressable>
    )
}

export function LangToggle({ label, onPress }: { label: string; onPress: () => void }) {
    return (
        <Pressable
            onPress={onPress}
            accessibilityRole="button"
            hitSlop={8}
            style={({ pressed }) => [styles.langToggle, pressed && styles.btnPressed]}
        >
            <Text style={styles.langToggleText}>{label}</Text>
        </Pressable>
    )
}

export function Loading({ label }: { label?: string }) {
    return (
        <View style={styles.center}>
            <ActivityIndicator color={colors.amber} size="large" />
            {label ? <Text style={[styles.muted, { marginTop: spacing.md }]}>{label}</Text> : null}
        </View>
    )
}

export function ErrorNote({ message }: { message: string }) {
    return <Text style={styles.error}>{message}</Text>
}

const styles = StyleSheet.create({
    card: {
        backgroundColor: colors.panel,
        borderColor: colors.border,
        borderWidth: 1,
        borderRadius: radius.lg,
        padding: spacing.lg,
    },
    eyebrow: {
        color: colors.amber,
        fontSize: fontSize.xs,
        letterSpacing: 2,
        textTransform: 'uppercase',
        fontFamily: fonts.mono,
        marginBottom: spacing.xs,
    },
    h1: { color: colors.text, fontSize: fontSize.h1, fontWeight: '700' },
    label: {
        color: colors.textDim,
        fontSize: fontSize.xs,
        letterSpacing: 1.5,
        textTransform: 'uppercase',
        fontFamily: fonts.mono,
    },
    muted: { color: colors.textDim, fontSize: fontSize.base },
    btn: {
        height: 52,
        borderRadius: radius.md,
        alignItems: 'center',
        justifyContent: 'center',
        flexDirection: 'row',
        paddingHorizontal: spacing.xl,
    },
    btnPrimary: { backgroundColor: colors.amber },
    btnSecondary: { backgroundColor: 'transparent', borderWidth: 1, borderColor: colors.borderStrong },
    btnGhost: { backgroundColor: 'transparent' },
    btnDisabled: { opacity: 0.5 },
    btnPressed: { opacity: 0.85 },
    btnText: { fontSize: fontSize.base, fontWeight: '700' },
    btnTextPrimary: { color: colors.ink },
    btnTextSecondary: { color: colors.text },
    tile: {
        flexGrow: 1,
        flexBasis: '45%',
        backgroundColor: colors.panel2,
        borderColor: colors.border,
        borderWidth: 1,
        borderRadius: radius.md,
        padding: spacing.md,
    },
    tileLabel: {
        color: colors.textDim,
        fontSize: fontSize.xs,
        letterSpacing: 1,
        textTransform: 'uppercase',
        marginBottom: spacing.xs,
    },
    tileValue: { color: colors.text, fontSize: fontSize.fig, fontFamily: fonts.mono, fontWeight: '600' },
    center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: spacing.xl },
    error: { color: colors.sell, fontSize: fontSize.sm, marginTop: spacing.sm },
    option: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: spacing.md,
        backgroundColor: colors.panel,
        borderColor: colors.border,
        borderWidth: 1,
        borderRadius: radius.md,
        padding: spacing.lg,
        marginBottom: spacing.md,
    },
    optionSelected: { borderColor: colors.amber, backgroundColor: colors.amberGlow },
    optionRadio: {
        width: 22, height: 22, borderRadius: 11,
        borderWidth: 2, borderColor: colors.borderStrong,
        alignItems: 'center', justifyContent: 'center',
    },
    optionDot: { width: 10, height: 10, borderRadius: 5, backgroundColor: colors.amber },
    optionBody: { flex: 1 },
    optionTitle: { color: colors.textDim, fontSize: fontSize.base, fontWeight: '600' },
    optionDesc: { color: colors.textFaint, fontSize: fontSize.sm, marginTop: 2 },
    langToggle: {
        borderColor: colors.borderStrong, borderWidth: 1, borderRadius: radius.pill,
        paddingHorizontal: spacing.md, paddingVertical: spacing.xs,
    },
    langToggleText: { color: colors.amber, fontSize: fontSize.sm, fontWeight: '700', fontFamily: fonts.mono },
})
