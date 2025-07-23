"use client"

import * as React from "react"
import { CheckCircle, XCircle, AlertTriangle, Info, X } from "lucide-react"
import { cn } from "@/lib/utils"
import { toast as sonnerToast } from "sonner"

export type ToastVariant = "default" | "success" | "error" | "warning" | "info"

interface ToastOptions {
  title?: string
  description?: string
  duration?: number
  action?: {
    label: string
    onClick: () => void
  }
  variant?: ToastVariant
  position?: "top-center" | "top-right" | "bottom-center" | "bottom-right"
}

const toastVariants = {
  default: {
    icon: Info,
    className: "border-border bg-background text-foreground"
  },
  success: {
    icon: CheckCircle,
    className: "border-green-200 bg-green-50 text-green-900 dark:border-green-800 dark:bg-green-900/20 dark:text-green-100"
  },
  error: {
    icon: XCircle,
    className: "border-red-200 bg-red-50 text-red-900 dark:border-red-800 dark:bg-red-900/20 dark:text-red-100"
  },
  warning: {
    icon: AlertTriangle,
    className: "border-yellow-200 bg-yellow-50 text-yellow-900 dark:border-yellow-800 dark:bg-yellow-900/20 dark:text-yellow-100"
  },
  info: {
    icon: Info,
    className: "border-blue-200 bg-blue-50 text-blue-900 dark:border-blue-800 dark:bg-blue-900/20 dark:text-blue-100"
  }
}

export const toast = {
  success: (message: string, options?: Omit<ToastOptions, 'variant'>) => {
    return sonnerToast.success(message, {
      duration: options?.duration || 4000,
      action: options?.action,
      description: options?.description,
      position: options?.position || "top-right",
      className: toastVariants.success.className
    })
  },

  error: (message: string, options?: Omit<ToastOptions, 'variant'>) => {
    return sonnerToast.error(message, {
      duration: options?.duration || 6000,
      action: options?.action,
      description: options?.description,
      position: options?.position || "top-right",
      className: toastVariants.error.className
    })
  },

  warning: (message: string, options?: Omit<ToastOptions, 'variant'>) => {
    return sonnerToast.warning(message, {
      duration: options?.duration || 5000,
      action: options?.action,
      description: options?.description,
      position: options?.position || "top-right",
      className: toastVariants.warning.className
    })
  },

  info: (message: string, options?: Omit<ToastOptions, 'variant'>) => {
    return sonnerToast.info(message, {
      duration: options?.duration || 4000,
      action: options?.action,
      description: options?.description,
      position: options?.position || "top-right",
      className: toastVariants.info.className
    })
  },

  default: (message: string, options?: Omit<ToastOptions, 'variant'>) => {
    return sonnerToast(message, {
      duration: options?.duration || 4000,
      action: options?.action,
      description: options?.description,
      position: options?.position || "top-right",
      className: toastVariants.default.className
    })
  },

  promise: <T,>(
    promise: Promise<T>,
    {
      loading,
      success,
      error,
      ...options
    }: {
      loading: string
      success: string | ((data: T) => string)
      error: string | ((error: any) => string)
    } & Omit<ToastOptions, 'variant'>
  ) => {
    return sonnerToast.promise(promise, {
      loading,
      success,
      error,
      duration: options.duration || 4000,
      position: options.position || "top-right"
    })
  },

  dismiss: (id?: string | number) => {
    sonnerToast.dismiss(id)
  }
}

