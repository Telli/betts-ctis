import { lazy, ComponentType } from 'react'

// Lazy loading utilities
export function lazyLoad<T extends ComponentType<any>>(
  factory: () => Promise<{ default: T }>,
  fallback?: ComponentType
) {
  const Component = lazy(factory)
  
  if (fallback) {
    (Component as any).displayName = `LazyLoaded(${(Component as any).displayName || 'Component'})`
  }
  
  return Component
}

// Performance monitoring
export class PerformanceMonitor {
  private static marks: Map<string, number> = new Map()
  
  static startTiming(label: string) {
    this.marks.set(label, performance.now())
  }
  
  static endTiming(label: string): number {
    const startTime = this.marks.get(label)
    if (!startTime) {
      console.warn(`No start time found for "${label}"`)
      return 0
    }
    
    const duration = performance.now() - startTime
    this.marks.delete(label)
    
    if (process.env.NODE_ENV === 'development') {
      console.log(`⏱️ ${label}: ${duration.toFixed(2)}ms`)
    }
    
    return duration
  }
  
  static measureRender<T extends ComponentType<any>>(
    Component: T,
    displayName?: string
  ): T {
    const WrappedComponent = (props: any) => {
      const componentName = displayName || Component.displayName || 'Component'
      
      React.useEffect(() => {
        this.startTiming(`${componentName} render`)
        return () => {
          this.endTiming(`${componentName} render`)
        }
      })
      
      return React.createElement(Component, props)
    }
    
    WrappedComponent.displayName = `Measured(${displayName || Component.displayName})`
    return WrappedComponent as T
  }
}

// Image optimization helpers
export function getOptimizedImageSrc(
  src: string,
  width: number,
  height?: number,
  quality: number = 75
): string {
  if (!src) return ''
  
  // For external images, return as-is
  if (src.startsWith('http')) {
    return src
  }
  
  // For local images, construct optimized URL
  const params = new URLSearchParams()
  params.set('w', width.toString())
  if (height) params.set('h', height.toString())
  params.set('q', quality.toString())
  
  return `/_next/image?url=${encodeURIComponent(src)}&${params.toString()}`
}

// Debounce utility for performance
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number,
  immediate = false
): T {
  let timeout: NodeJS.Timeout | null = null
  
  return ((...args: Parameters<T>) => {
    const later = () => {
      timeout = null
      if (!immediate) func.apply(null, args)
    }
    
    const callNow = immediate && !timeout
    
    if (timeout) clearTimeout(timeout)
    timeout = setTimeout(later, wait)
    
    if (callNow) func.apply(null, args)
  }) as T
}

// Throttle utility for performance
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): T {
  let inThrottle: boolean
  
  return ((...args: Parameters<T>) => {
    if (!inThrottle) {
      func.apply(null, args)
      inThrottle = true
      setTimeout(() => inThrottle = false, limit)
    }
  }) as T
}

// Virtual scrolling helper
export function calculateVirtualScrollItems(
  totalItems: number,
  containerHeight: number,
  itemHeight: number,
  scrollTop: number,
  overscan: number = 3
) {
  const visibleItemsCount = Math.ceil(containerHeight / itemHeight)
  const startIndex = Math.max(0, Math.floor(scrollTop / itemHeight) - overscan)
  const endIndex = Math.min(
    totalItems - 1,
    startIndex + visibleItemsCount + overscan * 2
  )
  
  return {
    startIndex,
    endIndex,
    visibleItems: endIndex - startIndex + 1,
    offsetY: startIndex * itemHeight
  }
}

// Memory usage monitoring
export class MemoryMonitor {
  static logMemoryUsage(label?: string) {
    if (typeof window === 'undefined' || !('memory' in performance)) {
      return
    }
    
    const memory = (performance as any).memory
    const used = memory.usedJSHeapSize / 1024 / 1024
    const total = memory.totalJSHeapSize / 1024 / 1024
    const limit = memory.jsHeapSizeLimit / 1024 / 1024
    
    console.log(`🧠 Memory Usage${label ? ` (${label})` : ''}:`, {
      used: `${used.toFixed(2)} MB`,
      total: `${total.toFixed(2)} MB`,
      limit: `${limit.toFixed(2)} MB`,
      percentage: `${((used / limit) * 100).toFixed(1)}%`
    })
  }
  
