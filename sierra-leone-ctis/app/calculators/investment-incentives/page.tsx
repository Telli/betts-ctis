"use client"

import React, { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Switch } from '@/components/ui/switch'
import { ArrowLeft, Calculator, CheckCircle, XCircle, DollarSign, TrendingUp } from 'lucide-react'
import Link from 'next/link'
import { toast } from '@/components/ui/enhanced-toast'

interface InvestmentIncentiveForm {
  businessName: string
  investmentAmount: number
  employeeCount: number
  localOwnershipPercentage: number
  businessSector: string
  isNewBusiness: boolean
  annualRevenue: number
  estimatedCorporateTax: number
  cultivatedLandHectares: number
  livestockCount: number
  machineryImportValue: number
  renewableEnergyEquipmentValue: number
  rAndDExpenses: number
}

interface IncentiveResult {
  businessName: string
  investmentAmount: number
  employeeCount: number
  localOwnershipPercentage: number
  businessSector: string
  employmentBasedExemption?: {
    isEligible: boolean
    exemptionYears: number
    exemptionType: string
    requirements: string
    estimatedAnnualSavings: number
    reason?: string
  }
  agribusinessExemption?: {
    isEligible: boolean
    exemptionType: string
    requirements: string
    estimatedAnnualSavings: number
    qualifyingActivities?: string[]
    reason?: string
  }
  renewableEnergyExemption?: {
    isEligible: boolean
    exemptionType: string
    requirements: string
    estimatedAnnualSavings: number
    qualifyingEquipment?: string[]
    reason?: string
  }
  dutyFreeImportProvisions?: Array<{
    type: string
    durationYears: number
    requirements: string
    estimatedSavings: number
    qualifyingItems: string[]
  }>
  rAndDDeduction?: {
    isEligible: boolean
    deductionRate: number
    rAndDExpenses: number
    extraDeductionAmount: number
    estimatedTaxSavings: number
    qualifyingExpenses?: string[]
    reason?: string
  }
  totalEstimatedAnnualSavings: number
  savingsAsPercentageOfRevenue: number
  calculationDate: string
  financeActVersion: string
}

