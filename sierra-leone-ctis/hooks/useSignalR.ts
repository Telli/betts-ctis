/**
 * React Hook for SignalR Integration
 * Makes it easy to use SignalR in components
 */

import { useEffect, useState, useCallback, useRef } from 'react';
import { signalRService, ChatMessage, Notification } from '@/lib/signalr-client';
import { useAuth } from '@/context/auth-context';
import { useToast } from './use-toast';

/**
 * Hook for managing SignalR chat connection
 */
export function useChat(conversationId?: number) {
  const [isConnected, setIsConnected] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isTyping, setIsTyping] = useState<Record<string, boolean>>({});
  const { isLoggedIn } = useAuth();
  const { toast } = useToast();

  // Initialize chat connection
  useEffect(() => {
    if (!isLoggedIn) return;

    const initializeChat = async () => {
      try {
        await signalRService.initializeChatHub();
        setIsConnected(true);

        // Join conversation if ID provided
        if (conversationId) {
          await signalRService.joinConversation(conversationId);
        }
      } catch (error: any) {
        console.error('Failed to initialize chat:', error);
        toast({
          title: 'Chat Connection Error',
          description: error.message || 'Failed to connect to chat server',
          variant: 'destructive',
        });
      }
    };

    initializeChat();

    // Cleanup on unmount
    return () => {
      if (conversationId) {
        signalRService.leaveConversation(conversationId);
      }
    };
  }, [isLoggedIn, conversationId, toast]);

  // Subscribe to messages
  useEffect(() => {
    if (!isConnected) return;

    const unsubscribe = signalRService.onMessage((message) => {
      setMessages((prev) => [...prev, message]);
    });

    return unsubscribe;
  }, [isConnected]);

  const sendMessage = useCallback(async (message: string) => {
    if (!conversationId) {
      throw new Error('Conversation ID is required to send messages');
    }

    try {
      await signalRService.sendMessage(conversationId, message);
    } catch (error: any) {
      toast({
        title: 'Failed to send message',
        description: error.message,
        variant: 'destructive',
      });
      throw error;
    }
  }, [conversationId, toast]);

  const sendTypingIndicator = useCallback(async () => {
    if (!conversationId) return;

    try {
      await signalRService.sendTypingIndicator(conversationId);
    } catch (error) {
      // Silently fail for typing indicator
      console.error('Failed to send typing indicator:', error);
    }
  }, [conversationId]);

  return {
    isConnected,
    messages,
    isTyping,
    sendMessage,
    sendTypingIndicator,
  };
}

/**
 * Hook for managing SignalR notifications
 */
export function useNotifications() {
  const [isConnected, setIsConnected] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const { isLoggedIn } = useAuth();
  const { toast } = useToast();

  // Initialize notification connection
  useEffect(() => {
    if (!isLoggedIn) return;

    const initializeNotifications = async () => {
      try {
        await signalRService.initializeNotificationHub();
        setIsConnected(true);
      } catch (error: any) {
        console.error('Failed to initialize notifications:', error);
        toast({
          title: 'Notification Connection Error',
          description: error.message || 'Failed to connect to notification server',
          variant: 'destructive',
        });
      }
    };

    initializeNotifications();

    return () => {
      signalRService.disconnectNotifications();
    };
  }, [isLoggedIn, toast]);

  // Subscribe to notifications
  useEffect(() => {
    if (!isConnected) return;

    const unsubscribe = signalRService.onNotification((notification) => {
      setNotifications((prev) => [notification, ...prev]);
      
      // Increment unread count
      if (!notification.isRead) {
        setUnreadCount((prev) => prev + 1);
      }

      // Show toast notification
      toast({
        title: notification.title,
        description: notification.message,
        duration: 5000,
      });
    });

    return unsubscribe;
  }, [isConnected, toast]);

  const markAsRead = useCallback((notificationId: number) => {
    setNotifications((prev) =>
      prev.map((n) =>
        n.id === notificationId ? { ...n, isRead: true } : n
      )
    );
    setUnreadCount((prev) => Math.max(0, prev - 1));
  }, []);

  const clearAll = useCallback(() => {
    setNotifications([]);
    setUnreadCount(0);
  }, []);

  return {
    isConnected,
    notifications,
    unreadCount,
    markAsRead,
    clearAll,
  };
}

/**
 * Hook for managing SignalR connection state
 */
export function useSignalRStatus() {
  const [chatStatus, setChatStatus] = useState<string | null>(null);
  const [notificationStatus, setNotificationStatus] = useState<string | null>(null);

  useEffect(() => {
    const interval = setInterval(() => {
      const chatState = signalRService.getChatState();
      const notifState = signalRService.getNotificationState();
      
      setChatStatus(chatState ? chatState.toString() : null);
      setNotificationStatus(notifState ? notifState.toString() : null);
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  return {
    isChatConnected: signalRService.isChatConnected(),
    isNotificationConnected: signalRService.isNotificationConnected(),
    chatStatus,
    notificationStatus,
  };
}

/**
 * Payment Status Update Interface
 */
export interface PaymentStatusUpdate {
  paymentId: number;
  status: string;
  amount: number;
  paymentMethod: string;
  transactionId?: string;
  updatedAt: string;
}

/**
 * Hook for managing real-time payment status updates
 */
export function usePaymentStatus(paymentId?: number) {
  const [isConnected, setIsConnected] = useState(false);
  const [paymentStatus, setPaymentStatus] = useState<PaymentStatusUpdate | null>(null);
  const [lastUpdate, setLastUpdate] = useState<Date | null>(null);
  const { isLoggedIn } = useAuth();
  const { toast } = useToast();

  // Initialize payment hub connection
  useEffect(() => {
    if (!isLoggedIn) return;

    const initializePayments = async () => {
      try {
        await signalRService.initializePaymentHub();
        setIsConnected(true);

        // Subscribe to specific payment if ID provided
        if (paymentId) {
          await signalRService.subscribeToPayment(paymentId);
        }
      } catch (error: any) {
        console.error('Failed to initialize payment updates:', error);
        toast({
          title: 'Payment Connection Error',
          description: error.message || 'Failed to connect to payment server',
          variant: 'destructive',
        });
      }
    };

    initializePayments();

    return () => {
      if (paymentId) {
        signalRService.unsubscribeFromPayment(paymentId);
      }
      signalRService.disconnectPayments();
    };
  }, [isLoggedIn, paymentId, toast]);

  // Subscribe to payment status updates
  useEffect(() => {
    if (!isConnected) return;

    const unsubscribe = signalRService.onPaymentStatusUpdate((update: PaymentStatusUpdate) => {
      setPaymentStatus(update);
      setLastUpdate(new Date());

      // Show toast notification for status changes
      toast({
        title: 'Payment Status Updated',
        description: `Payment #${update.paymentId} is now ${update.status}`,
        duration: 5000,
      });
    });

    return unsubscribe;
  }, [isConnected, toast]);

  const refreshStatus = useCallback(async (id: number) => {
    try {
      await signalRService.getPaymentStatus(id);
    } catch (error: any) {
      toast({
        title: 'Failed to get payment status',
        description: error.message,
        variant: 'destructive',
      });
    }
  }, [toast]);

  return {
    isConnected,
    paymentStatus,
    lastUpdate,
    refreshStatus,
  };
}
