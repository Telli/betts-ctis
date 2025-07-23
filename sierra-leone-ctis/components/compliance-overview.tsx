import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"
import { Badge } from "@/components/ui/badge"
import { CheckCircle, AlertCircle, Clock, XCircle } from "lucide-react"

export function ComplianceOverview() {
  const complianceItems = [
    {
      category: "GST Returns",
      status: "compliant",
      progress: 100,
      dueDate: "Filed on time",
      description: "21-day filing requirement met",
    },
    {
      category: "Payroll Tax",
      status: "pending",
      progress: 75,
      dueDate: "Due in 5 days",
      description: "Foreign employee documentation pending",
    },
    {
      category: "Income Tax",
      status: "warning",
      progress: 45,
      dueDate: "Due in 12 days",
      description: "Large taxpayer category - requires review",
    },
    {
      category: "Excise Duty",
      status: "overdue",
      progress: 0,
      dueDate: "Overdue by 3 days",
      description: "Mandatory filing requirement - penalty applies",
    },
  ]

  const getStatusIcon = (status: string) => {
    switch (status) {
      case "compliant":
        return <CheckCircle className="h-5 w-5 text-green-500" />
      case "pending":
        return <Clock className="h-5 w-5 text-amber-500" />
      case "warning":
        return <AlertCircle className="h-5 w-5 text-orange-500" />
      case "overdue":
        return <XCircle className="h-5 w-5 text-red-500" />
      default:
        return null
    }
  }

  const getStatusBadge = (status: string) => {
    const variants = {
      compliant: "bg-green-100 text-green-800",
      pending: "bg-amber-100 text-amber-800",
      warning: "bg-orange-100 text-orange-800",
      overdue: "bg-red-100 text-red-800",
    }

    return (
      <Badge className={variants[status as keyof typeof variants]}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    )
  }

  return (
    <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
      <CardHeader>
        <CardTitle className="text-xl font-bold text-gray-900 flex items-center">
          Sierra Leone Tax Compliance Overview
          <Badge className="ml-3 bg-blue-100 text-blue-800">Finance Act 2025</Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-6">
          {complianceItems.map((item, index) => (
            <div key={index} className="border rounded-lg p-4 hover:bg-gray-50 transition-colors">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center space-x-3">
                  {getStatusIcon(item.status)}
                  <h3 className="font-semibold text-gray-900">{item.category}</h3>
                  {getStatusBadge(item.status)}
                </div>
                <span className="text-sm text-gray-600">{item.dueDate}</span>
              </div>

              <div className="mb-2">
                <Progress value={item.progress} className="h-2" />
              </div>

              <p className="text-sm text-gray-600">{item.description}</p>
            </div>
          ))}
        </div>

        <div className="mt-6 p-4 bg-blue-50 rounded-lg border border-blue-200">
          <div className="flex items-center space-x-2">
            <div className="w-12 h-12 bg-blue-600 rounded-full flex items-center justify-center">
              <span className="text-white font-bold text-lg">94</span>
            </div>
            <div>
              <h4 className="font-semibold text-blue-900">Overall Compliance Score</h4>
              <p className="text-sm text-blue-700">Excellent compliance with Sierra Leone tax regulations</p>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