export default function InvestmentIncentivesCalculator() {
  const [formData, setFormData] = useState<InvestmentIncentiveForm>({
    businessName: '',
    investmentAmount: 0,
    employeeCount: 0,
    localOwnershipPercentage: 0,
    businessSector: '',
    isNewBusiness: true,
    annualRevenue: 0,
    estimatedCorporateTax: 0,
    cultivatedLandHectares: 0,
    livestockCount: 0,
    machineryImportValue: 0,
    renewableEnergyEquipmentValue: 0,
    rAndDExpenses: 0
  })

  const [result, setResult] = useState<IncentiveResult | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const businessSectors = [
    { value: 'Agriculture', label: 'Agriculture' },
    { value: 'Manufacturing', label: 'Manufacturing' },
    { value: 'Services', label: 'Services' },
    { value: 'RenewableEnergy', label: 'Renewable Energy' },
    { value: 'Mining', label: 'Mining' },
    { value: 'Tourism', label: 'Tourism' },
    { value: 'Technology', label: 'Technology' },
    { value: 'Other', label: 'Other' }
  ]

  const handleCalculate = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/financeact2025/investment-incentives', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
      })

      if (!response.ok) {
        throw new Error('Failed to calculate investment incentives')
      }

      const data = await response.json()
      setResult(data)
      toast.success('Investment incentives calculated successfully')
    } catch (error) {
      console.error('Error calculating incentives:', error)
      toast.error('Failed to calculate investment incentives')
    } finally {
      setIsLoading(false)
    }
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-SL', {
      style: 'currency',
      currency: 'SLE',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount)
  }

  const formatPercentage = (value: number) => {
    return `${value.toFixed(1)}%`
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
          <Badge variant="default">Finance Act 2025</Badge>
        </div>
        
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Investment Incentives Calculator
        </h1>
        <p className="text-lg text-gray-600">
          Calculate your eligibility for Sierra Leone's comprehensive investment incentives under the Finance Act 2025
        </p>
      </div>

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Input Form */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Calculator className="h-5 w-5" />
                Business Information
              </CardTitle>
              <CardDescription>
                Enter your business details to assess eligibility for various investment incentives
              </CardDescription>
            </CardHeader>
            
            <CardContent className="space-y-6">
              {/* Basic Information */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Basic Information</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="businessName">Business Name</Label>
                    <Input
                      id="businessName"
                      value={formData.businessName}
                      onChange={(e) => setFormData(prev => ({ ...prev, businessName: e.target.value }))}
                      placeholder="Enter business name"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="businessSector">Business Sector</Label>
                    <Select 
                      value={formData.businessSector} 
                      onValueChange={(value) => setFormData(prev => ({ ...prev, businessSector: value }))}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select sector" />
                      </SelectTrigger>
                      <SelectContent>
                        {businessSectors.map(sector => (
                          <SelectItem key={sector.value} value={sector.value}>
                            {sector.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="flex items-center space-x-2">
                  <Switch
                    id="isNewBusiness"
                    checked={formData.isNewBusiness}
                    onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isNewBusiness: checked }))}
                  />
                  <Label htmlFor="isNewBusiness">This is a new business</Label>
                </div>
              </div>

              <Separator />

              {/* Investment & Employment */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Investment & Employment</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="investmentAmount">Total Investment Amount (USD)</Label>
                    <Input
                      id="investmentAmount"
                      type="number"
                      value={formData.investmentAmount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, investmentAmount: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="employeeCount">Number of Full-Time Employees</Label>
                    <Input
                      id="employeeCount"
                      type="number"
                      value={formData.employeeCount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, employeeCount: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="localOwnershipPercentage">Local Ownership Percentage (%)</Label>
                    <Input
                      id="localOwnershipPercentage"
                      type="number"
                      max="100"
                      value={formData.localOwnershipPercentage || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, localOwnershipPercentage: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="machineryImportValue">Machinery Import Value (USD)</Label>
                    <Input
                      id="machineryImportValue"
                      type="number"
                      value={formData.machineryImportValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, machineryImportValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>
              </div>

              <Separator />

              {/* Financial Information */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Financial Information</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="annualRevenue">Annual Revenue (SLE)</Label>
                    <Input
                      id="annualRevenue"
                      type="number"
                      value={formData.annualRevenue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, annualRevenue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="estimatedCorporateTax">Estimated Corporate Tax (SLE)</Label>
                    <Input
                      id="estimatedCorporateTax"
                      type="number"
                      value={formData.estimatedCorporateTax || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, estimatedCorporateTax: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="rAndDExpenses">R&D Expenses (SLE)</Label>
                    <Input
                      id="rAndDExpenses"
                      type="number"
                      value={formData.rAndDExpenses || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, rAndDExpenses: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>
              </div>

              {/* Sector-Specific Fields */}
              {(formData.businessSector === 'Agriculture') && (
                <>
                  <Separator />
                  <div className="space-y-4">
                    <h3 className="text-lg font-medium">Agriculture-Specific Information</h3>
                    
                    <div className="grid gap-4 md:grid-cols-2">
                      <div>
                        <Label htmlFor="cultivatedLandHectares">Cultivated Land (Hectares)</Label>
                        <Input
                          id="cultivatedLandHectares"
                          type="number"
                          value={formData.cultivatedLandHectares || ''}
                          onChange={(e) => setFormData(prev => ({ ...prev, cultivatedLandHectares: Number(e.target.value) }))}
                          placeholder="0"
                        />
                      </div>
                      
                      <div>
                        <Label htmlFor="livestockCount">Livestock Count</Label>
                        <Input
                          id="livestockCount"
                          type="number"
                          value={formData.livestockCount || ''}
                          onChange={(e) => setFormData(prev => ({ ...prev, livestockCount: Number(e.target.value) }))}
                          placeholder="0"
                        />
                      </div>
                    </div>
                  </div>
                </>
              )}

              {(formData.businessSector === 'RenewableEnergy') && (
                <>
                  <Separator />
                  <div className="space-y-4">
                    <h3 className="text-lg font-medium">Renewable Energy Information</h3>
                    
                    <div>
                      <Label htmlFor="renewableEnergyEquipmentValue">Renewable Energy Equipment Value (USD)</Label>
                      <Input
                        id="renewableEnergyEquipmentValue"
                        type="number"
                        value={formData.renewableEnergyEquipmentValue || ''}
                        onChange={(e) => setFormData(prev => ({ ...prev, renewableEnergyEquipmentValue: Number(e.target.value) }))}
                        placeholder="0"
                      />
                    </div>
                  </div>
                </>
              )}

              <Button 
                onClick={handleCalculate} 
                disabled={isLoading || !formData.businessName}
                className="w-full"
              >
                {isLoading ? 'Calculating...' : 'Calculate Investment Incentives'}
              </Button>
            </CardContent>
          </Card>
        </div>

        {/* Results */}
        <div className="lg:col-span-1">
          {result ? (
            <div className="space-y-6">
              {/* Summary Card */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <TrendingUp className="h-5 w-5 text-green-600" />
                    Incentive Summary
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div>
                      <p className="text-sm text-gray-600">Business Name</p>
                      <p className="font-medium">{result.businessName}</p>
                    </div>
                    
                    <div>
                      <p className="text-sm text-gray-600">Total Annual Savings</p>
                      <p className="text-2xl font-bold text-green-600">
                        {formatCurrency(result.totalEstimatedAnnualSavings)}
                      </p>
                    </div>
                    
                    <div>
                      <p className="text-sm text-gray-600">Savings as % of Revenue</p>
                      <p className="text-lg font-semibold">
                        {formatPercentage(result.savingsAsPercentageOfRevenue)}
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Employment-Based Exemption */}
              {result.employmentBasedExemption && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      {result.employmentBasedExemption.isEligible ? (
                        <CheckCircle className="h-5 w-5 text-green-600" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-600" />
                      )}
                      Employment-Based Exemption
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    {result.employmentBasedExemption.isEligible ? (
                      <div className="space-y-3">
                        <Badge variant="secondary" className="bg-green-100 text-green-800">
                          {result.employmentBasedExemption.exemptionYears}-Year Exemption
                        </Badge>
                        <p className="text-sm">{result.employmentBasedExemption.requirements}</p>
                        <div className="flex items-center gap-2">
                          <DollarSign className="h-4 w-4 text-green-600" />
                          <span className="font-medium">
                            {formatCurrency(result.employmentBasedExemption.estimatedAnnualSavings)} annually
                          </span>
                        </div>
                      </div>
                    ) : (
                      <p className="text-sm text-gray-600">{result.employmentBasedExemption.reason}</p>
                    )}
                  </CardContent>
                </Card>
              )}

              {/* Agribusiness Exemption */}
              {result.agribusinessExemption && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      {result.agribusinessExemption.isEligible ? (
                        <CheckCircle className="h-5 w-5 text-green-600" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-600" />
                      )}
                      Agribusiness Exemption
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    {result.agribusinessExemption.isEligible ? (
                      <div className="space-y-3">
                        <p className="text-sm">{result.agribusinessExemption.exemptionType}</p>
                        <div className="flex items-center gap-2">
                          <DollarSign className="h-4 w-4 text-green-600" />
                          <span className="font-medium">
                            {formatCurrency(result.agribusinessExemption.estimatedAnnualSavings)} annually
                          </span>
                        </div>
                        {result.agribusinessExemption.qualifyingActivities && (
                          <div>
                            <p className="text-sm font-medium mb-1">Qualifying Activities:</p>
                            <ul className="text-sm text-gray-600 space-y-1">
                              {result.agribusinessExemption.qualifyingActivities.map((activity, idx) => (
                                <li key={idx}>• {activity}</li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>
                    ) : (
                      <p className="text-sm text-gray-600">{result.agribusinessExemption.reason}</p>
                    )}
                  </CardContent>
                </Card>
              )}

              {/* Renewable Energy Exemption */}
              {result.renewableEnergyExemption && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      {result.renewableEnergyExemption.isEligible ? (
                        <CheckCircle className="h-5 w-5 text-green-600" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-600" />
                      )}
                      Renewable Energy Incentives
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    {result.renewableEnergyExemption.isEligible ? (
                      <div className="space-y-3">
                        <p className="text-sm">{result.renewableEnergyExemption.exemptionType}</p>
                        <div className="flex items-center gap-2">
                          <DollarSign className="h-4 w-4 text-green-600" />
                          <span className="font-medium">
                            {formatCurrency(result.renewableEnergyExemption.estimatedAnnualSavings)} annually
                          </span>
                        </div>
                      </div>
                    ) : (
                      <p className="text-sm text-gray-600">{result.renewableEnergyExemption.reason}</p>
                    )}
                  </CardContent>
                </Card>
              )}

              {/* Duty-Free Import Provisions */}
              {result.dutyFreeImportProvisions && result.dutyFreeImportProvisions.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <CheckCircle className="h-5 w-5 text-green-600" />
                      Duty-Free Import Benefits
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {result.dutyFreeImportProvisions.map((provision, idx) => (
                        <div key={idx} className="border-l-2 border-blue-500 pl-3">
                          <p className="font-medium text-sm">{provision.type}</p>
                          <p className="text-sm text-gray-600">{provision.requirements}</p>
                          <p className="text-sm">
                            <span className="font-medium">{provision.durationYears}-year period • </span>
                            <span className="text-green-600">{formatCurrency(provision.estimatedSavings)} savings</span>
                          </p>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* R&D Deduction */}
              {result.rAndDDeduction && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      {result.rAndDDeduction.isEligible ? (
                        <CheckCircle className="h-5 w-5 text-green-600" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-600" />
                      )}
                      R&D Tax Deduction
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    {result.rAndDDeduction.isEligible ? (
                      <div className="space-y-3">
                        <Badge variant="secondary" className="bg-blue-100 text-blue-800">
                          {result.rAndDDeduction.deductionRate}% Deduction Rate
                        </Badge>
                        <div className="flex items-center gap-2">
                          <DollarSign className="h-4 w-4 text-green-600" />
                          <span className="font-medium">
                            {formatCurrency(result.rAndDDeduction.estimatedTaxSavings)} annual savings
                          </span>
                        </div>
                        <p className="text-sm text-gray-600">
                          Extra deduction: {formatCurrency(result.rAndDDeduction.extraDeductionAmount)}
                        </p>
                      </div>
                    ) : (
                      <p className="text-sm text-gray-600">{result.rAndDDeduction.reason}</p>
                    )}
                  </CardContent>
                </Card>
              )}
            </div>
          ) : (
            <Card>
              <CardContent className="text-center py-12">
                <Calculator className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600">
                  Enter your business information to calculate investment incentives
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}