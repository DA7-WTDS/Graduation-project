import type { ButtonHTMLAttributes } from 'react'
import './primitives.css'

type Variant = 'primary' | 'secondary' | 'ghost'

export function Button({
    variant = 'secondary',
    className = '',
    ...rest
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant }) {
    return <button className={`qw-btn qw-btn-${variant} ${className}`.trim()} {...rest} />
}
