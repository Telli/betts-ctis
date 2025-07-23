"use client"

import { toast } from '@/components/ui/enhanced-toast'

// Service Worker registration and management
export class OfflineManager {
  private static instance: OfflineManager
  private swRegistration: ServiceWorkerRegistration | null = null
  private isOnline: boolean = true
  private offlineQueue: Array<{ url: string; options: RequestInit; timestamp: number }> = []

  private constructor() {
    if (typeof window !== 'undefined') {
      this.isOnline = navigator.onLine
      this.setupEventListeners()
      this.registerServiceWorker()
    }
  }

  static getInstance(): OfflineManager {
    if (!OfflineManager.instance) {
      OfflineManager.instance = new OfflineManager()
    }
    return OfflineManager.instance
  }

  private setupEventListeners() {
    window.addEventListener('online', this.handleOnline.bind(this))
    window.addEventListener('offline', this.handleOffline.bind(this))
  }

  private async registerServiceWorker() {
    if ('serviceWorker' in navigator && process.env.NODE_ENV === 'production') {
      try {
        const registration = await navigator.serviceWorker.register('/sw.js')
        this.swRegistration = registration
        
        // Check for updates
        registration.addEventListener('updatefound', () => {
          const newWorker = registration.installing
          if (newWorker) {
            newWorker.addEventListener('statechange', () => {
              if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                this.showUpdateAvailable()
              }
            })
          }
        })

        console.log('Service Worker registered successfully')
      } catch (error) {
        console.error('Service Worker registration failed:', error)
      }
    }
  }

  private handleOnline() {
    this.isOnline = true
    toast.success('Connection restored', {
      description: 'You are back online. Syncing pending changes...'
    })
    this.processOfflineQueue()
  }

  private handleOffline() {
    this.isOnline = false
    toast.warning('No internet connection', {
      description: 'You are now offline. Changes will be saved locally and synced when connection is restored.',
      duration: 6000
    })
  }

  private showUpdateAvailable() {
    toast.info('App update available', {
      description: 'A new version is ready. Refresh to update.',
      duration: 10000,
      action: {
        label: 'Refresh',
        onClick: () => window.location.reload()
      }
    })
  }

  // Queue requests when offline
  public async queueRequest(url: string, options: RequestInit = {}) {
    if (this.isOnline) {
      return fetch(url, options)
    }

    // Add to offline queue
    this.offlineQueue.push({
      url,
      options,
      timestamp: Date.now()
    })

    // Store in localStorage for persistence
    this.saveOfflineQueue()

    toast.info('Request queued', {
      description: 'This action will be completed when you are back online.'
    })

    return Promise.reject(new Error('Offline - request queued'))
  }

  private saveOfflineQueue() {
    try {
      localStorage.setItem('offline-queue', JSON.stringify(this.offlineQueue))
    } catch (error) {
      console.error('Failed to save offline queue:', error)
    }
  }

  private loadOfflineQueue() {
    try {
      const saved = localStorage.getItem('offline-queue')
      if (saved) {
        this.offlineQueue = JSON.parse(saved)
      }
    } catch (error) {
      console.error('Failed to load offline queue:', error)
      this.offlineQueue = []
    }
  }

  private async processOfflineQueue() {
    if (this.offlineQueue.length === 0) return

    const queue = [...this.offlineQueue]
    this.offlineQueue = []
    this.saveOfflineQueue()

    let successCount = 0
    let failureCount = 0

    for (const request of queue) {
      try {
        await fetch(request.url, request.options)
        successCount++
      } catch (error) {
        failureCount++
        // Re-queue failed requests
        this.offlineQueue.push(request)
      }
    }

    if (successCount > 0) {
      toast.success(`${successCount} changes synced successfully`)
    }

    if (failureCount > 0) {
      toast.warning(`${failureCount} changes failed to sync`, {
        description: 'Will retry when connection improves.'
      })
      this.saveOfflineQueue()
    }
  }

  // Check if app is online
  public isAppOnline(): boolean {
    return this.isOnline
  }

  // Manual sync trigger
  public async syncNow() {
    if (!this.isOnline) {
      toast.warning('Cannot sync while offline')
      return
    }

    await this.processOfflineQueue()
  }

  // Clear offline data
  public clearOfflineData() {
    this.offlineQueue = []
    localStorage.removeItem('offline-queue')
    toast.info('Offline data cleared')
  }
}

// Offline storage utilities
export class OfflineStorage {
  private static readonly CACHE_PREFIX = 'ctis-cache-'
  private static readonly CACHE_VERSION = 'v1'

