import { apiClient } from '@/lib/api-client';

/**
 * Create empty notification data for new users or when API is unavailable
 */
const createEmptyNotificationData = (): Notification[] => [];

const createEmptyNotificationStats = (): NotificationStats => ({
  total: 0,
  unread: 0,
  high: 0,
  sent: 0,
  delivered: 0,
  failed: 0,
  byType: {},
  byCategory: {}
});

const createDefaultNotificationSettings = (): NotificationSettings => ({
  emailEnabled: true,
  smsEnabled: true,
  systemEnabled: true,
  deadlineReminders: true,
  paymentAlerts: true,
  complianceAlerts: true,
  systemUpdates: true,
  marketingEmails: false,
  reminderFrequency: '7days',
  preferences: {
    deadlineReminder: {
      enabled: true,
      daysBefore: [7, 3, 1],
      methods: ['email', 'sms']
    },
    paymentReminder: {
      enabled: true,
      daysBefore: [14, 7, 1],
      methods: ['email', 'sms']
    },
    complianceAlert: {
      enabled: true,
      severity: ['critical', 'warning'],
      methods: ['email', 'sms', 'system']
    }
  }
});

/**
 * Notification data interfaces
 */
export interface Notification {
  id: string;
  type: 'email' | 'sms' | 'system' | 'reminder';
  title: string;
  message: string;
  sender: string;
  recipient: string;
  status: 'sent' | 'delivered' | 'read' | 'failed';
  priority: 'high' | 'medium' | 'low';
  category: 'deadline' | 'payment' | 'compliance' | 'system' | 'general';
  createdAt: string;
  readAt?: string;
  clientId?: string;
  clientName?: string;
  isRead: boolean;
  metadata?: Record<string, any>;
}

export interface NotificationSettings {
  emailEnabled: boolean;
  smsEnabled: boolean;
  systemEnabled: boolean;
  deadlineReminders: boolean;
  paymentAlerts: boolean;
  complianceAlerts: boolean;
  systemUpdates: boolean;
  marketingEmails: boolean;
  reminderFrequency: string;
  preferences: {
    deadlineReminder: {
      enabled: boolean;
      daysBefore: number[];
      methods: ('email' | 'sms' | 'system')[];
    };
    paymentReminder: {
      enabled: boolean;
      daysBefore: number[];
      methods: ('email' | 'sms' | 'system')[];
    };
    complianceAlert: {
      enabled: boolean;
      severity: ('critical' | 'warning' | 'info')[];
      methods: ('email' | 'sms' | 'system')[];
    };
  };
}

export interface NotificationFilters {
  status?: 'sent' | 'delivered' | 'read' | 'failed' | 'unread' | 'all';
  type?: 'email' | 'sms' | 'system' | 'reminder' | 'all';
  category?: 'deadline' | 'payment' | 'compliance' | 'system' | 'general' | 'all';
  priority?: 'high' | 'medium' | 'low' | 'all';
  clientId?: string;
  dateRange?: {
    from: string;
    to: string;
  };
}

export interface NotificationStats {
  total: number;
  unread: number;
  high: number;
  sent: number;
  delivered: number;
  failed: number;
  byType: Record<string, number>;
  byCategory: Record<string, number>;
}

