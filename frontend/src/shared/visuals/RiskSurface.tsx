import './RiskSurface.css'

/**
 * The signature visual: an amber "risk surface" ridgeline rendered as stacked
 * SVG polylines with an opacity ramp (dim back → bright front). Animated purely
 * with CSS keyframes (front line draws on, the whole stack drifts slowly), so
 * the global `prefers-reduced-motion` rule in index.css freezes it to a static
 * frame for free. No WebGL.
 */
const RIDGE =
    '0,170 100,140 200,156 300,116 400,138 500,100 600,128 700,90 800,122 900,104 1000,148 1100,124 1200,162'

export function RiskSurface({ className = '' }: { className?: string }) {
    return (
        <svg
            className={`qw-rs ${className}`.trim()}
            viewBox="0 0 1200 220"
            preserveAspectRatio="none"
            aria-hidden="true"
            focusable="false"
        >
            <defs>
                <polyline id="qw-rs-ridge" points={RIDGE} fill="none" strokeLinejoin="round" strokeLinecap="round" />
            </defs>
            <g className="qw-rs-group">
                <use href="#qw-rs-ridge" transform="translate(0,64)" className="qw-rs-line" style={{ opacity: 0.1 }} />
                <use href="#qw-rs-ridge" transform="translate(0,48)" className="qw-rs-line" style={{ opacity: 0.18 }} />
                <use href="#qw-rs-ridge" transform="translate(0,32)" className="qw-rs-line" style={{ opacity: 0.32 }} />
                <use href="#qw-rs-ridge" transform="translate(0,16)" className="qw-rs-line" style={{ opacity: 0.55 }} />
                <use href="#qw-rs-ridge" transform="translate(0,0)" className="qw-rs-front" />
            </g>
        </svg>
    )
}