  // Cache data with expiration
  static async setItem(key: string, data: any, expirationMinutes: number = 60) {
    const item = {
      data,
      timestamp: Date.now(),
      expiration: Date.now() + (expirationMinutes * 60 * 1000)
    }

    try {
      localStorage.setItem(
        `${this.CACHE_PREFIX}${this.CACHE_VERSION}-${key}`,
        JSON.stringify(item)
      )
    } catch (error) {
      console.error('Failed to cache data:', error)
    }
  }

  // Get cached data if not expired
  static getItem<T>(key: string): T | null {
    try {
      const cached = localStorage.getItem(
        `${this.CACHE_PREFIX}${this.CACHE_VERSION}-${key}`
      )
      
      if (!cached) return null

      const item = JSON.parse(cached)
      
      // Check if expired
      if (Date.now() > item.expiration) {
        this.removeItem(key)
        return null
      }

      return item.data
    } catch (error) {
      console.error('Failed to retrieve cached data:', error)
      return null
    }
  }

  // Remove cached item
  static removeItem(key: string) {
    localStorage.removeItem(`${this.CACHE_PREFIX}${this.CACHE_VERSION}-${key}`)
  }

  // Clear all cached data
  static clearAll() {
    const keys = Object.keys(localStorage)
    keys.forEach(key => {
      if (key.startsWith(this.CACHE_PREFIX)) {
        localStorage.removeItem(key)
      }
    })
  }

  // Get cache size and statistics
  static getCacheStats() {
    const keys = Object.keys(localStorage)
    const cacheKeys = keys.filter(key => key.startsWith(this.CACHE_PREFIX))
    
    let totalSize = 0
    let expiredCount = 0
    let validCount = 0

    cacheKeys.forEach(key => {
      const value = localStorage.getItem(key)
      if (value) {
        totalSize += value.length
        
        try {
          const item = JSON.parse(value)
          if (Date.now() > item.expiration) {
            expiredCount++
          } else {
            validCount++
          }
        } catch {
          expiredCount++
        }
      }
    })

    return {
      totalItems: cacheKeys.length,
      validItems: validCount,
      expiredItems: expiredCount,
      totalSizeKB: Math.round(totalSize / 1024)
    }
  }
}

// Hook for offline functionality
import { useState, useEffect } from 'react'

export function useOffline() {
  const [isOffline, setIsOffline] = useState(false)
  const [queueSize, setQueueSize] = useState(0)

  useEffect(() => {
    const offlineManager = OfflineManager.getInstance()
    
    const updateStatus = () => {
      setIsOffline(!offlineManager.isAppOnline())
    }

    // Update queue size periodically
    const updateQueueSize = () => {
      try {
        const queue = localStorage.getItem('offline-queue')
        const queueData = queue ? JSON.parse(queue) : []
        setQueueSize(queueData.length)
      } catch {
        setQueueSize(0)
      }
    }

    // Initial check
    updateStatus()
    updateQueueSize()

    // Listen for online/offline events
    window.addEventListener('online', updateStatus)
    window.addEventListener('offline', updateStatus)

    // Update queue size every 5 seconds
    const interval = setInterval(updateQueueSize, 5000)

    return () => {
      window.removeEventListener('online', updateStatus)
      window.removeEventListener('offline', updateStatus)
      clearInterval(interval)
    }
  }, [])

  const syncNow = () => {
    OfflineManager.getInstance().syncNow()
  }

  const clearOfflineData = () => {
    OfflineManager.getInstance().clearOfflineData()
    setQueueSize(0)
  }

  return {
    isOffline,
    queueSize,
    syncNow,
    clearOfflineData
  }
}

// PWA install prompt
export function usePWAInstall() {
  const [installPrompt, setInstallPrompt] = useState<any>(null)
  const [isInstallable, setIsInstallable] = useState(false)

  useEffect(() => {
    const handleBeforeInstallPrompt = (e: Event) => {
      // Prevent the mini-infobar from appearing on mobile
      e.preventDefault()
      setInstallPrompt(e)
      setIsInstallable(true)
    }

    const handleAppInstalled = () => {
      setInstallPrompt(null)
      setIsInstallable(false)
      toast.success('App installed successfully', {
        description: 'Betts CTIS has been added to your home screen.'
      })
    }

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    window.addEventListener('appinstalled', handleAppInstalled)

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
      window.removeEventListener('appinstalled', handleAppInstalled)
    }
  }, [])

  const installApp = async () => {
    if (!installPrompt) return false

    try {
      const result = await installPrompt.prompt()
      
      if (result.outcome === 'accepted') {
        setInstallPrompt(null)
        setIsInstallable(false)
        return true
      }
    } catch (error) {
      console.error('Failed to install app:', error)
    }

    return false
  }

  return {
    isInstallable,
    installApp
  }
}