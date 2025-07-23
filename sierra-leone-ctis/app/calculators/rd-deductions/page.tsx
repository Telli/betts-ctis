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
import { ArrowLeft, Calculator, CheckCircle, XCircle, DollarSign, Lightbulb } from 'lucide-react'
import Link from 'next/link'
import { toast } from '@/components/ui/enhanced-toast'

interface RAndDDeductionForm {
  businessName: string
  businessSector: string
  taxableIncome: number
  corporateTaxRate: number
  rdExpenses: {
    researchActivities: number
    developmentActivities: number
    trainingPrograms: number
    consultancyFees: number
    equipmentCosts: number
    personnelCosts: number
    materialCosts: number
    facilityRentalCosts: number
    otherQualifyingCosts: number
  }
  rdActivities: string[]
  hasQualifiedPersonnel: boolean
  qualifiedPersonnelCount: number
  hasRDFacility: boolean
  rdFacilityType: string
  isCollaboratingWithUniversity: boolean
  collaboratingInstitutions: string[]
  rdProjectDuration: number
  rdProjectDescription: string
  expectedOutcomes: string[]
  hasIntellectualProperty: boolean
  patentApplications: number
  isExportOriented: boolean
  expectedCommercializationDate: string
}

interface RAndDDeductionResult {
  businessName: string
  isEligible: boolean
  deductionDetails: {
    standardDeductionRate: number
    enhancedDeductionRate: number
    totalRDExpenses: number
    standardDeduction: number
    enhancedDeduction: number
    extraDeductionAmount: number
  }
  taxSavings: {
    standardTaxSavings: number
    enhancedTaxSavings: number
    additionalTaxSavings: number
    totalTaxSavings: number
  }
  qualifyingExpenses: Array<{
    category: string
    amount: number
    deductionRate: number
    deductionAmount: number
  }>
  eligibilityCriteria: {
    qualifiedPersonnelRequirement: { met: boolean; requirement: string }
    rdFacilityRequirement: { met: boolean; requirement: string }
    projectDurationRequirement: { met: boolean; requirement: string }
    documentationRequirement: { met: boolean; requirement: string }
  }
  additionalBenefits: {
    universityCollaborationBonus: { eligible: boolean; bonus: number }
    ipDevelopmentBonus: { eligible: boolean; bonus: number }
    exportOrientedBonus: { eligible: boolean; bonus: number }
  }
  recommendedActions: string[]
  calculationDate: string
  financeActVersion: string
  validityPeriod: string
  reason?: string
}

