"use client"

import React from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Calculator, Building2, Leaf, Users, Truck, Lightbulb } from 'lucide-react'
import Link from 'next/link'

export default function TaxCalculatorsPage() {
  const calculators = [
    {
      id: 'investment-incentives',
      title: 'Investment Incentives Calculator',
      description: 'Calculate eligibility for Finance Act 2025 investment incentives including employment-based exemptions and agribusiness benefits',
      icon: Building2,
      badge: 'Finance Act 2025',
      badgeVariant: 'default' as const,
      features: [
        '5-year & 10-year tax exemptions',
        'Employment-based benefits',
        'Local ownership requirements',
        'Comprehensive savings analysis'
      ],
      href: '/calculators/investment-incentives'
    },
    {
      id: 'agribusiness-exemptions',
      title: 'Agribusiness Tax Exemptions',
      description: 'Determine eligibility for agricultural sector tax exemptions and import duty benefits',
      icon: Leaf,
      badge: 'Finance Act 2025',
      badgeVariant: 'secondary' as const,
      features: [
        'Large-scale cultivation benefits',
        'Livestock investment exemptions',
        'Farm machinery duty exemptions',
        'Agro-processing equipment benefits'
      ],
      href: '/calculators/agribusiness-exemptions'
    },
    {
      id: 'employment-exemptions',
      title: 'Employment-Based Exemptions',
      description: 'Calculate tax exemptions based on employment creation and investment levels',
      icon: Users,
      badge: 'Finance Act 2025',
      badgeVariant: 'secondary' as const,
      features: [
        '100+ employees: 5-year exemption',
        '150+ employees: 10-year exemption',
        'Minimum investment thresholds',
        'Local ownership requirements'
      ],
      href: '/calculators/employment-exemptions'
    },
    {
      id: 'duty-free-imports',
      title: 'Duty-Free Import Calculator',
      description: 'Calculate savings from duty-free import provisions for qualifying businesses',
      icon: Truck,
      badge: 'Finance Act 2025',
      badgeVariant: 'secondary' as const,
      features: [
        'New business provisions',
        'Expansion incentives',
        '3-year duty-free periods',
        'Machinery & equipment focus'
      ],
      href: '/calculators/duty-free-imports'
    },
    {
      id: 'rd-deductions',
      title: 'R&D Tax Deductions',
      description: 'Calculate enhanced 125% tax deductions for research and development expenses',
      icon: Lightbulb,
      badge: 'Finance Act 2025',
      badgeVariant: 'secondary' as const,
      features: [
        '125% deduction rate',
        'R&D activities',
        'Training expenses',
        'Innovation projects'
      ],
      href: '/calculators/rd-deductions'
    },
    {
      id: 'basic-tax',
      title: 'Basic Tax Calculators',
      description: 'Standard Sierra Leone tax calculations including income tax, GST, and withholding tax',
      icon: Calculator,
      badge: 'Standard Rates',
      badgeVariant: 'outline' as const,
      features: [
        'Individual & corporate income tax',
        'GST calculations',
        'Withholding tax (15%)',
        'PAYE calculations'
      ],
      href: '/calculators/basic-tax'
    }
  ]

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Sierra Leone Tax Calculators
        </h1>
        <p className="text-lg text-gray-600 mb-4">
          Comprehensive tax calculation tools based on the latest Sierra Leone Finance Acts
        </p>
        <div className="flex items-center gap-4 text-sm text-gray-500">
          <span>📅 Updated for Finance Act 2025</span>
          <span>🏛️ Compliant with NRA regulations</span>
          <span>⚡ Real-time calculations</span>
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {calculators.map((calc) => {
          const Icon = calc.icon
          return (
            <Card key={calc.id} className="h-full hover:shadow-lg transition-shadow">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="p-2 bg-blue-100 rounded-lg">
                      <Icon className="h-6 w-6 text-blue-600" />
                    </div>
                    <div>
                      <CardTitle className="text-lg">{calc.title}</CardTitle>
                    </div>
                  </div>
                  <Badge variant={calc.badgeVariant} className="text-xs">
                    {calc.badge}
                  </Badge>
                </div>
                <CardDescription className="text-sm">
                  {calc.description}
                </CardDescription>
              </CardHeader>
              
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <h4 className="text-sm font-medium text-gray-900 mb-2">Key Features:</h4>
                    <ul className="space-y-1">
                      {calc.features.map((feature, index) => (
                        <li key={index} className="text-sm text-gray-600 flex items-center gap-2">
                          <span className="w-1.5 h-1.5 bg-blue-600 rounded-full flex-shrink-0"></span>
                          {feature}
                        </li>
                      ))}
                    </ul>
                  </div>
                  
                  <Button asChild className="w-full">
                    <Link href={calc.href}>
                      Open Calculator
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>

      <div className="mt-12 bg-blue-50 rounded-lg p-6">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">
          Finance Act 2025 Highlights
        </h2>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <div className="bg-white rounded-lg p-4">
            <h3 className="font-medium text-gray-900 mb-2">Investment Incentives</h3>
            <p className="text-sm text-gray-600">
              Up to 10-year corporate tax exemptions for qualifying businesses with significant employment and investment commitments.
            </p>
          </div>
          <div className="bg-white rounded-lg p-4">
            <h3 className="font-medium text-gray-900 mb-2">Agribusiness Benefits</h3>
            <p className="text-sm text-gray-600">
              Full corporate tax exemptions and import duty relief for large-scale agricultural operations.
            </p>
          </div>
          <div className="bg-white rounded-lg p-4">
            <h3 className="font-medium text-gray-900 mb-2">R&D Incentives</h3>
            <p className="text-sm text-gray-600">
              Enhanced 125% tax deductions for research, development, and training activities.
            </p>
          </div>
        </div>
      </div>

      <div className="mt-8 text-center text-sm text-gray-500">
        <p>
          ⚠️ These calculators provide estimates based on current tax legislation. 
          Consult with a qualified tax professional for specific advice.
        </p>
      </div>
    </div>
  )
}