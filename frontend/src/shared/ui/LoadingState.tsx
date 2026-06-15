import './primitives.css'

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
    return (
        <div className="qw-state">
            <span className="qw-spinner" aria-hidden="true" />
            <span>{label}</span>
        </div>
    )
}
