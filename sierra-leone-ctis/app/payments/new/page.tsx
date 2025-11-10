"use client"

import { useRouter } from "next/navigation"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import PaymentForm from "@/components/payment-form"
import { ArrowLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import Link from "next/link"

export default function NewPaymentPage() {
  const router = useRouter()

  const handleSuccess = () => {
    router.push("/payments")
  }

  return (
    <div className="container mx-auto py-8">
      <div className="mb-6">
        <Link href="/payments">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Payments
          </Button>
        </Link>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Record New Payment</CardTitle>
          <CardDescription>
            Select a client and record their payment. You can link the payment to a specific tax filing or record it as a general payment.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <PaymentForm onSuccess={handleSuccess} />
        </CardContent>
      </Card>
    </div>
  )
}
