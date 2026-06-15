import type { ReactNode } from 'react'
import './primitives.css'

/** Small metric tile: mono label + figure. */
export function StatTile({
    label,
    value,
    valueColor,
}: {
    label: string
    value: ReactNode
    valueColor?: string
}) {
    return (
        <div className="qw-tile">
            <div className="qw-tile-label">{label}</div>
            <div className="qw-tile-value" style={valueColor ? { color: valueColor } : undefined}>
                {value}
            </div>
        </div>
    )
}
