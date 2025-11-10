"use client"

import { useRouter } from "next/navigation"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import TaxFilingForm from "@/components/tax-filing-form"
import { ArrowLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import Link from "next/link"

export default function NewTaxFilingPage() {
  const router = useRouter()

  const handleSuccess = () => {
    router.push("/tax-filings")
  }

  return (
    <div className="container mx-auto py-8">
      <div className="mb-6">
        <Link href="/tax-filings">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Tax Filings
          </Button>
        </Link>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create New Tax Filing</CardTitle>
          <CardDescription>
            Select a client and enter tax filing information. You can calculate tax liability based on taxable amounts.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <TaxFilingForm onSuccess={handleSuccess} />
        </CardContent>
      </Card>
    </div>
  )
}
