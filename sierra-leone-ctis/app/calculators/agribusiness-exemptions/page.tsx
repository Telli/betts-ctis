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
import { ArrowLeft, Calculator, CheckCircle, XCircle, DollarSign, Sprout, Tractor } from 'lucide-react'
import Link from 'next/link'
import { toast } from '@/components/ui/enhanced-toast'

interface AgribusinessExemptionForm {
  businessName: string
  landOwnership: string
  cultivatedLandHectares: number
  livestockCount: number
  livestockType: string
  annualTurnover: number
  estimatedCorporateTax: number
  machineryImportValue: number
  farmingEquipmentValue: number
  processingEquipmentValue: number
  isExistingFarmer: boolean
  farmingExperience: number
  localEmployees: number
  primaryCrops: string[]
  hasProcessingFacility: boolean
  exportPercentage: number
  sustainablePractices: boolean
}

interface AgribusinessExemptionResult {
  businessName: string
  isEligible: boolean
  exemptionType: string
  exemptionDetails: {
    corporateTaxExemption: boolean
    importDutyExemption: boolean
    machineryExemption: boolean
    processingEquipmentExemption: boolean
    exemptionPeriodYears: number
  }
  estimatedAnnualSavings: number
  qualifyingActivities: string[]
  requirements: string
  eligibilityCriteria: {
    landSizeRequirement: { met: boolean; threshold: number; actual: number }
    livestockRequirement: { met: boolean; threshold: number; actual: number }
    turnoverRequirement: { met: boolean; threshold: number; actual: number }
    employmentRequirement: { met: boolean; threshold: number; actual: number }
  }
  dutyFreeImportSavings: number
  corporateTaxSavings: number
  totalEstimatedSavings: number
  calculationDate: string
  financeActVersion: string
  reason?: string
}