export default function RAndDDeductionsCalculator() {
  const [formData, setFormData] = useState<RAndDDeductionForm>({
    businessName: '',
    businessSector: '',
    taxableIncome: 0,
    corporateTaxRate: 25,
    rdExpenses: {
      researchActivities: 0,
      developmentActivities: 0,
      trainingPrograms: 0,
      consultancyFees: 0,
      equipmentCosts: 0,
      personnelCosts: 0,
      materialCosts: 0,
      facilityRentalCosts: 0,
      otherQualifyingCosts: 0
    },
    rdActivities: [],
    hasQualifiedPersonnel: false,
    qualifiedPersonnelCount: 0,
    hasRDFacility: false,
    rdFacilityType: '',
    isCollaboratingWithUniversity: false,
    collaboratingInstitutions: [],
    rdProjectDuration: 0,
    rdProjectDescription: '',
    expectedOutcomes: [],
    hasIntellectualProperty: false,
    patentApplications: 0,
    isExportOriented: false,
    expectedCommercializationDate: ''
  })

  const [result, setResult] = useState<RAndDDeductionResult | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const businessSectors = [
    { value: 'Technology', label: 'Technology' },
    { value: 'Manufacturing', label: 'Manufacturing' },
    { value: 'Agriculture', label: 'Agriculture' },
    { value: 'Healthcare', label: 'Healthcare' },
    { value: 'Energy', label: 'Energy' },
    { value: 'Mining', label: 'Mining' },
    { value: 'Financial Services', label: 'Financial Services' },
    { value: 'Other', label: 'Other' }
  ]

  const rdActivities = [
    'Applied Research',
    'Basic Research',
    'Product Development',
    'Process Innovation',
    'Software Development',
    'Agricultural Research',
    'Medical Research',
    'Environmental Research',
    'Technology Transfer',
    'Innovation Projects'
  ]

  const facilityTypes = [
    'In-house Laboratory',
    'Dedicated R&D Center',
    'Pilot Plant',
    'Testing Facility',
    'Software Development Center',
    'Innovation Hub',
    'Collaborative Research Space',
    'Virtual R&D Setup'
  ]

  const expectedOutcomes = [
    'New Product/Service',
    'Process Improvement',
    'Patent Application',
    'Technology Transfer',
    'Cost Reduction',
    'Quality Enhancement',
    'Environmental Benefit',
    'Export Product'
  ]

  const institutions = [
    'University of Sierra Leone',
    'Njala University',
    'Ernest Bai Koroma University',
    'Milton Margai Technical University',
    'International Research Institutions',
    'Regional Universities',
    'Technical Institutes'
  ]

  const handleActivityChange = (activity: string, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      rdActivities: checked 
        ? [...prev.rdActivities, activity]
        : prev.rdActivities.filter(a => a !== activity)
    }))
  }

  const handleOutcomeChange = (outcome: string, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      expectedOutcomes: checked 
        ? [...prev.expectedOutcomes, outcome]
        : prev.expectedOutcomes.filter(o => o !== outcome)
    }))
  }

  const handleInstitutionChange = (institution: string, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      collaboratingInstitutions: checked 
        ? [...prev.collaboratingInstitutions, institution]
        : prev.collaboratingInstitutions.filter(i => i !== institution)
    }))
  }

  const handleExpenseChange = (category: keyof typeof formData.rdExpenses, value: number) => {
    setFormData(prev => ({
      ...prev,
      rdExpenses: {
        ...prev.rdExpenses,
        [category]: value
      }
    }))
  }

  const totalRDExpenses = Object.values(formData.rdExpenses).reduce((sum, value) => sum + value, 0)

  const handleCalculate = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/financeact2025/rd-deductions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
      })

      if (!response.ok) {
        throw new Error('Failed to calculate R&D tax deductions')
      }

      const data = await response.json()
      setResult(data)
      toast.success('R&D tax deductions calculated successfully')
    } catch (error) {
      console.error('Error calculating deductions:', error)
      toast.error('Failed to calculate R&D tax deductions')
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
          R&D Tax Deductions Calculator
        </h1>
        <p className="text-lg text-gray-600">
          Calculate enhanced 125% tax deductions for research and development expenses under Finance Act 2025
        </p>
      </div>

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Input Form */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Lightbulb className="h-5 w-5" />
                R&D Project Information
              </CardTitle>
              <CardDescription>
                Enter your research and development details to calculate enhanced tax deductions
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
                    <Label htmlFor="taxableIncome">Annual Taxable Income (SLE)</Label>
                    <Input
                      id="taxableIncome"
                      type="number"
                      value={formData.taxableIncome || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, taxableIncome: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="corporateTaxRate">Corporate Tax Rate (%)</Label>
                    <Input
                      id="corporateTaxRate"
                      type="number"
                      value={formData.corporateTaxRate || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, corporateTaxRate: Number(e.target.value) }))}
                      placeholder="25"
                    />
                  </div>
                </div>
              </div>

              <Separator />

              {/* R&D Expenses */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">R&D Expense Breakdown (SLE)</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="researchActivities">Research Activities</Label>
                    <Input
                      id="researchActivities"
                      type="number"
                      value={formData.rdExpenses.researchActivities || ''}
                      onChange={(e) => handleExpenseChange('researchActivities', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="developmentActivities">Development Activities</Label>
                    <Input
                      id="developmentActivities"
                      type="number"
                      value={formData.rdExpenses.developmentActivities || ''}
                      onChange={(e) => handleExpenseChange('developmentActivities', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="personnelCosts">Personnel Costs</Label>
                    <Input
                      id="personnelCosts"
                      type="number"
                      value={formData.rdExpenses.personnelCosts || ''}
                      onChange={(e) => handleExpenseChange('personnelCosts', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="equipmentCosts">Equipment & Technology</Label>
                    <Input
                      id="equipmentCosts"
                      type="number"
                      value={formData.rdExpenses.equipmentCosts || ''}
                      onChange={(e) => handleExpenseChange('equipmentCosts', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="trainingPrograms">Training Programs</Label>
                    <Input
                      id="trainingPrograms"
                      type="number"
                      value={formData.rdExpenses.trainingPrograms || ''}
                      onChange={(e) => handleExpenseChange('trainingPrograms', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="consultancyFees">Consultancy Fees</Label>
                    <Input
                      id="consultancyFees"
                      type="number"
                      value={formData.rdExpenses.consultancyFees || ''}
                      onChange={(e) => handleExpenseChange('consultancyFees', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="materialCosts">Materials & Supplies</Label>
                    <Input
                      id="materialCosts"
                      type="number"
                      value={formData.rdExpenses.materialCosts || ''}
                      onChange={(e) => handleExpenseChange('materialCosts', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="facilityRentalCosts">Facility Costs</Label>
                    <Input
                      id="facilityRentalCosts"
                      type="number"
                      value={formData.rdExpenses.facilityRentalCosts || ''}
                      onChange={(e) => handleExpenseChange('facilityRentalCosts', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div className="md:col-span-2">
                    <Label htmlFor="otherQualifyingCosts">Other Qualifying Costs</Label>
                    <Input
                      id="otherQualifyingCosts"
                      type="number"
                      value={formData.rdExpenses.otherQualifyingCosts || ''}
                      onChange={(e) => handleExpenseChange('otherQualifyingCosts', Number(e.target.value))}
                      placeholder="0"
                    />
                  </div>
                </div>

                <div className="p-3 bg-blue-50 rounded-lg">
                  <div className="flex justify-between items-center">
                    <span className="font-medium">Total R&D Expenses:</span>
                    <span className="text-lg font-bold text-blue-600">
                      {formatCurrency(totalRDExpenses)}
                    </span>
                  </div>
                </div>
              </div>

              <Separator />

              {/* R&D Activities */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">R&D Activities</h3>
                <div className="grid gap-3 md:grid-cols-2">
                  {rdActivities.map(activity => (
                    <div key={activity} className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        id={activity}
                        checked={formData.rdActivities.includes(activity)}
                        onChange={(e) => handleActivityChange(activity, e.target.checked)}
                        className="rounded border-gray-300"
                      />
                      <Label htmlFor={activity} className="text-sm">{activity}</Label>
                    </div>
                  ))}
                </div>
              </div>

              <Separator />

              {/* Project Details */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Project Details</h3>
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label htmlFor="rdProjectDuration">Project Duration (Months)</Label>
                    <Input
                      id="rdProjectDuration"
                      type="number"
                      value={formData.rdProjectDuration || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, rdProjectDuration: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="qualifiedPersonnelCount">Qualified R&D Personnel</Label>
                    <Input
                      id="qualifiedPersonnelCount"
                      type="number"
                      value={formData.qualifiedPersonnelCount || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, qualifiedPersonnelCount: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="rdFacilityType">R&D Facility Type</Label>
                    <Select 
                      value={formData.rdFacilityType} 
                      onValueChange={(value) => setFormData(prev => ({ ...prev, rdFacilityType: value }))}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select facility type" />
                      </SelectTrigger>
                      <SelectContent>
                        {facilityTypes.map(type => (
                          <SelectItem key={type} value={type}>
                            {type}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  
                  <div>
                    <Label htmlFor="patentApplications">Patent Applications</Label>
                    <Input
                      id="patentApplications"
                      type="number"
                      value={formData.patentApplications || ''}
                      onChange={(e) => setFormData(prev => ({ ...prev, patentApplications: Number(e.target.value) }))}
                      placeholder="0"
                    />
                  </div>
                  
                  <div>
                    <Label htmlFor="expectedCommercializationDate">Expected Commercialization Date</Label>
                    <Input
                      id="expectedCommercializationDate"
                      type="date"
                      value={formData.expectedCommercializationDate}
                      onChange={(e) => setFormData(prev => ({ ...prev, expectedCommercializationDate: e.target.value }))}
                    />
                  </div>
                </div>

                <div>
                  <Label htmlFor="rdProjectDescription">Project Description</Label>
                  <textarea
                    id="rdProjectDescription"
                    className="w-full min-h-[80px] px-3 py-2 text-sm border border-gray-300 rounded-md"
                    value={formData.rdProjectDescription}
                    onChange={(e) => setFormData(prev => ({ ...prev, rdProjectDescription: e.target.value }))}
                    placeholder="Describe your R&D project objectives and expected outcomes"
                  />
                </div>

                <div className="space-y-3">
                  <div className="flex items-center space-x-2">
                    <Switch
                      id="hasQualifiedPersonnel"
                      checked={formData.hasQualifiedPersonnel}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, hasQualifiedPersonnel: checked }))}
                    />
                    <Label htmlFor="hasQualifiedPersonnel">Has qualified R&D personnel</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="hasRDFacility"
                      checked={formData.hasRDFacility}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, hasRDFacility: checked }))}
                    />
                    <Label htmlFor="hasRDFacility">Has dedicated R&D facility</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="isCollaboratingWithUniversity"
                      checked={formData.isCollaboratingWithUniversity}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isCollaboratingWithUniversity: checked }))}
                    />
                    <Label htmlFor="isCollaboratingWithUniversity">Collaborating with universities/institutions</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="hasIntellectualProperty"
                      checked={formData.hasIntellectualProperty}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, hasIntellectualProperty: checked }))}
                    />
                    <Label htmlFor="hasIntellectualProperty">Developing intellectual property</Label>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Switch
                      id="isExportOriented"
                      checked={formData.isExportOriented}
                      onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isExportOriented: checked }))}
                    />
                    <Label htmlFor="isExportOriented">Export-oriented R&D</Label>
                  </div>
                </div>
              </div>

              <Separator />

              {/* Expected Outcomes */}
              <div className="space-y-4">
                <h3 className="text-lg font-medium">Expected Outcomes</h3>
                <div className="grid gap-3 md:grid-cols-2">
                  {expectedOutcomes.map(outcome => (
                    <div key={outcome} className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        id={outcome}
                        checked={formData.expectedOutcomes.includes(outcome)}
                        onChange={(e) => handleOutcomeChange(outcome, e.target.checked)}
                        className="rounded border-gray-300"
                      />
                      <Label htmlFor={outcome} className="text-sm">{outcome}</Label>
                    </div>
                  ))}
                </div>
              </div>

              {/* Collaborating Institutions */}
              {formData.isCollaboratingWithUniversity && (
                <>
                  <Separator />
                  <div className="space-y-4">
                    <h3 className="text-lg font-medium">Collaborating Institutions</h3>
                    <div className="grid gap-3 md:grid-cols-1">
                      {institutions.map(institution => (
                        <div key={institution} className="flex items-center space-x-2">
                          <input
                            type="checkbox"
                            id={institution}
                            checked={formData.collaboratingInstitutions.includes(institution)}
                            onChange={(e) => handleInstitutionChange(institution, e.target.checked)}
                            className="rounded border-gray-300"
                          />
                          <Label htmlFor={institution} className="text-sm">{institution}</Label>
                        </div>
                      ))}
                    </div>
                  </div>
                </>
              )}

              <Button 
                onClick={handleCalculate} 
                disabled={isLoading || !formData.businessName || totalRDExpenses === 0}
                className="w-full"
              >
                {isLoading ? 'Calculating...' : 'Calculate R&D Tax Deductions'}
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
                    Deduction Status
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
                          <p className="text-sm text-gray-600">Enhanced Deduction Rate</p>
                          <Badge variant="secondary" className="bg-blue-100 text-blue-800">
                            {result.deductionDetails.enhancedDeductionRate}%
                          </Badge>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Total R&D Expenses</p>
                          <p className="text-lg font-semibold">
                            {formatCurrency(result.deductionDetails.totalRDExpenses)}
                          </p>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Extra Deduction Amount</p>
                          <p className="text-lg font-bold text-blue-600">
                            {formatCurrency(result.deductionDetails.extraDeductionAmount)}
                          </p>
                        </div>
                        
                        <div>
                          <p className="text-sm text-gray-600">Total Tax Savings</p>
                          <p className="text-2xl font-bold text-green-600">
                            {formatCurrency(result.taxSavings.totalTaxSavings)}
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

              {/* Tax Savings Breakdown */}
              {result.isEligible && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Lightbulb className="h-5 w-5 text-yellow-600" />
                      Tax Savings Breakdown
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      <div className="flex items-center justify-between">
                        <span className="text-sm">Standard Deduction Savings</span>
                        <span className="text-sm font-medium">
                          {formatCurrency(result.taxSavings.standardTaxSavings)}
                        </span>
                      </div>
                      
                      <div className="flex items-center justify-between">
                        <span className="text-sm">Enhanced Deduction Savings</span>
                        <span className="text-sm font-medium text-green-600">
                          {formatCurrency(result.taxSavings.enhancedTaxSavings)}
                        </span>
                      </div>
                      
                      {result.taxSavings.additionalTaxSavings > 0 && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Additional Incentive Savings</span>
                          <span className="text-sm font-medium text-blue-600">
                            {formatCurrency(result.taxSavings.additionalTaxSavings)}
                          </span>
                        </div>
                      )}
                      
                      <div className="border-t pt-2">
                        <div className="flex items-center justify-between font-medium">
                          <span>Total Savings</span>
                          <span className="text-green-600">
                            {formatCurrency(result.taxSavings.totalTaxSavings)}
                          </span>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Qualifying Expenses */}
              {result.isEligible && result.qualifyingExpenses.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle>Qualifying Expenses</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {result.qualifyingExpenses.map((expense, idx) => (
                        <div key={idx} className="border-l-2 border-blue-500 pl-3">
                          <p className="font-medium text-sm">{expense.category}</p>
                          <p className="text-sm text-gray-600">
                            {formatCurrency(expense.amount)} × {expense.deductionRate}% = 
                            <span className="font-medium text-green-600 ml-1">
                              {formatCurrency(expense.deductionAmount)}
                            </span>
                          </p>
                        </div>
                      ))}
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
                      {result.additionalBenefits.universityCollaborationBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">University Collaboration Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalBenefits.universityCollaborationBonus.bonus)}
                          </span>
                        </div>
                      )}
                      
                      {result.additionalBenefits.ipDevelopmentBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">IP Development Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalBenefits.ipDevelopmentBonus.bonus)}
                          </span>
                        </div>
                      )}
                      
                      {result.additionalBenefits.exportOrientedBonus.eligible && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm">Export-Oriented Bonus</span>
                          <span className="text-sm font-medium text-green-600">
                            {formatCurrency(result.additionalBenefits.exportOrientedBonus.bonus)}
                          </span>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Recommended Actions */}
              {result.isEligible && result.recommendedActions.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle>Recommended Actions</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ul className="space-y-2">
                      {result.recommendedActions.map((action, idx) => (
                        <li key={idx} className="text-sm flex items-center gap-2">
                          <DollarSign className="h-3 w-3 text-blue-600" />
                          {action}
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
                  Enter your R&D project information to calculate enhanced tax deductions
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}