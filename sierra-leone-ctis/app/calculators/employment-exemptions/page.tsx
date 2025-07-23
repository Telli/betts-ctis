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
import { ArrowLeft, Calculator, CheckCircle, XCircle, DollarSign, Users, Building } from 'lucide-react'
import Link from 'next/link'
import { toast } from '@/components/ui/enhanced-toast'

interface EmploymentExemptionForm {
  businessName: string
  businessSector: string
  investmentAmount: number
  currentEmployeeCount: number
  projectedEmployeeCount: number
  localOwnershipPercentage: number
  isNewBusiness: boolean
  businessRegistrationDate: string
  annualRevenue: number
  estimatedCorporateTax: number
  averageSalary: number
  localEmployeePercentage: number
  femaleEmployeePercentage: number
  youthEmployeePercentage: number
  isExportOriented: boolean
  exportPercentage: number
  hasSkillsDevelopmentProgram: boolean
  skillsTrainingBudget: number
  businessLocation: string
  isOutsideFreetown: boolean
}

interface EmploymentExemptionResult {
  businessName: string
  isEligible: boolean
  exemptionDetails: {
    exemptionType: string
    exemptionYears: number
    requirements: string
    estimatedAnnualSavings: number
  }
  eligibilityCriteria: {
    employmentRequirement: { met: boolean; threshold: number; actual: number }
    investmentRequirement: { met: boolean; threshold: number; actual: number }
    localOwnershipRequirement: { met: boolean; threshold: number; actual: number }
    localEmploymentRequirement: { met: boolean; threshold: number; actual: number }
  }
  additionalIncentives: {
    femaleEmploymentBonus: { eligible: boolean; bonus: number }
    youthEmploymentBonus: { eligible: boolean; bonus: number }
    ruralLocationBonus: { eligible: boolean; bonus: number }
    exportOrientedBonus: { eligible: boolean; bonus: number }
    skillsDevelopmentBonus: { eligible: boolean; bonus: number }
  }
  totalAnnualSavings: number
  corporateTaxSavings: number
  totalLifetimeSavings: number
  savingsBreakdown: {
    baseCorporateTaxExemption: number
    employmentBonuses: number
    additionalIncentives: number
  }
  calculationDate: string
  financeActVersion: string
  reason?: string
}