export default function AgribusinessExemptionsCalculator() {
  const [formData, setFormData] = useState<AgribusinessExemptionForm>({
    businessName: '',
    landOwnership: '',
    cultivatedLandHectares: 0,
    livestockCount: 0,
    livestockType: '',
    annualTurnover: 0,
    estimatedCorporateTax: 0,
    machineryImportValue: 0,
    farmingEquipmentValue: 0,
    processingEquipmentValue: 0,
    isExistingFarmer: false,
    farmingExperience: 0,
    localEmployees: 0,
    primaryCrops: [],
    hasProcessingFacility: false,
    exportPercentage: 0,
    sustainablePractices: false
  })

  const [result, setResult] = useState<AgribusinessExemptionResult | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const livestockTypes = [
    { value: 'cattle', label: 'Cattle' },
    { value: 'goats', label: 'Goats' },
    { value: 'sheep', label: 'Sheep' },
    { value: 'pigs', label: 'Pigs' },
    { value: 'poultry', label: 'Poultry' },
    { value: 'mixed', label: 'Mixed Livestock' }
  ]

  const landOwnershipOptions = [
    { value: 'owned', label: 'Owned Land' },
    { value: 'leased', label: 'Leased Land' },
    { value: 'mixed', label: 'Mixed (Owned & Leased)' }
  ]

  const cropOptions = [
    'Rice', 'Cassava', 'Sweet Potato', 'Maize', 'Groundnuts', 
    'Cocoa', 'Coffee', 'Oil Palm', 'Cashew', 'Vegetables', 'Fruits'
  ]

  const handleCropChange = (crop: string, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      primaryCrops: checked 
        ? [...prev.primaryCrops, crop]
        : prev.primaryCrops.filter(c => c !== crop)
    }))
  }

  const handleCalculate = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/financeact2025/agribusiness-exemptions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
      })

      if (!response.ok) {
        throw new Error('Failed to calculate agribusiness exemptions')
      }

      const data = await response.json()
      setResult(data)
      toast.success('Agribusiness exemptions calculated successfully')
    } catch (error) {
      console.error('Error calculating exemptions:', error)
      toast.error('Failed to calculate agribusiness exemptions')
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
          Agribusiness Tax Exemptions Calculator
        </h1>
        <p className="text-lg text-gray-600">
          Calculate eligibility for agricultural sector tax exemptions and import duty benefits under Finance Act 2025
        </p>
      </div>

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Input Form */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Sprout className="h-5 w-5" />
                Agricultural Business Information
              </CardTitle>
              <CardDescription>
                Enter your agricultural business details to assess eligibility for Finance Act 2025 exemptions
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
                    <Label htmlFor="landOwnership">Land Ownership Type</Label>
                    <Select 
                      value={formData.landOwnership} 
                      onValueChange={(value) => setFormData(prev => ({ ...prev, landOwnership: value }))}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select ownership type" />
                      </SelectTrigger>
                      <SelectContent>
                        {landOwnershipOptions.map(option => (
                          <SelectItem key={option.value} value={option.value}>
                            {option.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="flex items-center space-x-2">
                  <Switch
                    id="isExistingFarmer"
                    checked={formData.isExistingFarmer}
                    onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isExistingFarmer: checked }))}
                  />
                  <Label htmlFor="isExistingFarmer">Existing farmer (expanding operations)</Label>
                </div>
              </div>

              <Separator />

              {/* Land & Livestock */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Land & Livestock Details</h3>
                
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
                    <Label htmlFor="livestockCount">Number of Livestock</Label>
                    <Input
                      id="livestockCount"
                      type="number"
                      value={formData.livestockCount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, livestockCount: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="livestockType">Livestock Type</Label>
                    <Select 
                      value={formData.livestockType} 
                      onValueChange={(value) => setFormData(prev => ({ ...prev, livestockType: value }))}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select livestock type" />
                      </SelectTrigger>
                      <SelectContent>
                        {livestockTypes.map(type => (
                          <SelectItem key={type.value} value={type.value}>
                            {type.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  
                  <div>
                    <Label htmlFor="localEmployees">Local Employees</Label>
                    <Input
                      id="localEmployees"
                      type="number"
                      value={formData.localEmployees || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, localEmployees: Number(e.target.value) }))}
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
                    <Label htmlFor="annualTurnover">Annual Turnover (SLE)</Label>
                    <Input
                      id="annualTurnover"
                      type="number"
                      value={formData.annualTurnover || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, annualTurnover: Number(e.target.value) }))}
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
                    <Label htmlFor="farmingExperience">Farming Experience (Years)</Label>
                    <Input
                      id="farmingExperience"
                      type="number"
                      value={formData.farmingExperience || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, farmingExperience: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>
              </div>

              <Separator />

              {/* Equipment & Infrastructure */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Equipment & Infrastructure</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="machineryImportValue">Farm Machinery Import Value (USD)</Label>
                    <Input
                      id="machineryImportValue"
                      type="number"
                      value={formData.machineryImportValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, machineryImportValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="farmingEquipmentValue">Farming Equipment Value (SLE)</Label>
                    <Input
                      id="farmingEquipmentValue"
                      type="number"
                      value={formData.farmingEquipmentValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, farmingEquipmentValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="processingEquipmentValue">Processing Equipment Value (SLE)</Label>
                    <Input
                      id="processingEquipmentValue"
                      type="number"
                      value={formData.processingEquipmentValue || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, processingEquipmentValue: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                </div>

                <div className="flex items-center space-x-2">
                  <Switch
                    id="hasProcessingFacility"
                    checked={formData.hasProcessingFacility}
                    onCheckedChange={(checked) => setFormData(prev => ({ ...prev, hasProcessingFacility: checked }))}
                  />
                  <Label htmlFor="hasProcessingFacility">Has processing facility</Label>
                </div>

                <div className="flex items-center space-x-2">
                  <Switch
                    id="sustainablePractices"
                    checked={formData.sustainablePractices}
                    onCheckedChange={(checked) => setFormData(prev => ({ ...prev, sustainablePractices: checked }))}
                  />
                  <Label htmlFor="sustainablePractices">Uses sustainable farming practices</Label>
                </div>
              </div>

              <Separator />

              {/* Primary Crops */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Primary Crops</h3>
                <div className="grid gap-3 md:grid-cols-3">
                  {cropOptions.map(crop => (
                    <div key={crop} className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        id={crop}
                        checked={formData.primaryCrops.includes(crop)}
                        onChange={(e) => handleCropChange(crop, e.target.checked)}
                        className="rounded border-gray-300"
                      />
                      <Label htmlFor={crop} className="text-sm">{crop}</Label>
                    </div>
                  ))}
                </div>
              </div>

              <Button 
                onClick={handleCalculate} 
                disabled={isLoading || !formData.businessName}
                className="w-full"
              >
                {isLoading ? 'Calculating...' : 'Calculate Agribusiness Exemptions'}
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
                          <p className="font-medium text-green-600">{result.exemptionType}</p>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Total Annual Savings</p>
                          <p className="text-2xl font-bold text-green-600">
                            {formatCurrency(result.totalEstimatedSavings)}
                          </p>
                        </div>
                        
                        {result.exemptionDetails.exemptionPeriodYears > 0 && (
                          <div>
                            <p className="text-sm text-gray-600">Exemption Period</p>
                            <Badge variant="secondary" className="bg-green-100 text-green-800">
                              {result.exemptionDetails.exemptionPeriodYears} Years
                            </Badge>
                          </div>
                        )}
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

              {/* Eligibility Details */}
              {result.isEligible && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Tractor className="h-5 w-5 text-blue-600" />
                      Exemption Benefits
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {result.exemptionDetails.corporateTaxExemption && (
                        <div className="flex items-center gap-2 text-green-600">
                          <CheckCircle className="h-4 w-4" />
                          <span className="text-sm">Corporate Tax Exemption</span>
                          <span className="ml-auto font-medium">
                            {formatCurrency(result.corporateTaxSavings)}
                          </span>
                        </div>
                      )}
                      
                      {result.exemptionDetails.importDutyExemption && (
                        <div className="flex items-center gap-2 text-green-600">
                          <CheckCircle className="h-4 w-4" />
                          <span className="text-sm">Import Duty Exemption</span>
                          <span className="ml-auto font-medium">
                            {formatCurrency(result.dutyFreeImportSavings)}
                          </span>
                        </div>
                      )}
                      
                      {result.exemptionDetails.machineryExemption && (
                        <div className="flex items-center gap-2 text-green-600">
                          <CheckCircle className="h-4 w-4" />
                          <span className="text-sm">Farm Machinery Exemption</span>
                        </div>
                      )}
                      
                      {result.exemptionDetails.processingEquipmentExemption && (
                        <div className="flex items-center gap-2 text-green-600">
                          <CheckCircle className="h-4 w-4" />
                          <span className="text-sm">Processing Equipment Exemption</span>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Qualifying Activities */}
              {result.isEligible && result.qualifyingActivities.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle>Qualifying Activities</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="space-y-2">
                      {result.qualifyingActivities.map((activity, idx) => (
                        <li key={idx} className="text-sm flex items-center gap-2">
                          <DollarSign className="h-3 w-3 text-green-600" />
                          {activity}
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>
              )}

              {/* Requirements */}
              {result.requirements && (
                <Card>
                  <CardHeader>
                    <CardTitle>Requirements</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-gray-600">{result.requirements}</p>
                  </CardContent>
                </Card>
              )}
            </div>
          ) : (
            <Card>
              <CardContent className="text-center py-12">
                <Calculator className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-600">
                  Enter your agricultural business information to calculate exemption eligibility
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}