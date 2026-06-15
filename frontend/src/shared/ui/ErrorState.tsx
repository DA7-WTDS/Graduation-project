import { Button } from './Button'
import './primitives.css'

export function ErrorState({
    message = 'Something went wrong.',
    onRetry,
}: {
    message?: string
    onRetry?: () => void
}) {
    return (
        <div className="qw-error" role="alert">
            <div className="qw-error-icon" aria-hidden="true">!</div>
            <div className="qw-error-msg">{message}</div>
            {onRetry && (
                <Button variant="secondary" className="qw-state-retry" onClick={onRetry}>
                    Try again
                </Button>
            )}
        </div>
    )
}
