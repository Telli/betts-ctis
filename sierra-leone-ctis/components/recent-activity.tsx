import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { FileText, DollarSign, Upload, CheckCircle, Clock } from "lucide-react"

export function RecentActivity() {
  const activities = [
    {
      id: 1,
      type: "filing",
      title: "GST Return Filed",
      description: "Freetown Logistics Ltd - Q4 2024 GST return submitted successfully",
      user: "Sarah Bangura",
      time: "2 hours ago",
      status: "completed",
      icon: FileText,
    },
    {
      id: 2,
      type: "payment",
      title: "Payment Approved",
      description: "SLE 125,000 GST payment approved for Atlantic Mining Corp",
      user: "John Kamara",
      time: "4 hours ago",
      status: "approved",
      icon: DollarSign,
    },
    {
      id: 3,
      type: "document",
      title: "Documents Uploaded",
      description: "Payroll tax documents uploaded for Sierra Petroleum Ltd",
      user: "Mohamed Sesay",
      time: "6 hours ago",
      status: "pending",
      icon: Upload,
    },
    {
      id: 4,
      type: "compliance",
      title: "Compliance Check",
      description: "Mining royalty compliance verified for Diamond Mining Co",
      user: "Fatima Koroma",
      time: "1 day ago",
      status: "completed",
      icon: CheckCircle,
    },
    {
      id: 5,
      type: "deadline",
      title: "Deadline Reminder",
      description: "Excise duty filing deadline approaching for 3 clients",
      user: "System",
      time: "1 day ago",
      status: "warning",
      icon: Clock,
    },
  ]

  const getStatusBadge = (status: string) => {
    const variants = {
      completed: "bg-green-100 text-green-800",
      approved: "bg-blue-100 text-blue-800",
      pending: "bg-amber-100 text-amber-800",
      warning: "bg-orange-100 text-orange-800",
    }

    return (
      <Badge className={variants[status as keyof typeof variants]}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    )
  }

  const getIconColor = (type: string) => {
    switch (type) {
      case "filing":
        return "text-blue-600"
      case "payment":
        return "text-green-600"
      case "document":
        return "text-purple-600"
      case "compliance":
        return "text-emerald-600"
      case "deadline":
        return "text-amber-600"
      default:
        return "text-gray-600"
    }
  }

  return (
    <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
      <CardHeader>
        <CardTitle className="text-lg font-bold text-gray-900">Recent Activity</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {activities.map((activity) => (
            <div
              key={activity.id}
              className="flex items-start space-x-4 p-3 rounded-lg hover:bg-gray-50 transition-colors"
            >
              <div className={`p-2 rounded-full bg-gray-100 ${getIconColor(activity.type)}`}>
                <activity.icon className="h-4 w-4" />
              </div>

              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between mb-1">
                  <h3 className="font-semibold text-gray-900 text-sm">{activity.title}</h3>
                  {getStatusBadge(activity.status)}
                </div>
                <p className="text-sm text-gray-600 mb-2">{activity.description}</p>
                <div className="flex items-center space-x-4 text-xs text-gray-500">
                  <div className="flex items-center space-x-2">
                    <Avatar className="h-5 w-5">
                      <AvatarFallback className="text-xs">
                        {activity.user
                          .split(" ")
                          .map((n) => n[0])
                          .join("")}
                      </AvatarFallback>
                    </Avatar>
                    <span>{activity.user}</span>
                  </div>
                  <span>{activity.time}</span>
                </div>
              </div>
            </div>
          ))}
        </div>

        <div className="mt-4 text-center">
          <button className="text-sm text-blue-600 hover:text-blue-800 font-medium">View All Activity</button>
        </div>
      </CardContent>
    </Card>
  )
}
