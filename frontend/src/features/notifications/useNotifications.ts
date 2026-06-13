import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { notificationService } from '@/services/notificationService'
import type { AppNotification } from '@/types/api'

const POLL_MS = 30_000

/**
 * Notifications via TanStack Query (polling). The list + unread count share the
 * query cache, so the AppShell bell and the Dashboard "Recent Activity" stay in
 * sync and mark-as-read invalidates both. Surface is unchanged from Phase 1.
 */
export function useNotifications() {
    const qc = useQueryClient()

    const listQuery = useQuery({
        queryKey: ['notifications', 'list'],
        queryFn: async (): Promise<AppNotification[]> => {
            const res = await notificationService.getNotifications(1, 10)
            return Array.isArray(res) ? (res as AppNotification[]) : []
        },
        refetchInterval: POLL_MS,
    })

    const countQuery = useQuery({
        queryKey: ['notifications', 'unread'],
        queryFn: async (): Promise<number> => {
            const res = await notificationService.getUnreadCount()
            return typeof res === 'number' ? res : 0
        },
        refetchInterval: POLL_MS,
    })

    const invalidate = () => qc.invalidateQueries({ queryKey: ['notifications'] })

    const markAsReadMutation = useMutation({
        mutationFn: (id: string) => notificationService.markAsRead(id),
        onSuccess: invalidate,
    })

    const markAllAsReadMutation = useMutation({
        mutationFn: () => notificationService.markAllAsRead(),
        onSuccess: invalidate,
    })

    return {
        notifications: listQuery.data ?? [],
        unreadCount: countQuery.data ?? 0,
        markAsRead: (id: string) => markAsReadMutation.mutate(id),
        markAllAsRead: () => markAllAsReadMutation.mutate(),
        refetch: () => {
            listQuery.refetch()
            countQuery.refetch()
        },
    }
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
