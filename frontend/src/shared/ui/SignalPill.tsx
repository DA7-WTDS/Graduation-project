import type { SignalAction } from '@/types/api'
import './primitives.css'

/** BUY / SELL / HOLD pill, signal-colored. */
export function SignalPill({ action }: { action: SignalAction }) {
    return (
        <span className="qw-pill" data-signal={action}>
            {action}
        </span>
    )
}
