/**
 * SignalR Client for Real-Time Features
 * Handles chat, notifications, and real-time updates
 */

import * as signalR from "@microsoft/signalr";
import { getToken } from "./api-client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

export interface ChatMessage {
  id: number;
  conversationId: number;
  senderId: string;
  senderName: string;
  message: string;
  timestamp: Date;
  isRead: boolean;
}

export interface Notification {
  id: number;
  userId: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: Date;
}

export interface PaymentStatusUpdate {
  paymentId: number;
  status: string;
  amount: number;
  paymentMethod: string;
  transactionId?: string;
  updatedAt: string;
}

class SignalRService {
  private chatConnection: signalR.HubConnection | null = null;
  private notificationConnection: signalR.HubConnection | null = null;
  private paymentConnection: signalR.HubConnection | null = null;
  private messageHandlers: Array<(message: ChatMessage) => void> = [];
  private notificationHandlers: Array<(notification: Notification) => void> = [];
  private paymentStatusHandlers: Array<(update: PaymentStatusUpdate) => void> = [];

  /**
   * Initialize chat hub connection
   */
  async initializeChatHub() {
    if (this.chatConnection?.state === signalR.HubConnectionState.Connected) {
      console.log('Chat hub already connected');
      return this.chatConnection;
    }

    const token = getToken();
    if (!token) {
      throw new Error('No authentication token found');
    }

    this.chatConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/chathub`, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext: signalR.RetryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up message handler
    this.chatConnection.on("ReceiveMessage", (message: ChatMessage) => {
      console.log('Received message:', message);
      this.messageHandlers.forEach(handler => handler(message));
    });

    // Set up typing indicator
    this.chatConnection.on("UserTyping", (userId: string, conversationId: number) => {
      console.log(`User ${userId} is typing in conversation ${conversationId}`);
    });

    // Handle reconnection
    this.chatConnection.onreconnecting((error?: Error) => {
      console.warn('Chat connection lost. Reconnecting...', error);
    });

    this.chatConnection.onreconnected((connectionId?: string) => {
      console.log('Chat reconnected. Connection ID:', connectionId);
    });

    this.chatConnection.onclose((error?: Error) => {
      console.error('Chat connection closed:', error);
    });

    try {
      await this.chatConnection.start();
      console.log('✅ Chat hub connected successfully');
      return this.chatConnection;
    } catch (error) {
      console.error('❌ Error connecting to chat hub:', error);
      throw error;
    }
  }

  /**
   * Initialize notification hub connection
   */
  async initializeNotificationHub() {
    if (this.notificationConnection?.state === signalR.HubConnectionState.Connected) {
      console.log('Notification hub already connected');
      return this.notificationConnection;
    }

    const token = getToken();
    if (!token) {
      throw new Error('No authentication token found');
    }

    this.notificationConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/notificationhub`, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up notification handler
    this.notificationConnection.on("ReceiveNotification", (notification: Notification) => {
      console.log('Received notification:', notification);
      this.notificationHandlers.forEach(handler => handler(notification));
    });

    // Handle reconnection
    this.notificationConnection.onreconnecting((error?: Error) => {
      console.warn('Notification connection lost. Reconnecting...', error);
    });

    this.notificationConnection.onreconnected((connectionId?: string) => {
      console.log('Notification reconnected. Connection ID:', connectionId);
    });

    this.notificationConnection.onclose((error?: Error) => {
      console.error('Notification connection closed:', error);
    });

    try {
      await this.notificationConnection.start();
      console.log('✅ Notification hub connected successfully');
      return this.notificationConnection;
    } catch (error) {
      console.error('❌ Error connecting to notification hub:', error);
      throw error;
    }
  }

  /**
   * Send a chat message
   */
  async sendMessage(conversationId: number, message: string): Promise<void> {
    if (!this.chatConnection || this.chatConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Chat hub not connected');
    }

    try {
      await this.chatConnection.invoke("SendMessage", conversationId, message);
      console.log('Message sent successfully');
    } catch (error) {
      console.error('Error sending message:', error);
      throw error;
    }
  }

  /**
   * Send typing indicator
   */
  async sendTypingIndicator(conversationId: number): Promise<void> {
    if (!this.chatConnection || this.chatConnection.state !== signalR.HubConnectionState.Connected) {
      return; // Silently fail for typing indicator
    }

    try {
      await this.chatConnection.invoke("UserTyping", conversationId);
    } catch (error) {
      console.error('Error sending typing indicator:', error);
    }
  }

  /**
   * Join a conversation room
   */
  async joinConversation(conversationId: number): Promise<void> {
    if (!this.chatConnection || this.chatConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Chat hub not connected');
    }

    try {
      await this.chatConnection.invoke("JoinConversation", conversationId);
      console.log(`Joined conversation ${conversationId}`);
    } catch (error) {
      console.error('Error joining conversation:', error);
      throw error;
    }
  }

  /**
   * Leave a conversation room
   */
  async leaveConversation(conversationId: number): Promise<void> {
    if (!this.chatConnection || this.chatConnection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.chatConnection.invoke("LeaveConversation", conversationId);
      console.log(`Left conversation ${conversationId}`);
    } catch (error) {
      console.error('Error leaving conversation:', error);
    }
  }

