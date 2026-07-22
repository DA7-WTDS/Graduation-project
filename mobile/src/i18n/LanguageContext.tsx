import React, { createContext, useContext, useState, useCallback, useEffect } from 'react'
import { I18nManager, Platform } from 'react-native'
import { STRINGS, type Lang } from './strings'

interface LanguageValue {
    lang: Lang
    t: (key: string) => string
    toggle: () => void
    isRTL: boolean
}

const LanguageContext = createContext<LanguageValue | null>(null)

export function LanguageProvider({ children }: { children: React.ReactNode }) {
    const [lang, setLang] = useState<Lang>('en')

    // On web we can flip layout direction live; native RTL (I18nManager.forceRTL)
    // needs an app reload to fully re-mirror, so we set it best-effort and mirror
    // what we can immediately.
    useEffect(() => {
        const rtl = lang === 'ar'
        if (Platform.OS === 'web' && typeof document !== 'undefined') {
            document.documentElement.dir = rtl ? 'rtl' : 'ltr'
            document.documentElement.lang = lang
        } else if (I18nManager.isRTL !== rtl) {
            I18nManager.allowRTL(rtl)
            I18nManager.forceRTL(rtl)
        }
    }, [lang])

    const t = useCallback(
        (key: string) => STRINGS[lang][key] ?? STRINGS.en[key] ?? key,
        [lang],
    )

    const toggle = useCallback(() => setLang((l) => (l === 'en' ? 'ar' : 'en')), [])

    return (
        <LanguageContext.Provider value={{ lang, t, toggle, isRTL: lang === 'ar' }}>
            {children}
        </LanguageContext.Provider>
    )
}

export function useLanguage(): LanguageValue {
    const ctx = useContext(LanguageContext)
    if (!ctx) {
        throw new Error('useLanguage must be used within LanguageProvider')
    }
    return ctx
}
