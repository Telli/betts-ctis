import { apiClient } from '@/lib/api-client';

/**
 * Create empty deadline data for new users or when API is unavailable
 */
const createEmptyDeadlineData = (): Deadline[] => [];

const createEmptyDeadlineStats = (): DeadlineStats => ({
  total: 0,
  upcoming: 0,
  dueSoon: 0,
  overdue: 0,
  completed: 0,
  thisWeek: 0,
  thisMonth: 0,
  byType: {},
  byPriority: {}
});

/**
 * Deadline data interfaces
 */
export interface Deadline {
  id: string;
  title: string;
  type: 'tax-filing' | 'payment' | 'compliance' | 'document';
  description: string;
  dueDate: string;
  status: 'upcoming' | 'due-soon' | 'overdue' | 'completed';
  priority: 'high' | 'medium' | 'low';
  category: string;
  clientId?: string;
  clientName?: string;
  amount?: number;
  reminderSet: boolean;
  reminderDates: string[];
  notes?: string;
  completedDate?: string;
  taxYear?: number;
  taxType?: string;
}

export interface DeadlineFilters {
  status?: 'upcoming' | 'due-soon' | 'overdue' | 'completed' | 'all';
  type?: 'tax-filing' | 'payment' | 'compliance' | 'document' | 'all';
  priority?: 'high' | 'medium' | 'low' | 'all';
  clientId?: string;
  dateRange?: {
    from: string;
    to: string;
  };
  category?: string;
}

export interface DeadlineStats {
  total: number;
  upcoming: number;
  dueSoon: number;
  overdue: number;
  completed: number;
  thisWeek: number;
  thisMonth: number;
  byType: Record<string, number>;
  byPriority: Record<string, number>;
}

export interface DeadlineReminder {
  id: string;
  deadlineId: string;
  reminderDate: string;
  method: 'email' | 'sms' | 'system';
  status: 'pending' | 'sent' | 'failed';
  createdAt: string;
  sentAt?: string;
}

export interface CreateDeadlineRequest {
  title: string;
  type: 'tax-filing' | 'payment' | 'compliance' | 'document';
  description: string;
  dueDate: string;
  priority: 'high' | 'medium' | 'low';
  category: string;
  clientId?: string;
  amount?: number;
  taxYear?: number;
  taxType?: string;
  reminderDaysBefore?: number[];
  notes?: string;
}

export interface CalendarEvent {
  id: string;
  title: string;
  date: string;
  type: 'deadline' | 'reminder' | 'filing' | 'payment';
  priority: 'high' | 'medium' | 'low';
  clientName?: string;
  amount?: number;
  status: string;
}

/**
 * Deadline Service for managing tax deadlines and reminders
 */