// Specific toast functions for common use cases
export const toastUtils = {
  // Authentication toasts
  loginSuccess: (userName?: string) => {
    toast.success(
      `Welcome back${userName ? `, ${userName}` : ''}!`,
      { description: "You have successfully logged in." }
    )
  },

  loginError: (error?: string) => {
    toast.error(
      "Login failed",
      { 
        description: error || "Please check your credentials and try again.",
        duration: 6000
      }
    )
  },

  logoutSuccess: () => {
    toast.info("Logged out successfully", {
      description: "You have been safely logged out."
    })
  },

  // Data operation toasts
  saveSuccess: (itemType: string = "item") => {
    toast.success(
      `${itemType} saved successfully`,
      { description: "Your changes have been saved." }
    )
  },

  saveError: (itemType: string = "item", error?: string) => {
    toast.error(
      `Failed to save ${itemType}`,
      { 
        description: error || "Please try again or contact support.",
        duration: 6000
      }
    )
  },

  deleteSuccess: (itemType: string = "item") => {
    toast.success(
      `${itemType} deleted successfully`,
      { description: "The item has been permanently removed." }
    )
  },

  deleteError: (itemType: string = "item", error?: string) => {
    toast.error(
      `Failed to delete ${itemType}`,
      { 
        description: error || "Please try again or contact support.",
        duration: 6000
      }
    )
  },

  // Upload toasts
  uploadProgress: (fileName: string) => {
    return toast.default(
      `Uploading ${fileName}...`,
      { description: "Please wait while your file is being uploaded." }
    )
  },

  uploadSuccess: (fileName: string) => {
    toast.success(
      "File uploaded successfully",
      { description: `${fileName} has been uploaded and is ready for review.` }
    )
  },

  uploadError: (fileName: string, error?: string) => {
    toast.error(
      "Upload failed",
      { 
        description: error || `Failed to upload ${fileName}. Please try again.`,
        duration: 6000
      }
    )
  },

  // Network toasts
  networkError: () => {
    toast.error(
      "Network connection error",
      { 
        description: "Please check your internet connection and try again.",
        duration: 8000,
        action: {
          label: "Retry",
          onClick: () => window.location.reload()
        }
      }
    )
  },

  serverError: () => {
    toast.error(
      "Server error",
      { 
        description: "Something went wrong on our end. Please try again later.",
        duration: 8000
      }
    )
  },

  // Validation toasts
  validationError: (message?: string) => {
    toast.warning(
      "Please check your input",
      { 
        description: message || "Some fields contain invalid information.",
        duration: 5000
      }
    )
  },

  // Permission toasts
  permissionDenied: () => {
    toast.error(
      "Access denied",
      { 
        description: "You don't have permission to perform this action.",
        duration: 6000
      }
    )
  },

  // Generic promise toast for async operations
  asyncOperation: <T,>(
    promise: Promise<T>,
    {
      operationName,
      successMessage,
      errorMessage
    }: {
      operationName: string
      successMessage?: string | ((data: T) => string)
      errorMessage?: string | ((error: any) => string)
    }
  ) => {
    return toast.promise(promise, {
      loading: `${operationName}...`,
      success: successMessage || `${operationName} completed successfully`,
      error: errorMessage || `${operationName} failed`
    })
  }
}

// Enhanced toast component with better styling
interface EnhancedToastProps {
  variant?: ToastVariant
  title: string
  description?: string
  action?: {
    label: string
    onClick: () => void
  }
  onClose?: () => void
}

export function EnhancedToast({
  variant = "default",
  title,
  description,
  action,
  onClose
}: EnhancedToastProps) {
  const { icon: Icon, className } = toastVariants[variant]

  return (
    <div className={cn(
      "relative flex w-full items-start gap-3 rounded-lg border p-4 shadow-lg",
      className
    )}>
      <Icon className="h-5 w-5 flex-shrink-0 mt-0.5" />
      <div className="flex-1 space-y-1">
        <p className="font-medium text-sm leading-5">{title}</p>
        {description && (
          <p className="text-sm opacity-90 leading-5">{description}</p>
        )}
        {action && (
          <button
            onClick={action.onClick}
            className="text-sm font-medium underline hover:no-underline"
          >
            {action.label}
          </button>
        )}
      </div>
      {onClose && (
        <button
          onClick={onClose}
          className="flex-shrink-0 opacity-70 hover:opacity-100 transition-opacity"
        >
          <X className="h-4 w-4" />
        </button>
      )}
    </div>
  )
}