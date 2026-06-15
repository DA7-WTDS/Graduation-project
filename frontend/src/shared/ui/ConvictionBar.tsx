import type { SignalAction } from '@/types/api'
import './primitives.css'

const toneColor: Record<SignalAction, string> = {
    BUY: 'var(--qw-buy)',
    SELL: 'var(--qw-sell)',
    HOLD: 'var(--qw-hold)',
}

/** Horizontal 0..1 conviction meter; tinted to the pick's signal when given. */
export function ConvictionBar({ value, signal }: { value: number; signal?: SignalAction }) {
    const pct = Math.max(0, Math.min(1, value)) * 100
    const color = signal ? toneColor[signal] : 'var(--qw-amber)'
    return (
        <span className="qw-bar" role="meter" aria-valuemin={0} aria-valuemax={1} aria-valuenow={value}>
            <span className="qw-bar-fill" style={{ width: `${pct}%`, background: color }} />
        </span>
    )
}
