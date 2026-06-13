import type { HTMLAttributes } from 'react'
import './primitives.css'

/** Raised Quant Terminal panel. */
export function Card({ className = '', ...rest }: HTMLAttributes<HTMLDivElement>) {
    return <div className={`qw-card ${className}`.trim()} {...rest} />
}
