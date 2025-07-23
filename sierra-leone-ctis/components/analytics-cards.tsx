import { Card, CardContent } from "@/components/ui/card"
import { TrendingUp, TrendingDown, DollarSign, Users, FileText, AlertTriangle } from "lucide-react"

export function AnalyticsCards() {
  const stats = [
    {
      title: "Total Tax Liability",
      value: "SLE 2,450,000",
      change: "+12.5%",
      trend: "up",
      icon: DollarSign,
      color: "text-blue-600",
    },
    {
      title: "Active Clients",
      value: "156",
      change: "+8.2%",
      trend: "up",
      icon: Users,
      color: "text-green-600",
    },
    {
      title: "Pending Filings",
      value: "23",
      change: "-15.3%",
      trend: "down",
      icon: FileText,
      color: "text-amber-600",
    },
    {
      title: "Compliance Score",
      value: "94.2%",
      change: "+2.1%",
      trend: "up",
      icon: AlertTriangle,
      color: "text-emerald-600",
    },
  ]

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      {stats.map((stat, index) => (
        <Card
          key={index}
          className="backdrop-blur-sm bg-white/80 border-0 shadow-lg hover:shadow-xl transition-all duration-300"
        >
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">{stat.title}</p>
                <p className="text-2xl font-bold text-gray-900 mt-1">{stat.value}</p>
                <div className="flex items-center mt-2">
                  {stat.trend === "up" ? (
                    <TrendingUp className="h-4 w-4 text-green-500 mr-1" />
                  ) : (
                    <TrendingDown className="h-4 w-4 text-red-500 mr-1" />
                  )}
                  <span className={`text-sm font-medium ${stat.trend === "up" ? "text-green-600" : "text-red-600"}`}>
                    {stat.change}
                  </span>
                </div>
              </div>
              <div className={`p-3 rounded-full bg-gray-50 ${stat.color}`}>
                <stat.icon className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