  /**
   * Subscribe to chat messages
   */
  onMessage(handler: (message: ChatMessage) => void): () => void {
    this.messageHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      this.messageHandlers = this.messageHandlers.filter(h => h !== handler);
    };
  }

  /**
   * Subscribe to notifications
   */
  onNotification(handler: (notification: Notification) => void): () => void {
    this.notificationHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      this.notificationHandlers = this.notificationHandlers.filter(h => h !== handler);
    };
  }

  /**
   * Disconnect from chat hub
   */
  async disconnectChat(): Promise<void> {
    if (this.chatConnection) {
      try {
        await this.chatConnection.stop();
        console.log('Chat hub disconnected');
      } catch (error) {
        console.error('Error disconnecting chat hub:', error);
      }
      this.chatConnection = null;
      this.messageHandlers = [];
    }
  }

  /**
   * Disconnect from notification hub
   */
  async disconnectNotifications(): Promise<void> {
    if (this.notificationConnection) {
      try {
        await this.notificationConnection.stop();
        console.log('Notification hub disconnected');
      } catch (error) {
        console.error('Error disconnecting notification hub:', error);
      }
      this.notificationConnection = null;
      this.notificationHandlers = [];
    }
  }

  /**
   * Disconnect all hubs
   */
  async disconnectAll(): Promise<void> {
    await Promise.all([
      this.disconnectChat(),
      this.disconnectNotifications(),
      this.disconnectPayments()
    ]);
  }

  /**
   * Get chat connection state
   */
  getChatState(): signalR.HubConnectionState | null {
    return this.chatConnection?.state || null;
  }

  /**
   * Get notification connection state
   */
  getNotificationState(): signalR.HubConnectionState | null {
    return this.notificationConnection?.state || null;
  }

  /**
   * Check if chat is connected
   */
  isChatConnected(): boolean {
    return this.chatConnection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Check if notifications are connected
   */
  isNotificationConnected(): boolean {
    return this.notificationConnection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Initialize payment hub connection
   */
  async initializePaymentHub() {
    if (this.paymentConnection?.state === signalR.HubConnectionState.Connected) {
      console.log('Payment hub already connected');
      return this.paymentConnection;
    }

    const token = getToken();
    if (!token) {
      throw new Error('No authentication token found');
    }

    this.paymentConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/payments`, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up payment status update handler
    this.paymentConnection.on("PaymentStatusUpdate", (update: PaymentStatusUpdate) => {
      console.log('Received payment status update:', update);
      this.paymentStatusHandlers.forEach(handler => handler(update));
    });

    // Set up payment confirmed handler
    this.paymentConnection.on("PaymentConfirmed", (confirmation: any) => {
      console.log('Payment confirmed:', confirmation);
      // Treat confirmation as status update
      this.paymentStatusHandlers.forEach(handler => handler({
        paymentId: confirmation.paymentId,
        status: 'confirmed',
        amount: confirmation.amount,
        paymentMethod: confirmation.paymentMethod,
        transactionId: confirmation.transactionId,
        updatedAt: confirmation.confirmedAt
      }));
    });

    // Handle reconnection
    this.paymentConnection.onreconnecting((error?: Error) => {
      console.warn('Payment connection lost. Reconnecting...', error);
    });

    this.paymentConnection.onreconnected((connectionId?: string) => {
      console.log('Payment reconnected. Connection ID:', connectionId);
    });

    this.paymentConnection.onclose((error?: Error) => {
      console.error('Payment connection closed:', error);
    });

    try {
      await this.paymentConnection.start();
      console.log('✅ Payment hub connected successfully');
      return this.paymentConnection;
    } catch (error) {
      console.error('❌ Error connecting to payment hub:', error);
      throw error;
    }
  }

  /**
   * Subscribe to payment status updates
   */
  async subscribeToPayment(paymentId: number): Promise<void> {
    if (!this.paymentConnection || this.paymentConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Payment hub not connected');
    }

    try {
      await this.paymentConnection.invoke("SubscribeToPayment", paymentId);
      console.log(`Subscribed to payment ${paymentId}`);
    } catch (error) {
      console.error('Error subscribing to payment:', error);
      throw error;
    }
  }

  /**
   * Unsubscribe from payment status updates
   */
  async unsubscribeFromPayment(paymentId: number): Promise<void> {
    if (!this.paymentConnection || this.paymentConnection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.paymentConnection.invoke("UnsubscribeFromPayment", paymentId);
      console.log(`Unsubscribed from payment ${paymentId}`);
    } catch (error) {
      console.error('Error unsubscribing from payment:', error);
    }
  }

  /**
   * Get current payment status
   */
  async getPaymentStatus(paymentId: number): Promise<void> {
    if (!this.paymentConnection || this.paymentConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Payment hub not connected');
    }

    try {
      await this.paymentConnection.invoke("GetPaymentStatus", paymentId);
      console.log(`Requested status for payment ${paymentId}`);
    } catch (error) {
      console.error('Error getting payment status:', error);
      throw error;
    }
  }

  /**
   * Subscribe to payment status updates
   */
  onPaymentStatusUpdate(handler: (update: PaymentStatusUpdate) => void): () => void {
    this.paymentStatusHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      this.paymentStatusHandlers = this.paymentStatusHandlers.filter(h => h !== handler);
    };
  }

  /**
   * Disconnect from payment hub
   */
  async disconnectPayments(): Promise<void> {
    if (this.paymentConnection) {
      try {
        await this.paymentConnection.stop();
        console.log('Payment hub disconnected');
      } catch (error) {
        console.error('Error disconnecting payment hub:', error);
      }
      this.paymentConnection = null;
      this.paymentStatusHandlers = [];
    }
  }

  /**
   * Get payment connection state
   */
  getPaymentState(): signalR.HubConnectionState | null {
    return this.paymentConnection?.state || null;
  }

  /**
   * Check if payments are connected
   */
  isPaymentConnected(): boolean {
    return this.paymentConnection?.state === signalR.HubConnectionState.Connected;
  }
}

// Export singleton instance
export const signalRService = new SignalRService();

// Export for testing
export { SignalRService };
