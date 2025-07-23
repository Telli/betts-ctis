import { cn } from "@/lib/utils"

interface SkeletonProps {
  className?: string
  lines?: number
  variant?: "text" | "circular" | "rectangular" | "card"
}

export function LoadingSkeleton({ 
  className, 
  lines = 1, 
  variant = "text" 
}: SkeletonProps) {
  const baseClasses = "animate-pulse bg-muted"
  
  const variantClasses = {
    text: "h-4 rounded",
    circular: "rounded-full",
    rectangular: "rounded",
    card: "h-32 rounded-lg"
  }

  if (variant === "card") {
    return (
      <div className={cn(baseClasses, variantClasses.card, className)} />
    )
  }

  if (lines === 1) {
    return (
      <div className={cn(baseClasses, variantClasses[variant], className)} />
    )
  }

  return (
    <div className="space-y-2">
      {Array.from({ length: lines }, (_, i) => (
        <div
          key={i}
          className={cn(
            baseClasses,
            variantClasses[variant],
            i === lines - 1 && "w-3/4", // Last line is shorter
            className
          )}
        />
      ))}
    </div>
  )
}

// Specific skeleton components for common use cases
export function TableSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: rows }, (_, i) => (
        <div key={i} className="flex space-x-4">
          <LoadingSkeleton className="w-12" />
          <LoadingSkeleton className="flex-1" />
          <LoadingSkeleton className="w-24" />
          <LoadingSkeleton className="w-20" />
        </div>
      ))}
    </div>
  )
}

export function CardSkeleton() {
  return (
    <div className="border rounded-lg p-6 space-y-4">
      <div className="flex items-center space-x-4">
        <LoadingSkeleton variant="circular" className="w-12 h-12" />
        <div className="space-y-2 flex-1">
          <LoadingSkeleton className="h-4 w-1/4" />
          <LoadingSkeleton className="h-3 w-1/2" />
        </div>
      </div>
      <LoadingSkeleton lines={3} />
    </div>
  )
}

export function DashboardSkeleton() {
  return (
    <div className="space-y-6">
      {/* Header skeleton */}
      <div className="flex justify-between items-center">
        <LoadingSkeleton className="h-8 w-48" />
        <LoadingSkeleton className="h-9 w-24" />
      </div>
      
      {/* Stats cards skeleton */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }, (_, i) => (
          <CardSkeleton key={i} />
        ))}
      </div>
      
      {/* Main content skeleton */}
      <div className="grid gap-6 md:grid-cols-2">
        <div className="space-y-4">
          <LoadingSkeleton className="h-6 w-32" />
          <TableSkeleton rows={3} />
        </div>
        <div className="space-y-4">
          <LoadingSkeleton className="h-6 w-32" />
          <LoadingSkeleton variant="card" />
        </div>
      </div>
    </div>
  )
}