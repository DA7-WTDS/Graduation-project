import { useEffect, useRef, useState } from 'react'
import { useNotifications, formatRelativeTime } from './useNotifications'
import './NotificationBell.css'

export function NotificationBell() {
    const { notifications, unreadCount, markAsRead, markAllAsRead } = useNotifications()
    const [open, setOpen] = useState(false)
    const ref = useRef<HTMLDivElement>(null)

    useEffect(() => {
        const onMouseDown = (e: MouseEvent) => {
            if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
        }
        document.addEventListener('mousedown', onMouseDown)
        return () => document.removeEventListener('mousedown', onMouseDown)
    }, [])

    return (
        <div className="qw-bell" ref={ref}>
            <button
                type="button"
                className="qw-bell-btn"
                aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
                onClick={() => setOpen((o) => !o)}
            >
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
                    <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
                    <path d="M13.7 21a2 2 0 0 1-3.4 0" />
                </svg>
                {unreadCount > 0 && <span className="qw-bell-badge">{unreadCount > 9 ? '9+' : unreadCount}</span>}
            </button>

            {open && (
                <div className="qw-bell-menu" role="menu">
                    <div className="qw-bell-head">
                        <span>Notifications</span>
                        {unreadCount > 0 && (
                            <button type="button" className="qw-bell-mark" onClick={markAllAsRead}>
                                Mark all read
                            </button>
                        )}
                    </div>
                    <div className="qw-bell-list">
                        {notifications.length > 0 ? (
                            notifications.map((n) => (
                                <button
                                    type="button"
                                    key={n.id}
                                    className={`qw-bell-item${n.isRead ? '' : ' unread'}`}
                                    onClick={() => !n.isRead && markAsRead(n.id)}
                                >
                                    <span className="qw-bell-dot" aria-hidden="true" />
                                    <span className="qw-bell-body">
                                        <span className="qw-bell-title">{n.title}</span>
                                        <span className="qw-bell-msg">{n.message}</span>
                                        <span className="qw-bell-time">{formatRelativeTime(n.createdAt)}</span>
                                    </span>
                                </button>
                            ))
                        ) : (
                            <div className="qw-bell-empty">No notifications yet</div>
                        )}
                    </div>
                </div>
            )}
        </div>
    )
}
