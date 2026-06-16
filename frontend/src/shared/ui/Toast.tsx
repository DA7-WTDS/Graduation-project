import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'
import { CheckCircle2, AlertCircle, Info, X } from 'lucide-react'
import './Toast.css'

export type ToastVariant = 'success' | 'error' | 'info'

interface ToastItem {
    id: number
    message: string
    variant: ToastVariant
}

interface ToastApi {
    show: (message: string, variant?: ToastVariant) => void
    success: (message: string) => void
    error: (message: string) => void
    info: (message: string) => void
}

const ToastContext = createContext<ToastApi | null>(null)

const DISMISS_MS = 4000

const icons: Record<ToastVariant, typeof Info> = {
    success: CheckCircle2,
    error: AlertCircle,
    info: Info,
}

/**
 * App-wide toast notifications. Wrap the app once; call `useToast()` anywhere to
 * surface short, auto-dismissing feedback for user actions.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<ToastItem[]>([])
    const idRef = useRef(0)

    const remove = useCallback((id: number) => {
        setToasts((list) => list.filter((t) => t.id !== id))
    }, [])

    const show = useCallback(
        (message: string, variant: ToastVariant = 'info') => {
            const id = ++idRef.current
            setToasts((list) => [...list, { id, message, variant }])
            window.setTimeout(() => remove(id), DISMISS_MS)
        },
        [remove]
    )

    const api = useMemo<ToastApi>(
        () => ({
            show,
            success: (m) => show(m, 'success'),
            error: (m) => show(m, 'error'),
            info: (m) => show(m, 'info'),
        }),
        [show]
    )

    return (
        <ToastContext.Provider value={api}>
            {children}
            <div className="qw-toast-viewport" role="region" aria-label="Notifications" aria-live="polite">
                {toasts.map((t) => {
                    const Icon = icons[t.variant]
                    return (
                        <div key={t.id} className={`qw-toast qw-toast-${t.variant}`} role="status">
                            <Icon size={17} strokeWidth={2} className="qw-toast-icon" aria-hidden="true" />
                            <span className="qw-toast-msg">{t.message}</span>
                            <button
                                type="button"
                                className="qw-toast-close"
                                aria-label="Dismiss"
                                onClick={() => remove(t.id)}
                            >
                                <X size={14} strokeWidth={2} aria-hidden="true" />
                            </button>
                        </div>
                    )
                })}
            </div>
        </ToastContext.Provider>
    )
}

export function useToast(): ToastApi {
    const ctx = useContext(ToastContext)
    if (!ctx) throw new Error('useToast must be used within ToastProvider')
    return ctx
}
