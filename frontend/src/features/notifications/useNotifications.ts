import { useCallback, useEffect, useState } from 'react'
import { notificationService } from '@/services/notificationService'
import type { AppNotification } from '@/types/api'

const POLL_MS = 30_000

/**
 * Phase 1 notifications hook (plain fetch + polling).
 * Phase 2 will swap the internals to TanStack Query without changing this surface.
 */
export function useNotifications() {
    const [notifications, setNotifications] = useState<AppNotification[]>([])
    const [unreadCount, setUnreadCount] = useState(0)

    const refetch = useCallback(async () => {
        try {
            const [list, count] = await Promise.all([
                notificationService.getNotifications(1, 10),
                notificationService.getUnreadCount(),
            ])
            if (Array.isArray(list)) setNotifications(list as AppNotification[])
            if (typeof count === 'number') setUnreadCount(count)
        } catch {
            /* transient — keep last good state */
        }
    }, [])

    useEffect(() => {
        refetch()
        const id = window.setInterval(refetch, POLL_MS)
        return () => window.clearInterval(id)
    }, [refetch])

    const markAsRead = useCallback(async (id: string) => {
        try {
            await notificationService.markAsRead(id)
        } catch {
            return
        }
        setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)))
        setUnreadCount((c) => Math.max(0, c - 1))
    }, [])

    const markAllAsRead = useCallback(async () => {
        try {
            await notificationService.markAllAsRead()
        } catch {
            return
        }
        setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
        setUnreadCount(0)
    }, [])

    return { notifications, unreadCount, markAsRead, markAllAsRead, refetch }
}

/** Relative-time formatter shared by notification surfaces. */
export function formatRelativeTime(dateString: string): string {
    const date = new Date(dateString)
    const diff = Math.floor((Date.now() - date.getTime()) / 1000)
    if (diff < 60) return 'Just now'
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`
    return date.toLocaleDateString()
}
