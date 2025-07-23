import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { FileText, Upload, Calculator, Users, Download, Plus } from "lucide-react"

export function QuickActions() {
  const actions = [
    {
      title: "New GST Return",
      description: "File GST return for current period",
      icon: FileText,
      color: "bg-blue-600 hover:bg-blue-700",
    },
    {
      title: "Upload Documents",
      description: "Add tax documents and receipts",
      icon: Upload,
      color: "bg-green-600 hover:bg-green-700",
    },
    {
      title: "Tax Calculator",
      description: "Calculate tax liability by category",
      icon: Calculator,
      color: "bg-purple-600 hover:bg-purple-700",
    },
    {
      title: "Add Client",
      description: "Register new taxpayer",
      icon: Users,
      color: "bg-amber-600 hover:bg-amber-700",
    },
    {
      title: "Generate Report",
      description: "Create compliance report",
      icon: Download,
      color: "bg-indigo-600 hover:bg-indigo-700",
    },
    {
      title: "New Payment",
      description: "Submit payment for approval",
      icon: Plus,
      color: "bg-emerald-600 hover:bg-emerald-700",
    },
  ]

  return (
    <Card className="backdrop-blur-sm bg-white/90 border-0 shadow-lg">
      <CardHeader>
        <CardTitle className="text-lg font-bold text-gray-900">Quick Actions</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-2 gap-3">
          {actions.map((action, index) => (
            <Button
              key={index}
              variant="outline"
              className={`h-auto p-4 flex flex-col items-center space-y-2 text-white border-0 ${action.color}`}
            >
              <action.icon className="h-6 w-6" />
              <div className="text-center">
                <p className="font-semibold text-xs">{action.title}</p>
                <p className="text-xs opacity-90">{action.description}</p>
              </div>
            </Button>
          ))}
        </div>

        <div className="mt-4 p-3 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200">
          <p className="text-sm font-medium text-blue-900">🇸🇱 Sierra Leone Features</p>
          <p className="text-xs text-blue-700 mt-1">All actions comply with Finance Act 2025 and NRA requirements</p>
        </div>
      </CardContent>
    </Card>
  )
}