export default function EmploymentExemptionsCalculator() {
  const [formData, setFormData] = useState<EmploymentExemptionForm>({
    businessName: '',
    businessSector: '',
    investmentAmount: 0,
    currentEmployeeCount: 0,
    projectedEmployeeCount: 0,
    localOwnershipPercentage: 0,
    isNewBusiness: true,
    businessRegistrationDate: '',
    annualRevenue: 0,
    estimatedCorporateTax: 0,
    averageSalary: 0,
    localEmployeePercentage: 0,
    femaleEmployeePercentage: 0,
    youthEmployeePercentage: 0,
    isExportOriented: false,
    exportPercentage: 0,
    hasSkillsDevelopmentProgram: false,
    skillsTrainingBudget: 0,
    businessLocation: '',
    isOutsideFreetown: false
  })

  const [result, setResult] = useState<EmploymentExemptionResult | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const businessSectors = [
    { value: 'Manufacturing', label: 'Manufacturing' },
    { value: 'Agriculture', label: 'Agriculture' },
    { value: 'Services', label: 'Services' },
    { value: 'Technology', label: 'Technology' },
    { value: 'Tourism', label: 'Tourism' },
    { value: 'Mining', label: 'Mining' },
    { value: 'RenewableEnergy', label: 'Renewable Energy' },
    { value: 'Other', label: 'Other' }
  ]

  const locationOptions = [
    { value: 'freetown', label: 'Freetown' },
    { value: 'bo', label: 'Bo' },
    { value: 'kenema', label: 'Kenema' },
    { value: 'makeni', label: 'Makeni' },
    { value: 'koidu', label: 'Koidu' },
    { value: 'rural', label: 'Rural Area' },
    { value: 'other', label: 'Other' }
  ]

  const handleCalculate = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/financeact2025/employment-exemptions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
      })

      if (!response.ok) {
        throw new Error('Failed to calculate employment exemptions')
      }

      const data = await response.json()
      setResult(data)
      toast.success('Employment exemptions calculated successfully')
    } catch (error) {
      console.error('Error calculating exemptions:', error)
      toast.error('Failed to calculate employment exemptions')
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

  const formatUSD = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount)
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
          <Badge variant="secondary">Finance Act 2025</Badge>
        </div>
        
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Employment-Based Tax Exemptions Calculator
        </h1>
        <p className="text-lg text-gray-600">
          Calculate tax exemptions based on employment creation and investment levels under Finance Act 2025
        </p>
      </div>

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Input Form */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Employment & Investment Information
              </CardTitle>
              <CardDescription>
                Enter your business details to assess eligibility for employment-based tax exemptions
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
                  
                  <div>
                    <Label htmlFor="businessLocation">Business Location</Label>
                    <Select 
                      value={formData.businessLocation} 
                      onValueChange={(value) => setFormData(prev => ({ 
                        ...prev, 
                        businessLocation: value,
                        isOutsideFreetown: value !== 'freetown'
                      }))}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select location" />
                      </SelectTrigger>
                      <SelectContent>
                        {locationOptions.map(location => (
                          <SelectItem key={location.value} value={location.value}>
                            {location.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  
                  <div>
                    <Label htmlFor="businessRegistrationDate">Business Registration Date</Label>
                    <Input
                      id="businessRegistrationDate"
                      type="date"
                      value={formData.businessRegistrationDate}
                      onChange={(e) => setFormData(prev => ({ ...prev, businessRegistrationDate: e.target.value }))}
                    />
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

              {/* Investment & Ownership */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Investment & Ownership</h3>
                
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
                </div>
              </div>

              <Separator />

              {/* Employment Details */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Employment Details</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="currentEmployeeCount">Current Employee Count</Label>
                    <Input
                      id="currentEmployeeCount"
                      type="number"
                      value={formData.currentEmployeeCount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, currentEmployeeCount: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="projectedEmployeeCount">Projected Employee Count</Label>
                    <Input
                      id="projectedEmployeeCount"
                      type="number"
                      value={formData.projectedEmployeeCount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, projectedEmployeeCount: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="averageSalary">Average Monthly Salary (SLE)</Label>
                    <Input
                      id="averageSalary"
                      type="number"
                      value={formData.averageSalary || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, averageSalary: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="localEmployeePercentage">Local Employee Percentage (%)</Label>
                    <Input
                      id="localEmployeePercentage"
                      type="number"
                      max="100"
                      value={formData.localEmployeePercentage || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, localEmployeePercentage: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="femaleEmployeePercentage">Female Employee Percentage (%)</Label>
                    <Input
                      id="femaleEmployeePercentage"
                      type="number"
                      max="100"
                      value={formData.femaleEmployeePercentage || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, femaleEmployeePercentage: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="youthEmployeePercentage">Youth Employee Percentage (18-35) (%)</Label>
                    <Input
                      id="youthEmployeePercentage"
                      type="number"
                      max="100"
                      value={formData.youthEmployeePercentage || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, youthEmployeePercentage: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>
              </div>

              <Separator />

              {/* Additional Incentives */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Additional Incentive Eligibility</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="exportPercentage">Export Percentage (%)</Label>
                    <Input
                      id="exportPercentage"
                      type="number"
                      max="100"
                      value={formData.exportPercentage || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, exportPercentage: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="skillsTrainingBudget">Skills Training Budget (SLE)</Label>
                    <Input
                      id="skillsTrainingBudget"
                      type="number"
                      value={formData.skillsTrainingBudget || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, skillsTrainingBudget: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>

                <div className="space-y-3">
                  <div className="flex items-center space-x-2">
                    <Switch
                      id="isExportOriented"
                      checked={formData.isExportOriented}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isExportOriented: checked }))}
                    />
                    <Label htmlFor="isExportOriented">Export-oriented business</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="hasSkillsDevelopmentProgram"
                      checked={formData.hasSkillsDevelopmentProgram}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, hasSkillsDevelopmentProgram: checked }))}
                    />
                    <Label htmlFor="hasSkillsDevelopmentProgram">Has skills development program</Label>
                  </div>
                </div>
              </div>

              <Button 
                onClick={handleCalculate} 
                disabled={isLoading || !formData.businessName}
                className="w-full"
              >
                {isLoading ? 'Calculating...' : 'Calculate Employment Exemptions'}
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
                    {result.isEligible ? (
                      <CheckCircle className="h-5 w-5 text-green-600" />
                    ) : (
                      <XCircle className="h-5 w-5 text-red-600" />
                    )}
                    Exemption Status
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div>
                      <p className="text-sm text-gray-600">Business Name</p>
                      <p className="font-medium">{result.businessName}</p>
                    </div>
                    
                    {result.isEligible ? (
                      <>
                        <div>
                          <p className="text-sm text-gray-600">Exemption Type</p>
                          <p className="font-medium text-green-600">{result.exemptionDetails.exemptionType}</p>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Exemption Period</p>
                          <Badge variant="secondary" className="bg-green-100 text-green-800">
                            {result.exemptionDetails.exemptionYears} Years
                          </Badge>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Annual Savings</p>
                          <p className="text-2xl font-bold text-green-600">
                            {formatCurrency(result.totalAnnualSavings)}
                          </p>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Total Lifetime Savings</p>
                          <p className="text-lg font-semibold text-green-600">
                            {formatCurrency(result.totalLifetimeSavings)}
                          </p>
                        </div>
                      </>
                    ) : (
                      <div>
                        <p className="text-sm text-gray-600">Reason for Ineligibility</p>
                        <p className="text-sm text-red-600">{result.reason}</p>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>

              {/* Eligibility Criteria */}
              {result.isEligible && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Building className="h-5 w-5 text-blue-600" />
                      Eligibility Status
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      <div className="flex items-center justify-between">
                        <span className="text-sm">Employment Requirement</span>
                        <div className="flex items-center gap-2">
                          {result.eligibilityCriteria.employmentRequirement.met ? (
                            <CheckCircle className="h-4 w-4 text-green-600" />
                          ) : (
                            <XCircle className="h-4 w-4 text-red-600" />
                          )}
                          <span className="text-sm">
                            {result.eligibilityCriteria.employmentRequirement.actual}/
                            {result.eligibilityCriteria.employmentRequirement.threshold}
                          </span>
                        </div>
                      </div>
                      
                      <div className="flex items-center justify-between">
                        <span className="text-sm">Investment Requirement</span>
                        <div className="flex items-center gap-2">
                          {result.eligibilityCriteria.investmentRequirement.met ? (
                            <CheckCircle className="h-4 w-4 text-green-600" />
                          ) : (
                            <XCircle className="h-4 w-4 text-red-600" />
                          )}
                          <span className="text-sm">
                            {formatUSD(result.eligibilityCriteria.investmentRequirement.actual)}/
                            {formatUSD(result.eligibilityCriteria.investmentRequirement.threshold)}
                          </span>
                        </div>
                      </div>
                      
                      <div className="flex items-center justify-between">
                        <span className="text-sm">Local Ownership</span>
                        <div className="flex items-center gap-2">
                          {result.eligibilityCriteria.localOwnershipRequirement.met ? (
                            <CheckCircle className="h-4 w-4 text-green-600" />
                          ) : (
                            <XCircle className="h-4 w-4 text-red-600" />
                          )}
                          <span className="text-sm">
                            {result.eligibilityCriteria.localOwnershipRequirement.actual}%/
                            {result.eligibilityCriteria.localOwnershipRequirement.threshold}%
                          </span>
                        </div>
                      </div>
                      
                      <div className="flex items-center justify-between">
                        <span className="text-sm">Local Employment</span>
                        <div className="flex items-center gap-2">
                          {result.eligibilityCriteria.localEmploymentRequirement.met ? (
                            <CheckCircle className="h-4 w-4 text-green-600" />
                          ) : (
                            <XCircle className="h-4 w-4 text-red-600" />
                          )}
                          <span className="text-sm">
                            {result.eligibilityCriteria.localEmploymentRequirement.actual}%/
                            {result.eligibilityCriteria.localEmploymentRequirement.threshold}%
                          </span>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Additional Incentives */}
              {result.isEligible && (
                <Card>
                  <CardHeader>
                    <CardTitle>Additional Incentives</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {result.additionalIncentives.femaleEmploymentBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Female Employment Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalIncentives.femaleEmploymentBonus.bonus)}
                          </span>
                        </div>
                      )}
                      
                      {result.additionalIncentives.youthEmploymentBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Youth Employment Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalIncentives.youthEmploymentBonus.bonus)}
                          </span>
                        </div>
                      )}
                      
                      {result.additionalIncentives.ruralLocationBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Rural Location Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalIncentives.ruralLocationBonus.bonus)}
                          </span>
                        </div>
                      )}
                      
                      {result.additionalIncentives.exportOrientedBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Export-Oriented Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalIncentives.exportOrientedBonus.bonus)}
                          </span>
                        </div>
                      )}
                      
                      {result.additionalIncentives.skillsDevelopmentBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Skills Development Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalIncentives.skillsDevelopmentBonus.bonus)}
                          </span>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Requirements */}
              {result.isEligible && result.exemptionDetails.requirements && (
                <Card>
                  <CardHeader>
                    <CardTitle>Requirements</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-gray-600">{result.exemptionDetails.requirements}</p>
                  </CardContent>
                </Card>
              )}
            </div>
          ) : (
            <Card>
              <CardContent className="text-center py-12">
                <Calculator className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600">
                  Enter your employment and investment information to calculate exemption eligibility
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}