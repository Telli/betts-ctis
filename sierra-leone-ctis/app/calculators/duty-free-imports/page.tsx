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
import { ArrowLeft, Calculator, CheckCircle, XCircle, DollarSign, Truck, Package } from 'lucide-react'
import Link from 'next/link'
import { toast } from '@/components/ui/enhanced-toast'

interface DutyFreeImportForm {
  businessName: string
  businessSector: string
  investmentAmount: number
  isNewBusiness: boolean
  isExpansionProject: boolean
  employeeCount: number
  localOwnershipPercentage: number
  businessRegistrationDate: string
  projectDescription: string
  machineryImportValue: number
  equipmentImportValue: number
  rawMaterialImportValue: number
  vehicleImportValue: number
  sparePartsImportValue: number
  constructionMaterialValue: number
  projectStartDate: string
  projectedCompletionDate: string
  isExportOriented: boolean
  exportPercentage: number
  hasEnvironmentalClearance: boolean
  hasProjectApproval: boolean
  importItemCategories: string[]
  dutyRateBeforeExemption: number
  totalImportValue: number
}

interface DutyFreeImportResult {
  businessName: string
  isEligible: boolean
  eligibilityType: string
  exemptionDetails: {
    dutyFreeProvisions: Array<{
      category: string
      items: string[]
      exemptionPeriodYears: number
      estimatedSavings: number
      dutyRate: number
    }>
    totalExemptionPeriod: number
    requirements: string[]
  }
  estimatedSavings: {
    machineryDutySavings: number
    equipmentDutySavings: number
    rawMaterialDutySavings: number
    vehicleDutySavings: number
    constructionMaterialSavings: number
    totalDutySavings: number
  }
  eligibilityCriteria: {
    investmentThreshold: { met: boolean; threshold: number; actual: number }
    employmentThreshold: { met: boolean; threshold: number; actual: number }
    localOwnershipThreshold: { met: boolean; threshold: number; actual: number }
    projectApprovalStatus: { met: boolean; requirement: string }
    environmentalClearance: { met: boolean; requirement: string }
  }
  additionalBenefits: {
    expeditedCustomsClearance: boolean
    reducedDocumentation: boolean
    singleWindowProcessing: boolean
  }
  calculationDate: string
  financeActVersion: string
  validityPeriod: string
  reason?: string
}

