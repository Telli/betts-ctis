"use client"

import React, { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ArrowLeft, Calculator, Info } from 'lucide-react'
import Link from 'next/link'
import { toast } from '@/components/ui/enhanced-toast'

export default function BasicTaxCalculator() {
  const [incomeTaxData, setIncomeTaxData] = useState({
    taxableIncome: 0,
    taxpayerCategory: '',
    isIndividual: true
  })

  const [gstData, setGstData] = useState({
    taxableAmount: 0,
    itemCategory: 'standard'
  })

  const [withholdingTaxData, setWithholdingTaxData] = useState({
    amount: 0,
    withholdingTaxType: '',
    isResident: true
  })

  const [payeData, setPayeData] = useState({
    grossSalary: 0,
    allowances: 0
  })

  const [results, setResults] = useState<any>({})
  const [isLoading, setIsLoading] = useState(false)

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-SL', {
      style: 'currency',
      currency: 'SLE',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount)
  }

  const calculateIncomeTax = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/taxcalculation/income-tax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(incomeTaxData)
      })

      if (!response.ok) throw new Error('Failed to calculate income tax')

      const data = await response.json()
      setResults((prev: any) => ({ ...prev, incomeTax: data }))
      toast.success('Income tax calculated successfully')
    } catch (error) {
      toast.error('Failed to calculate income tax')
    } finally {
      setIsLoading(false)
    }
  }

  const calculateGST = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/taxcalculation/gst', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(gstData)
      })

      if (!response.ok) throw new Error('Failed to calculate GST')

      const data = await response.json()
      setResults((prev: any) => ({ ...prev, gst: data }))
      toast.success('GST calculated successfully')
    } catch (error) {
      toast.error('Failed to calculate GST')
    } finally {
      setIsLoading(false)
    }
  }

  const calculateWithholdingTax = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/taxcalculation/withholding-tax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(withholdingTaxData)
      })

      if (!response.ok) throw new Error('Failed to calculate withholding tax')

      const data = await response.json()
      setResults((prev: any) => ({ ...prev, withholdingTax: data }))
      toast.success('Withholding tax calculated successfully')
    } catch (error) {
      toast.error('Failed to calculate withholding tax')
    } finally {
      setIsLoading(false)
    }
  }

  const calculatePAYE = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/taxcalculation/paye', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payeData)
      })

      if (!response.ok) throw new Error('Failed to calculate PAYE')

      const data = await response.json()
      setResults((prev: any) => ({ ...prev, paye: data }))
      toast.success('PAYE calculated successfully')
    } catch (error) {
      toast.error('Failed to calculate PAYE')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-4 mb-4">
          <Button variant="ghost" size="sm" asChild>
            <Link href="/calculators">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Calculators
            </Link>
          </Button>
          <Badge variant="outline">Standard Rates</Badge>
        </div>
        
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Basic Tax Calculators
        </h1>
        <p className="text-lg text-gray-600">
          Calculate standard Sierra Leone taxes including income tax, GST, withholding tax, and PAYE
        </p>
      </div>

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Calculators */}
        <div className="lg:col-span-2">
          <Tabs defaultValue="income-tax" className="space-y-6">
            <TabsList className="grid w-full grid-cols-4">
              <TabsTrigger value="income-tax">Income Tax</TabsTrigger>
              <TabsTrigger value="gst">GST</TabsTrigger>
              <TabsTrigger value="withholding">Withholding</TabsTrigger>
              <TabsTrigger value="paye">PAYE</TabsTrigger>
            </TabsList>

            {/* Income Tax Calculator */}
            <TabsContent value="income-tax">
              <Card>
                <CardHeader>
                  <CardTitle>Income Tax Calculator</CardTitle>
                  <CardDescription>
                    Calculate income tax for individuals (progressive rates) or corporations (25% flat rate)
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <Label htmlFor="taxableIncome">Taxable Income (SLE)</Label>
                    <Input
                      id="taxableIncome"
                      type="number"
                      value={incomeTaxData.taxableIncome || ''}
                      onChange={(e) => setIncomeTaxData(prev => ({ 
                        ...prev, 
                        taxableIncome: Number(e.target.value) 
                      }))}
                      placeholder="Enter taxable income"
                    />
                  </div>

                  <div className="grid gap-4 md:grid-cols-2">
                    <div>
                      <Label htmlFor="taxpayerType">Taxpayer Type</Label>
                      <Select 
                        value={incomeTaxData.isIndividual ? 'individual' : 'corporate'}
                        onValueChange={(value) => setIncomeTaxData(prev => ({ 
                          ...prev, 
                          isIndividual: value === 'individual' 
                        }))}
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="individual">Individual</SelectItem>
                          <SelectItem value="corporate">Corporate</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div>
                      <Label htmlFor="taxpayerCategory">Taxpayer Category</Label>
                      <Select 
                        value={incomeTaxData.taxpayerCategory}
                        onValueChange={(value) => setIncomeTaxData(prev => ({ 
                          ...prev, 
                          taxpayerCategory: value 
                        }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select category" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Large">Large</SelectItem>
                          <SelectItem value="Medium">Medium</SelectItem>
                          <SelectItem value="Small">Small</SelectItem>
                          <SelectItem value="Micro">Micro</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <Button 
                    onClick={calculateIncomeTax}
                    disabled={isLoading || !incomeTaxData.taxableIncome}
                    className="w-full"
                  >
                    Calculate Income Tax
                  </Button>

                  {/* Tax Brackets Info */}
                  <div className="mt-4 p-4 bg-blue-50 rounded-lg">
                    <h4 className="font-medium mb-2">Individual Tax Brackets (2024)</h4>
                    <div className="text-sm space-y-1">
                      <div>First 600,000 SLE: <span className="font-medium">0%</span></div>
                      <div>Next 600,000 SLE: <span className="font-medium">15%</span></div>
                      <div>Next 600,000 SLE: <span className="font-medium">20%</span></div>
                      <div>Next 600,000 SLE: <span className="font-medium">25%</span></div>
                      <div>Above 2,400,000 SLE: <span className="font-medium">30%</span></div>
                    </div>
                    <div className="mt-2 text-sm">
                      <strong>Corporate Rate:</strong> 25% flat rate
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* GST Calculator */}
            <TabsContent value="gst">
              <Card>
                <CardHeader>
                  <CardTitle>GST Calculator</CardTitle>
                  <CardDescription>
                    Calculate Goods and Services Tax at 15% standard rate
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <Label htmlFor="gstTaxableAmount">Taxable Amount (SLE)</Label>
                    <Input
                      id="gstTaxableAmount"
                      type="number"
                      value={gstData.taxableAmount || ''}
                      onChange={(e) => setGstData(prev => ({ 
                        ...prev, 
                        taxableAmount: Number(e.target.value) 
                      }))}
                      placeholder="Enter taxable amount"
                    />
                  </div>

                  <div>
                    <Label htmlFor="itemCategory">Item Category</Label>
                    <Select 
                      value={gstData.itemCategory}
                      onValueChange={(value) => setGstData(prev => ({ 
                        ...prev, 
                        itemCategory: value 
                      }))}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="standard">Standard (15%)</SelectItem>
                        <SelectItem value="exempt">Exempt (0%)</SelectItem>
                        <SelectItem value="zero-rated">Zero-rated (0%)</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <Button 
                    onClick={calculateGST}
                    disabled={isLoading || !gstData.taxableAmount}
                    className="w-full"
                  >
                    Calculate GST
                  </Button>

                  <div className="mt-4 p-4 bg-green-50 rounded-lg">
                    <h4 className="font-medium mb-2">GST Information</h4>
                    <div className="text-sm space-y-1">
                      <div><strong>Standard Rate:</strong> 15%</div>
                      <div><strong>Registration Threshold:</strong> 350,000 SLE annually</div>
                      <div><strong>Filing:</strong> Monthly returns due by last day of following month</div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Withholding Tax Calculator */}
            <TabsContent value="withholding">
              <Card>
                <CardHeader>
                  <CardTitle>Withholding Tax Calculator</CardTitle>
                  <CardDescription>
                    Calculate withholding tax based on Finance Act 2024 rates (increased to 15%)
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <Label htmlFor="withholdingAmount">Payment Amount (SLE)</Label>
                    <Input
                      id="withholdingAmount"
                      type="number"
                      value={withholdingTaxData.amount || ''}
                      onChange={(e) => setWithholdingTaxData(prev => ({ 
                        ...prev, 
                        amount: Number(e.target.value) 
                      }))}
                      placeholder="Enter payment amount"
                    />
                  </div>

                  <div className="grid gap-4 md:grid-cols-2">
                    <div>
                      <Label htmlFor="withholdingType">Payment Type</Label>
                      <Select 
                        value={withholdingTaxData.withholdingTaxType}
                        onValueChange={(value) => setWithholdingTaxData(prev => ({ 
                          ...prev, 
                          withholdingTaxType: value 
                        }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select payment type" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Dividends">Dividends (15%)</SelectItem>
                          <SelectItem value="ManagementFees">Management Fees (15%)</SelectItem>
                          <SelectItem value="ProfessionalFees">Professional Fees (15%)</SelectItem>
                          <SelectItem value="LotteryWinnings">Lottery Winnings (15%)</SelectItem>
                          <SelectItem value="Rent">Rent (10%)</SelectItem>
                          <SelectItem value="Commissions">Commissions (5%)</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div>
                      <Label htmlFor="residencyStatus">Recipient Status</Label>
                      <Select 
                        value={withholdingTaxData.isResident ? 'resident' : 'non-resident'}
                        onValueChange={(value) => setWithholdingTaxData(prev => ({ 
                          ...prev, 
                          isResident: value === 'resident' 
                        }))}
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="resident">Resident</SelectItem>
                          <SelectItem value="non-resident">Non-resident</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <Button 
                    onClick={calculateWithholdingTax}
                    disabled={isLoading || !withholdingTaxData.amount || !withholdingTaxData.withholdingTaxType}
                    className="w-full"
                  >
                    Calculate Withholding Tax
                  </Button>

                  <div className="mt-4 p-4 bg-yellow-50 rounded-lg">
                    <h4 className="font-medium mb-2">Finance Act 2024 Changes</h4>
                    <div className="text-sm">
                      Withholding tax rates increased from 10% to 15% for dividends, 
                      management fees, professional fees, and lottery winnings.
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* PAYE Calculator */}
            <TabsContent value="paye">
              <Card>
                <CardHeader>
                  <CardTitle>PAYE Calculator</CardTitle>
                  <CardDescription>
                    Calculate Pay As You Earn tax for employees using progressive tax rates
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <Label htmlFor="grossSalary">Gross Monthly Salary (SLE)</Label>
                    <Input
                      id="grossSalary"
                      type="number"
                      value={payeData.grossSalary || ''}
                      onChange={(e) => setPayeData(prev => ({ 
                        ...prev, 
                        grossSalary: Number(e.target.value) 
                      }))}
                      placeholder="Enter gross salary"
                    />
                  </div>

                  <div>
                    <Label htmlFor="allowances">Monthly Allowances (SLE)</Label>
                    <Input
                      id="allowances"
                      type="number"
                      value={payeData.allowances || ''}
                      onChange={(e) => setPayeData(prev => ({ 
                        ...prev, 
                        allowances: Number(e.target.value) 
                      }))}
                      placeholder="Enter allowances (optional)"
                    />
                  </div>

                  <Button 
                    onClick={calculatePAYE}
                    disabled={isLoading || !payeData.grossSalary}
                    className="w-full"
                  >
                    Calculate PAYE
                  </Button>

                  <div className="mt-4 p-4 bg-purple-50 rounded-lg">
                    <h4 className="font-medium mb-2">PAYE Information</h4>
                    <div className="text-sm space-y-1">
                      <div><strong>Filing:</strong> Monthly by 15th of following month</div>
                      <div><strong>Calculation:</strong> Uses same progressive rates as individual income tax</div>
                      <div><strong>Employer Responsibility:</strong> Deduct and remit to NRA</div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>

        {/* Results Panel */}
        <div className="lg:col-span-1">
          <Card className="sticky top-8">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Calculator className="h-5 w-5" />
                Calculation Results
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {results.incomeTax && (
                  <div className="p-3 bg-blue-50 rounded-lg">
                    <h4 className="font-medium text-blue-900">Income Tax</h4>
                    <p className="text-lg font-bold text-blue-700">
                      {formatCurrency(results.incomeTax.taxAmount)}
                    </p>
                  </div>
                )}

                {results.gst && (
                  <div className="p-3 bg-green-50 rounded-lg">
                    <h4 className="font-medium text-green-900">GST ({results.gst.rate})</h4>
                    <p className="text-lg font-bold text-green-700">
                      {formatCurrency(results.gst.gstAmount)}
                    </p>
                  </div>
                )}

                {results.withholdingTax && (
                  <div className="p-3 bg-yellow-50 rounded-lg">
                    <h4 className="font-medium text-yellow-900">Withholding Tax</h4>
                    <p className="text-lg font-bold text-yellow-700">
                      {formatCurrency(results.withholdingTax.withholdingTaxAmount)}
                    </p>
                  </div>
                )}

                {results.paye && (
                  <div className="p-3 bg-purple-50 rounded-lg">
                    <h4 className="font-medium text-purple-900">PAYE</h4>
                    <p className="text-lg font-bold text-purple-700">
                      {formatCurrency(results.paye.payeAmount)}
                    </p>
                  </div>
                )}

                {Object.keys(results).length === 0 && (
                  <div className="text-center py-8 text-gray-500">
                    <Info className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p className="text-sm">
                      Calculate taxes using the tabs on the left to see results here
                    </p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}