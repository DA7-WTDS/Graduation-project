import { RiskSurface } from './RiskSurface'
import './Backdrop.css'

/**
 * Layered atmospheric backdrop: faint plotting grid + one soft radial amber glow,
 * with an optional ambient RiskSurface ridge along the bottom. Sits behind content
 * (pointer-events: none). Pass `fixed` to pin it to the viewport (app shell);
 * leave it absolute to fill a positioned parent (e.g. a hero section).
 */
export function Backdrop({
    fixed = false,
    surface = false,
    className = '',
}: {
    fixed?: boolean
    surface?: boolean
    className?: string
}) {
    return (
        <div className={`qw-backdrop${fixed ? ' qw-backdrop--fixed' : ''} ${className}`.trim()} aria-hidden="true">
            <div className="qw-backdrop-grid" />
            <div className="qw-backdrop-glow" />
            {surface && (
                <div className="qw-backdrop-surface">
                    <RiskSurface />
                </div>
            )}
        </div>
    )
}