export default function DutyFreeImportsCalculator() {
  const [formData, setFormData] = useState<DutyFreeImportForm>({
    businessName: '',
    businessSector: '',
    investmentAmount: 0,
    isNewBusiness: true,
    isExpansionProject: false,
    employeeCount: 0,
    localOwnershipPercentage: 0,
    businessRegistrationDate: '',
    projectDescription: '',
    machineryImportValue: 0,
    equipmentImportValue: 0,
    rawMaterialImportValue: 0,
    vehicleImportValue: 0,
    sparePartsImportValue: 0,
    constructionMaterialValue: 0,
    projectStartDate: '',
    projectedCompletionDate: '',
    isExportOriented: false,
    exportPercentage: 0,
    hasEnvironmentalClearance: false,
    hasProjectApproval: false,
    importItemCategories: [],
    dutyRateBeforeExemption: 15,
    totalImportValue: 0
  })

  const [result, setResult] = useState<DutyFreeImportResult | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const businessSectors = [
    { value: 'Manufacturing', label: 'Manufacturing' },
    { value: 'Agriculture', label: 'Agriculture' },
    { value: 'Mining', label: 'Mining' },
    { value: 'Tourism', label: 'Tourism' },
    { value: 'RenewableEnergy', label: 'Renewable Energy' },
    { value: 'Technology', label: 'Technology' },
    { value: 'Infrastructure', label: 'Infrastructure' },
    { value: 'Other', label: 'Other' }
  ]

  const importCategories = [
    'Machinery & Equipment',
    'Production Equipment',
    'Construction Materials',
    'Raw Materials',
    'Commercial Vehicles',
    'Spare Parts',
    'Office Equipment',
    'IT Equipment',
    'Laboratory Equipment',
    'Safety Equipment'
  ]

  const handleCategoryChange = (category: string, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      importItemCategories: checked 
        ? [...prev.importItemCategories, category]
        : prev.importItemCategories.filter(c => c !== category)
    }))
  }

  // Calculate total import value when individual values change
  React.useEffect(() => {
    const total = formData.machineryImportValue + 
                  formData.equipmentImportValue + 
                  formData.rawMaterialImportValue + 
                  formData.vehicleImportValue + 
                  formData.sparePartsImportValue + 
                  formData.constructionMaterialValue
    
    setFormData(prev => ({ ...prev, totalImportValue: total }))
  }, [
    formData.machineryImportValue,
    formData.equipmentImportValue,
    formData.rawMaterialImportValue,
    formData.vehicleImportValue,
    formData.sparePartsImportValue,
    formData.constructionMaterialValue
  ])

  const handleCalculate = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/financeact2025/duty-free-imports', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
      })

      if (!response.ok) {
        throw new Error('Failed to calculate duty-free import benefits')
      }

      const data = await response.json()
      setResult(data)
      toast.success('Duty-free import benefits calculated successfully')
    } catch (error) {
      console.error('Error calculating benefits:', error)
      toast.error('Failed to calculate duty-free import benefits')
    } finally {
      setIsLoading(false)
    }
  }

  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat(currency === 'USD' ? 'en-US' : 'en-SL', {
      style: 'currency',
      currency: currency,
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
          Duty-Free Import Calculator
        </h1>
        <p className="text-lg text-gray-600">
          Calculate savings from duty-free import provisions for qualifying businesses under Finance Act 2025
        </p>
      </div>

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Input Form */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Truck className="h-5 w-5" />
                Import Project Information
              </CardTitle>
              <CardDescription>
                Enter your business and import details to assess eligibility for duty-free import benefits
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
                    <Label htmlFor="businessRegistrationDate">Business Registration Date</Label>
                    <Input
                      id="businessRegistrationDate"
                      type="date"
                      value={formData.businessRegistrationDate}
                      onChange={(e) => setFormData(prev => ({ ...prev, businessRegistrationDate: e.target.value }))}
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="employeeCount">Employee Count</Label>
                    <Input
                      id="employeeCount"
                      type="number"
                      value={formData.employeeCount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, employeeCount: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>

                <div className="space-y-3">
                  <div className="flex items-center space-x-2">
                    <Switch
                      id="isNewBusiness"
                      checked={formData.isNewBusiness}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isNewBusiness: checked }))}
                    />
                    <Label htmlFor="isNewBusiness">New business</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="isExpansionProject"
                      checked={formData.isExpansionProject}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isExpansionProject: checked }))}
                    />
                    <Label htmlFor="isExpansionProject">Expansion project</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="isExportOriented"
                      checked={formData.isExportOriented}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isExportOriented: checked }))}
                    />
                    <Label htmlFor="isExportOriented">Export-oriented business</Label>
                  </div>
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
                    <Label htmlFor="dutyRateBeforeExemption">Current Duty Rate (%)</Label>
                    <Input
                      id="dutyRateBeforeExemption"
                      type="number"
                      value={formData.dutyRateBeforeExemption || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, dutyRateBeforeExemption: Number(e.target.value) }))}
                      placeholder="15"
                    />
                  </div>
                </div>
              </div>

              <Separator />

              {/* Project Timeline */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Project Timeline</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="projectStartDate">Project Start Date</Label>
                    <Input
                      id="projectStartDate"
                      type="date"
                      value={formData.projectStartDate}
                      onChange={(e) => setFormData(prev => ({ ...prev, projectStartDate: e.target.value }))}
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="projectedCompletionDate">Projected Completion Date</Label>
                    <Input
                      id="projectedCompletionDate"
                      type="date"
                      value={formData.projectedCompletionDate}
                      onChange={(e) => setFormData(prev => ({ ...prev, projectedCompletionDate: e.target.value }))}
                    />
                  </div>
                </div>

                <div>
                  <Label htmlFor="projectDescription">Project Description</Label>
                  <textarea
                    id="projectDescription"
                    className="w-full min-h-[80px] px-3 py-2 text-sm border border-gray-300 rounded-md"
                    value={formData.projectDescription}
                    onChange={(e) => setFormData(prev => ({ ...prev, projectDescription: e.target.value }))}
                    placeholder="Describe your project and its objectives"
                  />
                </div>
              </div>

              <Separator />

              {/* Import Values */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Import Value Breakdown (USD)</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="machineryImportValue">Machinery Import Value</Label>
                    <Input
                      id="machineryImportValue"
                      type="number"
                      value={formData.machineryImportValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, machineryImportValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="equipmentImportValue">Equipment Import Value</Label>
                    <Input
                      id="equipmentImportValue"
                      type="number"
                      value={formData.equipmentImportValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, equipmentImportValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="rawMaterialImportValue">Raw Materials Import Value</Label>
                    <Input
                      id="rawMaterialImportValue"
                      type="number"
                      value={formData.rawMaterialImportValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, rawMaterialImportValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="vehicleImportValue">Commercial Vehicles Import Value</Label>
                    <Input
                      id="vehicleImportValue"
                      type="number"
                      value={formData.vehicleImportValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, vehicleImportValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="sparePartsImportValue">Spare Parts Import Value</Label>
                    <Input
                      id="sparePartsImportValue"
                      type="number"
                      value={formData.sparePartsImportValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, sparePartsImportValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="constructionMaterialValue">Construction Materials Value</Label>
                    <Input
                      id="constructionMaterialValue"
                      type="number"
                      value={formData.constructionMaterialValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, constructionMaterialValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>

                <div className="p-3 bg-blue-50 rounded-lg">
                  <div className="flex justify-between items-center">
                    <span className="font-medium">Total Import Value:</span>
                    <span className="text-lg font-bold text-blue-600">
                      {formatCurrency(formData.totalImportValue, 'USD')}
                    </span>
                  </div>
                </div>
              </div>

              <Separator />

              {/* Import Categories */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Import Item Categories</h3>
                <div className="grid gap-3 md:grid-cols-2">
                  {importCategories.map(category => (
                    <div key={category} className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        id={category}
                        checked={formData.importItemCategories.includes(category)}
                        onChange={(e) => handleCategoryChange(category, e.target.checked)}
                        className="rounded border-gray-300"
                      />
                      <Label htmlFor={category} className="text-sm">{category}</Label>
                    </div>
                  ))}
                </div>
              </div>

              <Separator />

              {/* Approvals & Clearances */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Approvals & Clearances</h3>
                
                <div className="space-y-3">
                  <div className="flex items-center space-x-2">
                    <Switch
                      id="hasProjectApproval"
                      checked={formData.hasProjectApproval}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, hasProjectApproval: checked }))}
                    />
                    <Label htmlFor="hasProjectApproval">Has project approval from relevant ministry</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="hasEnvironmentalClearance"
                      checked={formData.hasEnvironmentalClearance}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, hasEnvironmentalClearance: checked }))}
                    />
                    <Label htmlFor="hasEnvironmentalClearance">Has environmental clearance (if required)</Label>
                  </div>
                </div>
              </div>

              <Button 
                onClick={handleCalculate} 
                disabled={isLoading || !formData.businessName}
                className="w-full"
              >
                {isLoading ? 'Calculating...' : 'Calculate Duty-Free Import Benefits'}
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
                    Eligibility Status
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
                          <p className="text-sm text-gray-600">Eligibility Type</p>
                          <p className="font-medium text-green-600">{result.eligibilityType}</p>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Total Duty Savings</p>
                          <p className="text-2xl font-bold text-green-600">
                            {formatCurrency(result.estimatedSavings.totalDutySavings, 'USD')}
                          </p>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Exemption Period</p>
                          <Badge variant="secondary" className="bg-green-100 text-green-800">
                            {result.exemptionDetails.totalExemptionPeriod} Years
                          </Badge>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Valid Until</p>
                          <p className="text-sm font-medium">{result.validityPeriod}</p>
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

              {/* Savings Breakdown */}
              {result.isEligible && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Package className="h-5 w-5 text-blue-600" />
                      Savings Breakdown
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {result.estimatedSavings.machineryDutySavings > 0 && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Machinery Duty</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.estimatedSavings.machineryDutySavings, 'USD')}
                          </span>
                        </div>
                      )}
                      
                      {result.estimatedSavings.equipmentDutySavings > 0 && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Equipment Duty</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.estimatedSavings.equipmentDutySavings, 'USD')}
                          </span>
                        </div>
                      )}
                      
                      {result.estimatedSavings.rawMaterialDutySavings > 0 && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Raw Materials Duty</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.estimatedSavings.rawMaterialDutySavings, 'USD')}
                          </span>
                        </div>
                      )}
                      
                      {result.estimatedSavings.vehicleDutySavings > 0 && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Vehicle Duty</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.estimatedSavings.vehicleDutySavings, 'USD')}
                          </span>
                        </div>
                      )}
                      
                      {result.estimatedSavings.constructionMaterialSavings > 0 && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Construction Materials</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.estimatedSavings.constructionMaterialSavings, 'USD')}
                          </span>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Additional Benefits */}
              {result.isEligible && (
                <Card>
                  <CardHeader>
                    <CardTitle>Additional Benefits</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {result.additionalBenefits.expeditedCustomsClearance && (
                        <div className="flex items-center gap-2 text-green-600">
                          <CheckCircle className="h-4 w-4" />
                          <span className="text-sm">Expedited Customs Clearance</span>
                        </div>
                      )}
                      
                      {result.additionalBenefits.reducedDocumentation && (
                        <div className="flex items-center gap-2 text-green-600">
                          <CheckCircle className="h-4 w-4" />
                          <span className="text-sm">Reduced Documentation</span>
                        </div>
                      )}
                      
                      {result.additionalBenefits.singleWindowProcessing && (
                        <div className="flex items-center gap-2 text-green-600">
                          <CheckCircle className="h-4 w-4" />
                          <span className="text-sm">Single Window Processing</span>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Requirements */}
              {result.isEligible && result.exemptionDetails.requirements.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle>Requirements</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="space-y-2">
                      {result.exemptionDetails.requirements.map((requirement, idx) => (
                        <li key={idx} className="text-sm flex items-center gap-2">
                          <DollarSign className="h-3 w-3 text-blue-600" />
                          {requirement}
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>
              )}
            </div>
          ) : (
            <Card>
              <CardContent className="text-center py-12">
                <Calculator className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600">
                  Enter your import project information to calculate duty-free benefits
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}