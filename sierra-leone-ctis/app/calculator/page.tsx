'use client'

import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { 
  Calculator, 
  DollarSign, 
  TrendingUp, 
  FileText, 
  CheckCircle,
  Info,
  Download,
  Save
} from 'lucide-react'

interface TaxCalculation {
  grossIncome: number
  taxableIncome: number
  taxLiability: number
  effectiveRate: number
  marginalRate: number
  deductions: number
  exemptions: number
}

export default function TaxCalculatorPage() {
  const [activeTab, setActiveTab] = useState('income-tax')
  const [grossIncome, setGrossIncome] = useState<number>(0)
  const [deductions, setDeductions] = useState<number>(0)
  const [exemptions, setExemptions] = useState<number>(0)
  const [calculation, setCalculation] = useState<TaxCalculation | null>(null)
  const [businessCategory, setBusinessCategory] = useState('large')
  const [gstTurnover, setGstTurnover] = useState<number>(0)
  const [loading, setLoading] = useState(false)

  // Sierra Leone Income Tax Rates 2025
  const incomeTaxBrackets = [
    { min: 0, max: 600000, rate: 0 }, // Tax-free threshold
    { min: 600000, max: 1800000, rate: 15 },
    { min: 1800000, max: 3600000, rate: 20 },
    { min: 3600000, max: 7200000, rate: 25 },
    { min: 7200000, max: Infinity, rate: 30 }
  ]

  const calculateIncomeTax = () => {
    setLoading(true)
    
    const taxableIncome = Math.max(0, grossIncome - deductions - exemptions)
    let taxLiability = 0
    let marginalRate = 0

    for (const bracket of incomeTaxBrackets) {
      if (taxableIncome > bracket.min) {
        const taxableAtThisBracket = Math.min(taxableIncome - bracket.min, bracket.max - bracket.min)
        taxLiability += taxableAtThisBracket * (bracket.rate / 100)
        marginalRate = bracket.rate
      }
    }

    const effectiveRate = grossIncome > 0 ? (taxLiability / grossIncome) * 100 : 0

    setCalculation({
      grossIncome,
      taxableIncome,
      taxLiability,
      effectiveRate,
      marginalRate,
      deductions,
      exemptions
    })

    setLoading(false)
  }

  const calculateGST = () => {
    setLoading(true)
    
    const gstRate = 15 // 15% GST rate in Sierra Leone
    const gstAmount = (gstTurnover * gstRate) / 100
    
    setCalculation({
      grossIncome: gstTurnover,
      taxableIncome: gstTurnover,
      taxLiability: gstAmount,
      effectiveRate: gstRate,
      marginalRate: gstRate,
      deductions: 0,
      exemptions: 0
    })

    setLoading(false)
  }

  const getBusinessCategoryInfo = () => {
    switch (businessCategory) {
      case 'large':
        return { threshold: '> Le 2 billion', description: 'Large taxpayer with comprehensive compliance requirements' }
      case 'medium':
        return { threshold: 'Le 500M - 2B', description: 'Medium taxpayer with standard compliance requirements' }
      case 'small':
        return { threshold: 'Le 50M - 500M', description: 'Small taxpayer with simplified compliance' }
      case 'micro':
        return { threshold: '< Le 50M', description: 'Micro taxpayer with basic compliance requirements' }
      default:
        return { threshold: '', description: '' }
    }
  }

  const formatCurrency = (amount: number) => {
    return `Le ${amount.toLocaleString()}`
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Tax Calculator</h1>
          <p className="text-muted-foreground mt-2">
            Calculate your tax liability based on Sierra Leone Finance Act 2025
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline">
            <Save className="mr-2 h-4 w-4" />
            Save Calculation
          </Button>
          <Button>
            <Download className="mr-2 h-4 w-4" />
            Export PDF
          </Button>
        </div>
      </div>

      {/* Tax Calculators */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="income-tax">Income Tax</TabsTrigger>
          <TabsTrigger value="gst">GST</TabsTrigger>
          <TabsTrigger value="payroll">Payroll Tax</TabsTrigger>
          <TabsTrigger value="business">Business Tax</TabsTrigger>
        </TabsList>

        <TabsContent value="income-tax" className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-2">
            {/* Input Form */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Calculator className="h-5 w-5" />
                  Income Tax Calculator
                </CardTitle>
                <CardDescription>
                  Calculate your annual income tax liability
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="grossIncome">Gross Annual Income (Le)</Label>
                  <Input
                    id="grossIncome"
                    type="number"
                    placeholder="Enter your gross income..."
                    value={grossIncome || ''}
                    onChange={(e) => setGrossIncome(Number(e.target.value))}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="deductions">Allowable Deductions (Le)</Label>
                  <Input
                    id="deductions"
                    type="number"
                    placeholder="Enter deductions..."
                    value={deductions || ''}
                    onChange={(e) => setDeductions(Number(e.target.value))}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="exemptions">Personal Exemptions (Le)</Label>
                  <Input
                    id="exemptions"
                    type="number"
                    placeholder="Enter exemptions..."
                    value={exemptions || ''}
                    onChange={(e) => setExemptions(Number(e.target.value))}
                  />
                </div>

                <Button 
                  onClick={calculateIncomeTax} 
                  className="w-full"
                  disabled={loading}
                >
                  {loading ? 'Calculating...' : 'Calculate Income Tax'}
                </Button>
              </CardContent>
            </Card>

            {/* Results */}
            <Card>
              <CardHeader>
                <CardTitle>Tax Calculation Results</CardTitle>
                <CardDescription>
                  Based on Sierra Leone Finance Act 2025
                </CardDescription>
              </CardHeader>
              <CardContent>
                {calculation ? (
                  <div className="space-y-4">
                    <div className="grid gap-4">
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Gross Income:</span>
                        <span className="font-medium">{formatCurrency(calculation.grossIncome)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Less: Deductions:</span>
                        <span className="font-medium">({formatCurrency(calculation.deductions)})</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Less: Exemptions:</span>
                        <span className="font-medium">({formatCurrency(calculation.exemptions)})</span>
                      </div>
                      <hr />
                      <div className="flex justify-between">
                        <span className="font-medium">Taxable Income:</span>
                        <span className="font-bold">{formatCurrency(calculation.taxableIncome)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="font-medium">Tax Liability:</span>
                        <span className="font-bold text-sierra-blue-600 text-lg">
                          {formatCurrency(calculation.taxLiability)}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Effective Rate:</span>
                        <span className="font-medium">{calculation.effectiveRate.toFixed(2)}%</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Marginal Rate:</span>
                        <span className="font-medium">{calculation.marginalRate}%</span>
                      </div>
                    </div>

                    <div className="pt-4 border-t">
                      <div className="flex items-center gap-2 text-green-600 mb-2">
                        <CheckCircle className="h-4 w-4" />
                        <span className="font-medium">Calculation Complete</span>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        This calculation is based on the current Sierra Leone tax rates. 
                        Please consult with a tax professional for complex situations.
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <Calculator className="mx-auto h-12 w-12 text-muted-foreground" />
                    <p className="text-muted-foreground mt-2">
                      Enter your income details to calculate tax liability
                    </p>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Tax Brackets Reference */}
          <Card>
            <CardHeader>
              <CardTitle>Sierra Leone Income Tax Brackets 2025</CardTitle>
              <CardDescription>
                Current tax rates as per Sierra Leone Finance Act 2025
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid gap-2">
                {incomeTaxBrackets.map((bracket, index) => (
                  <div key={index} className="flex justify-between items-center p-3 border rounded">
                    <div>
                      <span className="font-medium">
                        {bracket.max === Infinity 
                          ? `Over ${formatCurrency(bracket.min)}`
                          : `${formatCurrency(bracket.min)} - ${formatCurrency(bracket.max)}`
                        }
                      </span>
                    </div>
                    <Badge variant={bracket.rate === 0 ? "secondary" : "default"}>
                      {bracket.rate}%
                    </Badge>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="gst" className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>GST Calculator</CardTitle>
                <CardDescription>
                  Calculate Goods and Services Tax (15% rate)
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="gstTurnover">Annual Turnover (Le)</Label>
                  <Input
                    id="gstTurnover"
                    type="number"
                    placeholder="Enter annual turnover..."
                    value={gstTurnover || ''}
                    onChange={(e) => setGstTurnover(Number(e.target.value))}
                  />
                </div>

                <Button onClick={calculateGST} className="w-full" disabled={loading}>
                  {loading ? 'Calculating...' : 'Calculate GST'}
                </Button>

                <div className="p-4 bg-sierra-blue-50 border border-sierra-blue-200 rounded">
                  <div className="flex items-center gap-2 mb-2">
                    <Info className="h-4 w-4 text-sierra-blue-600" />
                    <span className="font-medium text-sierra-blue-800">GST Information</span>
                  </div>
                  <p className="text-sm text-sierra-blue-700">
                    GST registration is mandatory for businesses with annual turnover exceeding Le 500 million.
                    The standard rate is 15% on most goods and services.
                  </p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>GST Calculation Results</CardTitle>
              </CardHeader>
              <CardContent>
                {calculation && activeTab === 'gst' ? (
                  <div className="space-y-4">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Annual Turnover:</span>
                      <span className="font-medium">{formatCurrency(calculation.grossIncome)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">GST Rate:</span>
                      <span className="font-medium">15%</span>
                    </div>
                    <hr />
                    <div className="flex justify-between">
                      <span className="font-medium">GST Liability:</span>
                      <span className="font-bold text-sierra-blue-600 text-lg">
                        {formatCurrency(calculation.taxLiability)}
                      </span>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <DollarSign className="mx-auto h-12 w-12 text-muted-foreground" />
                    <p className="text-muted-foreground mt-2">
                      Calculate GST on your business turnover
                    </p>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="payroll" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Payroll Tax Calculator</CardTitle>
              <CardDescription>
                Calculate employer payroll taxes and employee deductions
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground text-center py-8">
                Payroll tax calculator will be implemented here
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="business" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Business Tax Planning</CardTitle>
              <CardDescription>
                Estimate business taxes based on company size and type
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>Business Category</Label>
                <select 
                  value={businessCategory} 
                  onChange={(e) => setBusinessCategory(e.target.value)}
                  className="w-full p-2 border rounded"
                >
                  <option value="large">Large Taxpayer</option>
                  <option value="medium">Medium Taxpayer</option>
                  <option value="small">Small Taxpayer</option>
                  <option value="micro">Micro Taxpayer</option>
                </select>
              </div>

              <div className="p-4 bg-gray-50 border rounded">
                <div className="flex items-center gap-2 mb-2">
                  <TrendingUp className="h-4 w-4" />
                  <span className="font-medium">{businessCategory.charAt(0).toUpperCase() + businessCategory.slice(1)} Taxpayer</span>
                </div>
                <p className="text-sm text-muted-foreground mb-1">
                  Threshold: {getBusinessCategoryInfo().threshold}
                </p>
                <p className="text-sm text-muted-foreground">
                  {getBusinessCategoryInfo().description}
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Sierra Leone Notice */}
      <Card className="border-sierra-blue-200 bg-sierra-blue-50">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <span className="text-lg">🇸🇱</span>
            Sierra Leone Finance Act 2025 Compliance
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <h4 className="font-medium text-sierra-blue-800">Income Tax</h4>
              <p className="text-sm text-sierra-blue-700">Progressive rates from 0% to 30%</p>
            </div>
            <div>
              <h4 className="font-medium text-sierra-blue-800">GST</h4>
              <p className="text-sm text-sierra-blue-700">Standard rate of 15% on most items</p>
            </div>
            <div>
              <h4 className="font-medium text-sierra-blue-800">Compliance</h4>
              <p className="text-sm text-sierra-blue-700">Regular filing and payment requirements</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}