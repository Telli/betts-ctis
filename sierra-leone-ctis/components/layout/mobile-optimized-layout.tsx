"use client"

import * as React from "react"
import { cn } from "@/lib/utils"
import { useIsMobile } from "@/components/ui/use-mobile"
import { MobileNavigation, ClientPortalMobileNav, AdminMobileNav } from "@/components/ui/mobile-navigation"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { WifiOff, CloudOff, RefreshCw } from "lucide-react"
import { useOffline, usePWAInstall } from "@/lib/offline-manager"
import { toast } from "@/components/ui/enhanced-toast"

interface MobileOptimizedLayoutProps {
  children: React.ReactNode
  userRole?: 'client' | 'admin' | 'associate'
  currentPath?: string
  onLogout?: () => void
  className?: string
}

export function MobileOptimizedLayout({
  children,
  userRole,
  currentPath,
  onLogout,
  className
}: MobileOptimizedLayoutProps) {
  const isMobile = useIsMobile()
  const { isOffline, queueSize, syncNow } = useOffline()
  const { isInstallable, installApp } = usePWAInstall()

  // Handle PWA install
  const handleInstallApp = async () => {
    const success = await installApp()
    if (success) {
      toast.success('Installing app...', {
        description: 'Betts CTIS will be added to your home screen.'
      })
    }
  }

  return (
    <div className={cn("min-h-screen bg-background", className)}>
      {/* Offline Status Bar */}
      {isOffline && (
        <div className="bg-yellow-500 text-yellow-900 px-4 py-2 text-sm font-medium flex items-center justify-between">
          <div className="flex items-center gap-2">
            <WifiOff className="h-4 w-4" />
            <span>You are offline</span>
            {queueSize > 0 && (
              <Badge variant="secondary" className="text-xs">
                {queueSize} pending
              </Badge>
            )}
          </div>
          <Button
            size="sm"
            variant="ghost"
            onClick={syncNow}
            className="text-yellow-900 hover:bg-yellow-400"
          >
            <RefreshCw className="h-3 w-3 mr-1" />
            Sync
          </Button>
        </div>
      )}

      {/* PWA Install Banner */}
      {isInstallable && (
        <div className="bg-blue-50 border-b border-blue-200 px-4 py-3 text-sm">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <p className="font-medium text-blue-900">Install Betts CTIS</p>
              <p className="text-blue-700">Add to your home screen for quick access</p>
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="ghost" onClick={() => {}}>
                Dismiss
              </Button>
              <Button size="sm" onClick={handleInstallApp}>
                Install
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Mobile Header */}
      {isMobile && (
        <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          <div className="container flex h-14 items-center justify-between px-4">
            <div className="flex items-center gap-2">
              {/* Mobile Navigation */}
              {userRole === 'client' && (
                <ClientPortalMobileNav 
                  currentPath={currentPath} 
                  onLogout={onLogout} 
                />
              )}
              {(userRole === 'admin' || userRole === 'associate') && (
                <AdminMobileNav 
                  currentPath={currentPath} 
                  onLogout={onLogout} 
                />
              )}
              
              {/* Logo */}
              <div className="flex items-center gap-2">
                <div className="h-8 w-8 rounded bg-sierra-blue flex items-center justify-center">
                  <span className="text-white font-bold text-sm">B</span>
                </div>
                <span className="font-semibold text-sm">Betts CTIS</span>
              </div>
            </div>

            {/* Status Indicators */}
            <div className="flex items-center gap-2">
              {isOffline && (
                <CloudOff className="h-4 w-4 text-muted-foreground" />
              )}
            </div>
          </div>
        </header>
      )}

      {/* Main Content */}
      <main className={cn(
        "flex-1",
        isMobile ? "pb-safe-bottom" : "",
        // Add padding for mobile header
        isMobile ? "pt-0" : ""
      )}>
        {children}
      </main>

      {/* Mobile Bottom Safe Area */}
      {isMobile && (
        <div className="h-safe-bottom bg-background" />
      )}
    </div>
  )
}

// Mobile-optimized card component
interface MobileCardProps {
  children: React.ReactNode
  className?: string
  padding?: 'none' | 'sm' | 'md' | 'lg'
  clickable?: boolean
  onClick?: () => void
}

export function MobileCard({
  children,
  className,
  padding = 'md',
  clickable = false,
  onClick
}: MobileCardProps) {
  const isMobile = useIsMobile()

  const paddingClasses = {
    none: '',
    sm: isMobile ? 'p-3' : 'p-4',
    md: isMobile ? 'p-4' : 'p-6',
    lg: isMobile ? 'p-6' : 'p-8'
  }

  return (
    <div
      className={cn(
        "bg-card text-card-foreground rounded-lg border shadow-sm",
        paddingClasses[padding],
        clickable && "cursor-pointer hover:shadow-md transition-shadow",
        isMobile && "mx-4 mb-4",
        className
      )}
      onClick={clickable ? onClick : undefined}
    >
      {children}
    </div>
  )
}

// Mobile-optimized form layout
interface MobileFormProps {
  children: React.ReactNode
  className?: string
  onSubmit?: (e: React.FormEvent) => void
}

export function MobileForm({
  children,
  className,
  onSubmit
}: MobileFormProps) {
  const isMobile = useIsMobile()

  return (
    <form
      onSubmit={onSubmit}
      className={cn(
        "space-y-4",
        isMobile && "px-4",
        className
      )}
    >
      {children}
    </form>
  )
}

// Mobile-optimized button group
interface MobileButtonGroupProps {
  children: React.ReactNode
  className?: string
  orientation?: 'horizontal' | 'vertical'
}

export function MobileButtonGroup({
  children,
  className,
  orientation = 'horizontal'
}: MobileButtonGroupProps) {
  const isMobile = useIsMobile()

  return (
    <div
      className={cn(
        "flex gap-2",
        // Stack buttons vertically on mobile for better touch targets
        isMobile && orientation === 'horizontal' ? "flex-col" : "",
        orientation === 'vertical' ? "flex-col" : "",
        // Add padding on mobile
        isMobile && "px-4 py-2",
        className
      )}
    >
      {children}
    </div>
  )
}

// Mobile-optimized table wrapper
interface MobileTableWrapperProps {
  children: React.ReactNode
  className?: string
}

export function MobileTableWrapper({
  children,
  className
}: MobileTableWrapperProps) {
  const isMobile = useIsMobile()

  if (isMobile) {
    return (
      <div className={cn("overflow-x-auto -mx-4", className)}>
        <div className="inline-block min-w-full align-middle">
          <div className="px-4">
            {children}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className={cn("overflow-x-auto", className)}>
      {children}
    </div>
  )
}

// Mobile-optimized grid
interface MobileGridProps {
  children: React.ReactNode
  columns?: {
    mobile: number
    tablet: number
    desktop: number
  }
  className?: string
}

export function MobileGrid({
  children,
  columns = { mobile: 1, tablet: 2, desktop: 3 },
  className
}: MobileGridProps) {
  return (
    <div
      className={cn(
        "grid gap-4",
        `grid-cols-${columns.mobile}`,
        `md:grid-cols-${columns.tablet}`,
        `lg:grid-cols-${columns.desktop}`,
        className
      )}
    >
      {children}
    </div>
  )
}

// Safe area utilities for mobile devices
export function useSafeArea() {
  const [safeArea, setSafeArea] = React.useState({
    top: 0,
    bottom: 0,
    left: 0,
    right: 0
  })

  React.useEffect(() => {
    if (typeof window === 'undefined') return

    const updateSafeArea = () => {
      const computedStyle = getComputedStyle(document.documentElement)
      
      setSafeArea({
        top: parseInt(computedStyle.getPropertyValue('--sat') || '0'),
        bottom: parseInt(computedStyle.getPropertyValue('--sab') || '0'),
        left: parseInt(computedStyle.getPropertyValue('--sal') || '0'),
        right: parseInt(computedStyle.getPropertyValue('--sar') || '0')
      })
    }

    updateSafeArea()
    window.addEventListener('resize', updateSafeArea)
    window.addEventListener('orientationchange', updateSafeArea)

    return () => {
      window.removeEventListener('resize', updateSafeArea)
      window.removeEventListener('orientationchange', updateSafeArea)
    }
  }, [])

  return safeArea
}