export const DeadlineService = {
  /**
   * Get deadlines with optional filtering
   */
  getDeadlines: async (filters: DeadlineFilters = {}): Promise<Deadline[]> => {
    try {
      const params = new URLSearchParams();
      if (filters.status && filters.status !== 'all') params.append('status', filters.status);
      if (filters.type && filters.type !== 'all') params.append('type', filters.type);
      if (filters.priority && filters.priority !== 'all') params.append('priority', filters.priority);
      if (filters.clientId) params.append('clientId', filters.clientId);
      if (filters.category) params.append('category', filters.category);
      if (filters.dateRange) {
        params.append('fromDate', filters.dateRange.from);
        params.append('toDate', filters.dateRange.to);
      }

      const response = await apiClient.get<{ success: boolean; data: Deadline[] }>(
        `/api/deadlines?${params.toString()}`
      );
      return response.data.data;
    } catch (error) {
      console.warn('Deadlines API unavailable, returning empty data for new user experience:', error);
      return createEmptyDeadlineData();
    }
  },

  /**
   * Get deadline statistics
   */
  getDeadlineStats: async (clientId?: string): Promise<DeadlineStats> => {
    try {
      const url = clientId 
        ? `/api/deadlines/stats?clientId=${clientId}`
        : '/api/deadlines/stats';
      
      const response = await apiClient.get<{ success: boolean; data: DeadlineStats }>(url);
      return response.data.data;
    } catch (error) {
      console.warn('Deadline stats API unavailable, returning empty data for new user experience:', error);
      return createEmptyDeadlineStats();
    }
  },

  /**
   * Get upcoming deadlines (next 30 days by default)
   */
  getUpcomingDeadlines: async (days: number = 30, clientId?: string): Promise<Deadline[]> => {
    const params = new URLSearchParams();
    params.append('days', days.toString());
    if (clientId) params.append('clientId', clientId);

    const response = await apiClient.get<{ success: boolean; data: Deadline[] }>(
      `/api/deadlines/upcoming?${params.toString()}`
    );
    return response.data.data;
  },

  /**
   * Get overdue deadlines
   */
  getOverdueDeadlines: async (clientId?: string): Promise<Deadline[]> => {
    const url = clientId 
      ? `/api/deadlines/overdue?clientId=${clientId}`
      : '/api/deadlines/overdue';
    
    const response = await apiClient.get<{ success: boolean; data: Deadline[] }>(url);
    return response.data.data;
  },

  /**
   * Get calendar events for a specific month
   */
  getCalendarEvents: async (year: number, month: number, clientId?: string): Promise<CalendarEvent[]> => {
    const params = new URLSearchParams();
    params.append('year', year.toString());
    params.append('month', month.toString());
    if (clientId) params.append('clientId', clientId);

    const response = await apiClient.get<{ success: boolean; data: CalendarEvent[] }>(
      `/api/deadlines/calendar?${params.toString()}`
    );
    return response.data.data;
  },

  /**
   * Create new deadline
   */
  createDeadline: async (deadline: CreateDeadlineRequest): Promise<Deadline> => {
    const response = await apiClient.post<{ success: boolean; data: Deadline }>(
      '/api/deadlines',
      deadline
    );
    return response.data.data;
  },

  /**
   * Update deadline
   */
  updateDeadline: async (deadlineId: string, updates: Partial<Deadline>): Promise<Deadline> => {
    const response = await apiClient.put<{ success: boolean; data: Deadline }>(
      `/api/deadlines/${deadlineId}`,
      updates
    );
    return response.data.data;
  },

  /**
   * Mark deadline as completed
   */
  markAsCompleted: async (deadlineId: string, notes?: string): Promise<Deadline> => {
    const response = await apiClient.put<{ success: boolean; data: Deadline }>(
      `/api/deadlines/${deadlineId}/complete`,
      { notes }
    );
    return response.data.data;
  },

  /**
   * Delete deadline
   */
  deleteDeadline: async (deadlineId: string): Promise<void> => {
    await apiClient.delete(`/api/deadlines/${deadlineId}`);
  },

  /**
   * Set reminder for deadline
   */
  setReminder: async (
    deadlineId: string, 
    daysBefore: number[], 
    methods: ('email' | 'sms' | 'system')[]
  ): Promise<DeadlineReminder[]> => {
    const response = await apiClient.post<{ success: boolean; data: DeadlineReminder[] }>(
      `/api/deadlines/${deadlineId}/reminders`,
      { daysBefore, methods }
    );
    return response.data.data;
  },

  /**
   * Get deadline reminders
   */
  getDeadlineReminders: async (deadlineId: string): Promise<DeadlineReminder[]> => {
    const response = await apiClient.get<{ success: boolean; data: DeadlineReminder[] }>(
      `/api/deadlines/${deadlineId}/reminders`
    );
    return response.data.data;
  },

  /**
   * Update reminder settings
   */
  updateReminder: async (reminderId: string, updates: Partial<DeadlineReminder>): Promise<DeadlineReminder> => {
    const response = await apiClient.put<{ success: boolean; data: DeadlineReminder }>(
      `/api/deadlines/reminders/${reminderId}`,
      updates
    );
    return response.data.data;
  },

  /**
   * Delete reminder
   */
  deleteReminder: async (reminderId: string): Promise<void> => {
    await apiClient.delete(`/api/deadlines/reminders/${reminderId}`);
  },

  /**
   * Get Sierra Leone tax calendar for a year
   */
  getSierraLeoneTaxCalendar: async (year: number): Promise<Array<{
    month: string;
    deadlines: Array<{
      date: string;
      title: string;
      description: string;
      taxType: string;
      clientCategory: string[];
      financeActReference: string;
    }>;
  }>> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: Array<{
        month: string;
        deadlines: Array<{
          date: string;
          title: string;
          description: string;
          taxType: string;
          clientCategory: string[];
          financeActReference: string;
        }>;
      }> 
    }>(`/api/deadlines/sierra-leone-calendar?year=${year}`);
    return response.data.data;
  },

  /**
   * Generate automatic deadlines for a client
   */
  generateClientDeadlines: async (
    clientId: string, 
    taxYear: number, 
    clientCategory: 'large' | 'medium' | 'small' | 'micro'
  ): Promise<Deadline[]> => {
    const response = await apiClient.post<{ success: boolean; data: Deadline[] }>(
      '/api/deadlines/generate',
      { clientId, taxYear, clientCategory }
    );
    return response.data.data;
  },

  /**
   * Get deadline analytics
   */
  getDeadlineAnalytics: async (timeRange: '3m' | '6m' | '12m' = '12m'): Promise<{
    completionRate: number;
    averageDaysToComplete: number;
    overdueRate: number;
    onTimeCompletionTrend: Array<{ month: string; onTime: number; overdue: number }>;
    deadlinesByType: Record<string, number>;
    clientPerformance: Array<{ clientName: string; completionRate: number; overdueCount: number }>;
  }> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: {
        completionRate: number;
        averageDaysToComplete: number;
        overdueRate: number;
        onTimeCompletionTrend: Array<{ month: string; onTime: number; overdue: number }>;
        deadlinesByType: Record<string, number>;
        clientPerformance: Array<{ clientName: string; completionRate: number; overdueCount: number }>;
      } 
    }>(`/api/deadlines/analytics?timeRange=${timeRange}`);
    return response.data.data;
  },

  /**
   * Export deadlines
   */
  exportDeadlines: async (
    format: 'excel' | 'csv' | 'pdf' | 'ical',
    filters: DeadlineFilters = {}
  ): Promise<Blob> => {
    const params = new URLSearchParams();
    params.append('format', format);
    if (filters.status && filters.status !== 'all') params.append('status', filters.status);
    if (filters.type && filters.type !== 'all') params.append('type', filters.type);
    if (filters.priority && filters.priority !== 'all') params.append('priority', filters.priority);
    if (filters.clientId) params.append('clientId', filters.clientId);
    if (filters.dateRange) {
      params.append('fromDate', filters.dateRange.from);
      params.append('toDate', filters.dateRange.to);
    }

    const response = await apiClient.get(`/api/deadlines/export?${params.toString()}`);
    return response.data as Blob;
  }
};