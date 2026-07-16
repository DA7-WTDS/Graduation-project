import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { STRINGS, type Lang } from './strings'

const STORAGE_KEY = 'qw_lang'

interface LanguageValue {
    lang: Lang
    isRtl: boolean
    setLang: (lang: Lang) => void
    toggle: () => void
    /** Translate a key. Falls back to English, then to the key itself, so a
     * missing string is never a blank screen. */
    t: (key: string) => string
}

const LanguageContext = createContext<LanguageValue | null>(null)

const readStored = (): Lang => (localStorage.getItem(STORAGE_KEY) === 'ar' ? 'ar' : 'en')

export function LanguageProvider({ children }: { children: ReactNode }) {
    const [lang, setLang] = useState<Lang>(readStored)

    // The document itself carries the language: `dir` drives RTL layout for the
    // whole tree (flex/grid mirror automatically), and `lang` lets the browser
    // pick correct fonts and hyphenation for Arabic.
    useEffect(() => {
        localStorage.setItem(STORAGE_KEY, lang)
        document.documentElement.lang = lang
        document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr'
    }, [lang])

    const t = useCallback(
        (key: string) => STRINGS[lang][key] ?? STRINGS.en[key] ?? key,
        [lang],
    )

    const value = useMemo<LanguageValue>(
        () => ({
            lang,
            isRtl: lang === 'ar',
            setLang,
            toggle: () => setLang((l) => (l === 'ar' ? 'en' : 'ar')),
            t,
        }),
        [lang, t],
    )

    return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}

export function useLanguage(): LanguageValue {
    const ctx = useContext(LanguageContext)
    if (!ctx) {
        throw new Error('useLanguage must be used within LanguageProvider')
    }
    return ctx
}
