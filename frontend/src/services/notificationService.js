import { apiCall } from './apiConfig';

const NOTIFICATIONS_BASE_URL = '/notifications';

export const notificationService = {
    /**
     * Get paginated notifications for the current user
     * @param {number} page
     * @param {number} pageSize
     */
    getNotifications: (page = 1, pageSize = 20) => {
        return apiCall(`${NOTIFICATIONS_BASE_URL}?page=${page}&pageSize=${pageSize}`, {
            requireAuth: true
        });
    },

    /**
     * Get the count of unread notifications
     */
    getUnreadCount: () => {
        return apiCall(`${NOTIFICATIONS_BASE_URL}/unread-count`, {
            requireAuth: true
        });
    },

    /**
     * Mark a specific notification as read
     * @param {string} notificationId
     */
    markAsRead: (notificationId) => {
        return apiCall(`${NOTIFICATIONS_BASE_URL}/${notificationId}/read`, {
            method: 'PUT',
            requireAuth: true
        });
    },

    /**
     * Mark all user notifications as read
     */
    markAllAsRead: () => {
        return apiCall(`${NOTIFICATIONS_BASE_URL}/read-all`, {
            method: 'PUT',
            requireAuth: true
        });
    }
};
