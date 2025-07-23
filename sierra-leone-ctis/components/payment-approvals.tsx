import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { DollarSign, CheckCircle, XCircle, Clock } from "lucide-react"

export function PaymentApprovals() {
  const payments = [
    {
      id: "PAY-001",
      client: "Sierra Mining Corp",
      amount: "SLE 850,000",
      type: "Mining Royalty",
      status: "pending",
      submittedBy: "John Kamara",
      date: "2025-01-15",
    },
    {
      id: "PAY-002",
      client: "Freetown Logistics Ltd",
      amount: "SLE 125,000",
      type: "GST Payment",
      status: "approved",
      submittedBy: "Sarah Bangura",
      date: "2025-01-14",
    },
    {
      id: "PAY-003",
      client: "Atlantic Petroleum",
      amount: "SLE 2,100,000",
      type: "Excise Duty",
      status: "review",
      submittedBy: "Mohamed Sesay",
      date: "2025-01-13",
    },
  ]

  const getStatusIcon = (status: string) => {
    switch (status) {
      case "approved":
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case "pending":
        return <Clock className="h-4 w-4 text-amber-500" />
      case "review":
        return <XCircle className="h-4 w-4 text-red-500" />
      default:
        return null
    }
  }

  const getStatusBadge = (status: string) => {
    const variants = {
      approved: "bg-green-100 text-green-800",
      pending: "bg-amber-100 text-amber-800",
      review: "bg-red-100 text-red-800",
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
        <CardTitle className="text-lg font-bold text-gray-900 flex items-center">
          <DollarSign className="h-5 w-5 mr-2 text-blue-600" />
          Payment Approvals
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {payments.map((payment, index) => (
            <div key={index} className="border rounded-lg p-4 hover:bg-gray-50 transition-colors">
              <div className="flex items-start justify-between mb-3">
                <div>
                  <div className="flex items-center space-x-2 mb-1">
                    {getStatusIcon(payment.status)}
                    <h3 className="font-semibold text-gray-900 text-sm">{payment.client}</h3>
                  </div>
                  <p className="text-lg font-bold text-blue-600">{payment.amount}</p>
                  <p className="text-xs text-gray-500">{payment.type}</p>
                </div>
                {getStatusBadge(payment.status)}
              </div>

              <div className="flex items-center justify-between text-xs text-gray-500 mb-3">
                <span>By: {payment.submittedBy}</span>
                <span>{payment.date}</span>
              </div>

              {payment.status === "pending" && (
                <div className="flex space-x-2">
                  <Button size="sm" className="flex-1 bg-green-600 hover:bg-green-700">
                    Approve
                  </Button>
                  <Button size="sm" variant="outline" className="flex-1">
                    Review
                  </Button>
                </div>
              )}
            </div>
          ))}
        </div>

        <Button className="w-full mt-4 bg-blue-600 hover:bg-blue-700">View All Payments</Button>
      </CardContent>
    </Card>
  )
}
