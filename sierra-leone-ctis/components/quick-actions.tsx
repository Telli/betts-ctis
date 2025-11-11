'use client'

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import {
  FileText, Upload, Calculator, Users, Download, Plus,
  Shield, MessageSquare, HelpCircle, BarChart, Settings,
  DollarSign, Workflow, User, AlertCircle
} from "lucide-react"
import { DashboardService, QuickActionsResponse } from "@/lib/services/dashboard-service"

const iconMap: Record<string, React.ComponentType<{ className?: string }>> = {
  FileText,
  Upload,
  Calculator,
  Users,
  Download,
  Plus,
  Shield,
  MessageSquare,
  HelpCircle,
  BarChart,
  Settings,
  DollarSign,
  Workflow,
  User
}

export function QuickActions() {
  const router = useRouter()
  const [quickActions, setQuickActions] = useState<QuickActionsResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchQuickActions = async () => {
      try {
        setError(null)
        const actions = await DashboardService.getQuickActions()
        setQuickActions(actions)
      } catch (error) {
        setError('Failed to load quick actions. Please try again.')
      } finally {
        setLoading(false)
      }
    }

    fetchQuickActions()
  }, [])

  const handleActionClick = (action: string) => {
    router.push(action)
  }

  const handleRetry = () => {
    setLoading(true)
    setError(null)
    // Trigger re-fetch by forcing useEffect to run
    window.location.reload()
  }

  if (loading) {
    return (
      <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
        <CardHeader>
          <CardTitle className="text-lg font-bold text-gray-900">Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="h-24 bg-gray-200 animate-pulse rounded-lg" />
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
        <CardHeader>
          <CardTitle className="text-lg font-bold text-gray-900">Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-6 space-y-3">
            <AlertCircle className="h-12 w-12 text-red-500" />
            <p className="text-sm text-gray-600 text-center">{error}</p>
            <Button
              onClick={handleRetry}
              variant="outline"
              className="mt-2"
            >
              Retry
            </Button>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!quickActions || quickActions.actions.length === 0) {
    return (
      <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
        <CardHeader>
          <CardTitle className="text-lg font-bold text-gray-900">Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-gray-600">No actions available at this time.</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
      <CardHeader>
        <CardTitle className="text-lg font-bold text-gray-900">
          Quick Actions
          {quickActions.userRole && (
            <span className="ml-2 text-xs text-gray-500 font-normal">
              ({quickActions.userRole})
            </span>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-2 gap-3">
          {quickActions.actions.map((action) => {
            const IconComponent = iconMap[action.icon] || FileText
            return (
              <Button
                key={`${action.action}-${action.title}`}
                variant="outline"
                disabled={!action.enabled}
                onClick={() => handleActionClick(action.action)}
                className={`h-auto p-4 flex flex-col items-center space-y-2 text-white border-0 transition-all hover:scale-105 ${
                  action.enabled
                    ? action.color
                    : 'bg-gray-400 cursor-not-allowed opacity-60'
                }`}
              >
                <IconComponent className="h-6 w-6" />
                <div className="text-center">
                  <p className="font-semibold text-xs">{action.title}</p>
                  <p className="text-xs opacity-90">{action.description}</p>
                </div>
              </Button>
            )
          })}
        </div>

        {/* Display counts if available */}
        {Object.keys(quickActions.counts).length > 0 && (
          <div className="mt-4 p-3 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200">
            <p className="text-sm font-medium text-blue-900">Quick Stats</p>
            <div className="flex flex-wrap gap-3 mt-2">
              {Object.entries(quickActions.counts).map(([key, value]) => (
                <div key={key} className="text-xs">
                  <span className="text-blue-700 capitalize">
                    {key.replace(/([A-Z])/g, ' $1').trim().replace(/^\w/, (c) => c.toUpperCase())}:
                  </span>{' '}
                  <span className="font-semibold text-blue-900">{value}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="mt-4 p-3 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200">
          <p className="text-sm font-medium text-blue-900">🇸🇱 Sierra Leone Features</p>
          <p className="text-xs text-blue-700 mt-1">All actions comply with Finance Act 2025 and NRA requirements</p>
        </div>
      </CardContent>
    </Card>
  )
}