  static checkMemoryPressure(): boolean {
    if (typeof window === 'undefined' || !('memory' in performance)) {
      return false
    }
    
    const memory = (performance as any).memory
    const usage = memory.usedJSHeapSize / memory.jsHeapSizeLimit
    
    // Consider memory pressure if usage is above 80%
    return usage > 0.8
  }
}

// Bundle size analyzer (development only)
export function analyzeBundleSize() {
  if (process.env.NODE_ENV !== 'development') {
    return
  }
  
  const scripts = Array.from(document.querySelectorAll('script[src]'))
  const styles = Array.from(document.querySelectorAll('link[rel="stylesheet"]'))
  
  console.group('📦 Bundle Analysis')
  
  scripts.forEach((script) => {
    const scriptElement = script as HTMLScriptElement
    if (scriptElement.src && scriptElement.src.includes('_next/static')) {
      console.log('Script:', scriptElement.src.split('/').pop())
    }
  })
  
  styles.forEach((style) => {
    const styleElement = style as HTMLLinkElement
    if (styleElement.href && styleElement.href.includes('_next/static')) {
      console.log('Stylesheet:', styleElement.href.split('/').pop())
    }
  })
  
  console.groupEnd()
}

// Intersection Observer for lazy loading
export function createIntersectionObserver(
  callback: (entries: IntersectionObserverEntry[]) => void,
  options: IntersectionObserverInit = {}
): IntersectionObserver | null {
  if (typeof window === 'undefined' || !('IntersectionObserver' in window)) {
    return null
  }
  
  return new IntersectionObserver(callback, {
    rootMargin: '50px',
    threshold: 0.1,
    ...options
  })
}

// Performance-optimized event listeners
export function addPassiveEventListener(
  element: EventTarget,
  event: string,
  handler: EventListener,
  options: AddEventListenerOptions = {}
) {
  element.addEventListener(event, handler, {
    passive: true,
    ...options
  })
  
  return () => element.removeEventListener(event, handler)
}

// Resource loading optimization
export function preloadResource(href: string, as: string) {
  if (typeof document === 'undefined') {
    return
  }
  
  const link = document.createElement('link')
  link.rel = 'preload'
  link.href = href
  link.as = as
  
  document.head.appendChild(link)
}

export function prefetchResource(href: string) {
  if (typeof document === 'undefined') {
    return
  }
  
  const link = document.createElement('link')
  link.rel = 'prefetch'
  link.href = href
  
  document.head.appendChild(link)
}

// React performance hooks
import React from 'react'

export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = React.useState<T>(value)
  
  React.useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value)
    }, delay)
    
    return () => {
      clearTimeout(handler)
    }
  }, [value, delay])
  
  return debouncedValue
}

export function useThrottle<T>(value: T, interval: number): T {
  const [throttledValue, setThrottledValue] = React.useState<T>(value)
  const lastUpdated = React.useRef<number>(0)
  
  React.useEffect(() => {
    const now = Date.now()
    
    if (now - lastUpdated.current >= interval) {
      setThrottledValue(value)
      lastUpdated.current = now
    } else {
      const timer = setTimeout(() => {
        setThrottledValue(value)
        lastUpdated.current = Date.now()
      }, interval - (now - lastUpdated.current))
      
      return () => clearTimeout(timer)
    }
  }, [value, interval])
  
  return throttledValue
}

export function useIntersectionObserver(
  elementRef: React.RefObject<Element>,
  options: IntersectionObserverInit = {}
) {
  const [isIntersecting, setIsIntersecting] = React.useState(false)
  
  React.useEffect(() => {
    const element = elementRef.current
    if (!element) return
    
    const observer = createIntersectionObserver(
      ([entry]) => setIsIntersecting(entry.isIntersecting),
      options
    )
    
    if (observer) {
      observer.observe(element)
      return () => observer.disconnect()
    }
  }, [elementRef, options])
  
  return isIntersecting
}