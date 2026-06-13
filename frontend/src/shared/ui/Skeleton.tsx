import './primitives.css'

/** Shimmering placeholder block. Freezes to a static fill under prefers-reduced-motion. */
export function Skeleton({
    width,
    height = 12,
    radius,
}: {
    width?: string | number
    height?: string | number
    radius?: string
}) {
    return <span className="qw-skeleton" style={{ width, height, borderRadius: radius }} />
}
