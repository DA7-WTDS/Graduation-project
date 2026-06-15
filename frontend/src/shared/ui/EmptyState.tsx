import type { ReactNode } from 'react'
import './primitives.css'

export function EmptyState({
    title,
    hint,
    icon,
}: {
    title: string
    hint?: string
    icon?: ReactNode
}) {
    return (
        <div className="qw-empty">
            {icon}
            <div className="qw-empty-title">{title}</div>
            {hint && <div className="qw-empty-hint">{hint}</div>}
        </div>
    )
}
