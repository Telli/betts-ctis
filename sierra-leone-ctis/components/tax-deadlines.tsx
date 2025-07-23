import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Calendar, Clock, AlertTriangle } from "lucide-react"

export function TaxDeadlines() {
  const deadlines = [
    {
      title: "GST Return Filing",
      date: "Jan 21, 2025",
      daysLeft: 3,
      priority: "high",
      category: "GST",
      description: "21-day filing deadline",
    },
    {
      title: "Payroll Tax Submission",
      date: "Jan 28, 2025",
      daysLeft: 10,
      priority: "medium",
      category: "Payroll",
      description: "Foreign employee documentation",
    },
    {
      title: "Income Tax Assessment",
      date: "Feb 15, 2025",
      daysLeft: 28,
      priority: "medium",
      category: "Income Tax",
      description: "Large taxpayer category",
    },
    {
      title: "Mining Royalty Payment",
      date: "Feb 28, 2025",
      daysLeft: 41,
      priority: "low",
      category: "Mining",
      description: "Quarterly payment due",
    },
  ]

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case "high":
        return "bg-red-100 text-red-800"
      case "medium":
        return "bg-amber-100 text-amber-800"
      case "low":
        return "bg-green-100 text-green-800"
      default:
        return "bg-gray-100 text-gray-800"
    }
  }

  const getPriorityIcon = (priority: string) => {
    switch (priority) {
      case "high":
        return <AlertTriangle className="h-4 w-4 text-red-500" />
      case "medium":
        return <Clock className="h-4 w-4 text-amber-500" />
      case "low":
        return <Calendar className="h-4 w-4 text-green-500" />
      default:
        return <Calendar className="h-4 w-4 text-gray-500" />
    }
  }

  return (
    <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
      <CardHeader>
        <CardTitle className="text-lg font-bold text-gray-900 flex items-center">
          <Calendar className="h-5 w-5 mr-2 text-blue-600" />
          Upcoming Tax Deadlines
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {deadlines.map((deadline, index) => (
            <div key={index} className="border rounded-lg p-4 hover:bg-gray-50 transition-colors">
              <div className="flex items-start justify-between mb-2">
                <div className="flex items-center space-x-2">
                  {getPriorityIcon(deadline.priority)}
                  <h3 className="font-semibold text-gray-900 text-sm">{deadline.title}</h3>
                </div>
                <Badge className={getPriorityColor(deadline.priority)}>{deadline.daysLeft} days</Badge>
              </div>

              <div className="ml-6">
                <p className="text-sm text-gray-600 mb-1">{deadline.date}</p>
                <p className="text-xs text-gray-500">{deadline.description}</p>
                <Badge variant="outline" className="mt-2 text-xs">
                  {deadline.category}
                </Badge>
              </div>
            </div>
          ))}
        </div>

        <div className="mt-4 p-3 bg-blue-50 rounded-lg border border-blue-200">
          <p className="text-sm text-blue-800 font-medium">📅 Sierra Leone Tax Calendar 2025</p>
          <p className="text-xs text-blue-600 mt-1">All deadlines based on Finance Act 2025 requirements</p>
        </div>
      </CardContent>
    </Card>
  )
}