export interface NotificationTemplate {
  id: string;
  name: string;
  type: 'email' | 'sms';
  subject?: string;
  content: string;
  variables: string[];
  category: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateNotificationRequest {
  type: 'email' | 'sms' | 'system';
  recipients: string[];
  title: string;
  message: string;
  priority: 'high' | 'medium' | 'low';
  category: 'deadline' | 'payment' | 'compliance' | 'system' | 'general';
  clientId?: string;
  templateId?: string;
  templateVariables?: Record<string, string>;
  scheduledFor?: string;
}

/**
 * Notification Service for managing notifications and alerts
 */
export const NotificationService = {
  /**
   * Get notifications with optional filtering
   */
  getNotifications: async (filters: NotificationFilters = {}): Promise<Notification[]> => {
    try {
      const params = new URLSearchParams();
      if (filters.status && filters.status !== 'all') params.append('status', filters.status);
      if (filters.type && filters.type !== 'all') params.append('type', filters.type);
      if (filters.category && filters.category !== 'all') params.append('category', filters.category);
      if (filters.priority && filters.priority !== 'all') params.append('priority', filters.priority);
      if (filters.clientId) params.append('clientId', filters.clientId);
      if (filters.dateRange) {
        params.append('fromDate', filters.dateRange.from);
        params.append('toDate', filters.dateRange.to);
      }

      const response = await apiClient.get<{ success: boolean; data: Notification[] }>(
        `/api/notifications?${params.toString()}`
      );
      return response.data.data;
    } catch (error) {
      console.warn('Notifications API unavailable, returning empty data for new user experience:', error);
      return createEmptyNotificationData();
    }
  },

  /**
   * Get notification statistics
   */
  getNotificationStats: async (): Promise<NotificationStats> => {
    try {
      const response = await apiClient.get<{ success: boolean; data: NotificationStats }>(
        '/api/notifications/stats'
      );
      return response.data.data;
    } catch (error) {
      console.warn('Notification stats API unavailable, returning empty data for new user experience:', error);
      return createEmptyNotificationStats();
    }
  },

  /**
   * Mark notification as read
   */
  markAsRead: async (notificationId: string): Promise<void> => {
    await apiClient.put(`/api/notifications/${notificationId}/read`);
  },

  /**
   * Mark notification as unread
   */
  markAsUnread: async (notificationId: string): Promise<void> => {
    await apiClient.put(`/api/notifications/${notificationId}/unread`);
  },

  /**
   * Mark multiple notifications as read
   */
  markMultipleAsRead: async (notificationIds: string[]): Promise<void> => {
    await apiClient.put('/api/notifications/bulk-read', { notificationIds });
  },

  /**
   * Delete notification
   */
  deleteNotification: async (notificationId: string): Promise<void> => {
    await apiClient.delete(`/api/notifications/${notificationId}`);
  },

  /**
   * Delete multiple notifications
   */
  deleteMultiple: async (notificationIds: string[]): Promise<void> => {
    await apiClient.post('/api/notifications/bulk-delete', { notificationIds });
  },

  /**
   * Create new notification
   */
  createNotification: async (notification: CreateNotificationRequest): Promise<Notification> => {
    const response = await apiClient.post<{ success: boolean; data: Notification }>(
      '/api/notifications',
      notification
    );
    return response.data.data;
  },

  /**
   * Get notification settings for current user
   */
  getNotificationSettings: async (): Promise<NotificationSettings> => {
    try {
      const response = await apiClient.get<{ success: boolean; data: NotificationSettings }>(
        '/api/notifications/settings'
      );
      return response.data.data;
    } catch (error) {
      console.warn('Notification settings API unavailable, returning default settings for new user experience:', error);
      return createDefaultNotificationSettings();
    }
  },

  /**
   * Update notification settings
   */
  updateNotificationSettings: async (settings: Partial<NotificationSettings>): Promise<NotificationSettings> => {
    const response = await apiClient.put<{ success: boolean; data: NotificationSettings }>(
      '/api/notifications/settings',
      settings
    );
    return response.data.data;
  },

  /**
   * Get notification templates
   */
  getNotificationTemplates: async (type?: 'email' | 'sms'): Promise<NotificationTemplate[]> => {
    const params = type ? `?type=${type}` : '';
    const response = await apiClient.get<{ success: boolean; data: NotificationTemplate[] }>(
      `/api/notifications/templates${params}`
    );
    return response.data.data;
  },

  /**
   * Create notification template
   */
  createNotificationTemplate: async (template: Omit<NotificationTemplate, 'id' | 'createdAt' | 'updatedAt'>): Promise<NotificationTemplate> => {
    const response = await apiClient.post<{ success: boolean; data: NotificationTemplate }>(
      '/api/notifications/templates',
      template
    );
    return response.data.data;
  },

  /**
   * Update notification template
   */
  updateNotificationTemplate: async (templateId: string, updates: Partial<NotificationTemplate>): Promise<NotificationTemplate> => {
    const response = await apiClient.put<{ success: boolean; data: NotificationTemplate }>(
      `/api/notifications/templates/${templateId}`,
      updates
    );
    return response.data.data;
  },

  /**
   * Delete notification template
   */
  deleteNotificationTemplate: async (templateId: string): Promise<void> => {
    await apiClient.delete(`/api/notifications/templates/${templateId}`);
  },

  /**
   * Send bulk notifications
   */
  sendBulkNotifications: async (notifications: CreateNotificationRequest[]): Promise<{ sent: number; failed: number; errors: string[] }> => {
    const response = await apiClient.post<{ 
      success: boolean; 
      data: { sent: number; failed: number; errors: string[] } 
    }>('/api/notifications/bulk-send', { notifications });
    return response.data.data;
  },

  /**
   * Get notification delivery status
   */
  getDeliveryStatus: async (notificationId: string): Promise<{
    status: string;
    attempts: number;
    lastAttempt?: string;
    errorMessage?: string;
    deliveredAt?: string;
  }> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: {
        status: string;
        attempts: number;
        lastAttempt?: string;
        errorMessage?: string;
        deliveredAt?: string;
      } 
    }>(`/api/notifications/${notificationId}/delivery-status`);
    return response.data.data;
  },

  /**
   * Retry failed notification
   */
  retryNotification: async (notificationId: string): Promise<void> => {
    await apiClient.post(`/api/notifications/${notificationId}/retry`);
  },

  /**
   * Get notification analytics
   */
  getNotificationAnalytics: async (timeRange: '7d' | '30d' | '90d' = '30d'): Promise<{
    deliveryRates: Array<{ date: string; sent: number; delivered: number; failed: number }>;
    typeBreakdown: Record<string, number>;
    categoryBreakdown: Record<string, number>;
    averageDeliveryTime: number;
    totalSent: number;
    deliveryRate: number;
  }> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: {
        deliveryRates: Array<{ date: string; sent: number; delivered: number; failed: number }>;
        typeBreakdown: Record<string, number>;
        categoryBreakdown: Record<string, number>;
        averageDeliveryTime: number;
        totalSent: number;
        deliveryRate: number;
      } 
    }>(`/api/notifications/analytics?timeRange=${timeRange}`);
    return response.data.data;
  }
};