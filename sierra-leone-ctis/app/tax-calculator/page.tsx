'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { 
  Calculator, 
  DollarSign, 
  Users, 
  Package, 
  Scale,
  PieChart,
  TrendingUp,
  FileText,
  AlertCircle,
  Info,
  CheckCircle
} from 'lucide-react';
import IncomeTaxCalculatorForm from '@/components/tax-calculators/IncomeTaxCalculatorForm';
import GstCalculatorForm from '@/components/tax-calculators/GstCalculatorForm';
import PayrollTaxCalculatorForm from '@/components/tax-calculators/PayrollTaxCalculatorForm';
import ExciseDutyCalculatorForm from '@/components/tax-calculators/ExciseDutyCalculatorForm';
import PenaltyCalculatorForm from '@/components/tax-calculators/PenaltyCalculatorForm';
import ComprehensiveTaxAssessmentForm from '@/components/tax-calculators/ComprehensiveTaxAssessmentForm';

export default function TaxCalculatorPage() {
  const [activeCalculator, setActiveCalculator] = useState('overview');

  const calculators = [
    {
      id: 'income-tax',
      title: 'Income Tax Calculator',
      description: 'Calculate individual and corporate income tax based on Finance Act 2025',
      icon: DollarSign,
      color: 'text-sierra-blue-600',
      bgColor: 'bg-sierra-blue-50',
      borderColor: 'border-sierra-blue-200',
      features: ['Progressive tax brackets', 'Tax allowances', 'Minimum tax calculation', 'Penalty calculation']
    },
    {
      id: 'gst',
      title: 'GST Calculator',
      description: 'Calculate Goods and Services Tax with input/output tax credits',
      icon: TrendingUp,
      color: 'text-sierra-gold-600',
      bgColor: 'bg-sierra-gold-50',
      borderColor: 'border-sierra-gold-200',
      features: ['15% standard rate', 'Input tax credits', 'Export zero-rating', 'Reverse charge GST']
    },
    {
      id: 'payroll',
      title: 'Payroll Tax Calculator',
      description: 'Calculate PAYE and Skills Development Levy for employees',
      icon: Users,
      color: 'text-sierra-green-600',
      bgColor: 'bg-sierra-green-50',
      borderColor: 'border-sierra-green-200',
      features: ['PAYE calculation', 'Skills Development Levy', 'Employee breakdown', 'Compliance tracking']
    },
    {
      id: 'excise',
      title: 'Excise Duty Calculator',
      description: 'Calculate excise duty on tobacco, alcohol, and fuel products',
      icon: Package,
      color: 'text-purple-600',
      bgColor: 'bg-purple-50',
      borderColor: 'border-purple-200',
      features: ['Specific duty rates', 'Ad valorem rates', 'Product categorization', 'Duty breakdown']
    },
    {
      id: 'penalty',
      title: 'Penalty Calculator',
      description: 'Calculate penalties for late filing and payment',
      icon: Scale,
      color: 'text-red-600',
      bgColor: 'bg-red-50',
      borderColor: 'border-red-200',
      features: ['Late filing penalties', 'Daily interest calculation', 'Penalty scenarios', 'Mitigation advice']
    },
    {
      id: 'comprehensive',
      title: 'Comprehensive Assessment',
      description: 'Complete tax assessment covering all tax types',
      icon: PieChart,
      color: 'text-indigo-600',
      bgColor: 'bg-indigo-50',
      borderColor: 'border-indigo-200',
      features: ['All tax types', 'Compliance scoring', 'Issue identification', 'Recommendations']
    },
  ];

  const stats = [
    {
      title: 'Tax Types Covered',
      value: '4',
      description: 'Income Tax, GST, Payroll Tax, Excise Duty',
      icon: FileText,
      color: 'text-sierra-blue-600'
    },
    {
      title: 'Finance Act 2025',
      value: 'Updated',
      description: 'Latest rates and regulations',
      icon: CheckCircle,
      color: 'text-sierra-green-600'
    },
    {
      title: 'Penalty Calculation',
      value: 'Included',
      description: 'Late filing and payment penalties',
      icon: AlertCircle,
      color: 'text-red-600'
    },
    {
      title: 'Compliance Scoring',
      value: 'A-F Grade',
      description: 'Comprehensive compliance assessment',
      icon: TrendingUp,
      color: 'text-sierra-gold-600'
    },
  ];

  const renderCalculatorContent = () => {
    switch (activeCalculator) {
      case 'income-tax':
        return <IncomeTaxCalculatorForm />;
      case 'gst':
        return <GstCalculatorForm />;
      case 'payroll':
        return <PayrollTaxCalculatorForm />;
      case 'excise':
        return <ExciseDutyCalculatorForm />;
      case 'penalty':
        return <PenaltyCalculatorForm />;
      case 'comprehensive':
        return <ComprehensiveTaxAssessmentForm />;
      default:
        return null;
    }
  };

  if (activeCalculator !== 'overview') {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              {calculators.find(calc => calc.id === activeCalculator)?.title}
            </h1>
            <p className="text-muted-foreground mt-2">
              {calculators.find(calc => calc.id === activeCalculator)?.description}
            </p>
          </div>
          <Button variant="outline" onClick={() => setActiveCalculator('overview')}>
            ← Back to Overview
          </Button>
        </div>
        {renderCalculatorContent()}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sierra Leone Tax Calculator</h1>
          <p className="text-muted-foreground mt-2">
            Comprehensive tax calculation tools based on Finance Act 2025
          </p>
        </div>
        <Badge variant="secondary" className="text-sm">
          Updated for 2025
        </Badge>
      </div>

      {/* Statistics */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat, index) => {
          const Icon = stat.icon;
          return (
            <Card key={index}>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">{stat.title}</p>
                    <p className={`text-2xl font-bold ${stat.color}`}>{stat.value}</p>
                    <p className="text-xs text-muted-foreground mt-1">{stat.description}</p>
                  </div>
                  <Icon className={`h-6 w-6 ${stat.color}`} />
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Calculator Grid */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {calculators.map((calc) => {
          const Icon = calc.icon;
          return (
            <Card key={calc.id} className={`${calc.borderColor} ${calc.bgColor} hover:shadow-lg transition-shadow cursor-pointer`}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className={`p-2 bg-white rounded-lg shadow-sm`}>
                      <Icon className={`h-6 w-6 ${calc.color}`} />
                    </div>
                    <div>
                      <CardTitle className="text-lg">{calc.title}</CardTitle>
                    </div>
                  </div>
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
                          <span className={`w-1.5 h-1.5 ${calc.color.replace('text-', 'bg-')} rounded-full flex-shrink-0`}></span>
                          {feature}
                        </li>
                      ))}
                    </ul>
                  </div>
                  
                  <Button 
                    className="w-full" 
                    onClick={() => setActiveCalculator(calc.id)}
                  >
                    <Calculator className="mr-2 h-4 w-4" />
                    Open Calculator
                  </Button>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Finance Act 2025 Information */}
      <Card className="border-sierra-blue-200 bg-sierra-blue-50">
        <CardHeader>
          <CardTitle className="text-sierra-blue-800 flex items-center gap-2">
            <span className="text-lg">🇸🇱</span>
            Sierra Leone Finance Act 2025 Compliance
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Tabs defaultValue="rates" className="w-full">
            <TabsList className="grid w-full grid-cols-4">
              <TabsTrigger value="rates">Tax Rates</TabsTrigger>
              <TabsTrigger value="thresholds">Thresholds</TabsTrigger>
              <TabsTrigger value="deadlines">Deadlines</TabsTrigger>
              <TabsTrigger value="changes">2025 Changes</TabsTrigger>
            </TabsList>
            
            <TabsContent value="rates" className="mt-4">
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Income Tax</h4>
                  <p className="text-sm text-sierra-blue-700">0%, 15%, 20%, 30% progressive rates</p>
                </div>
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">GST</h4>
                  <p className="text-sm text-sierra-blue-700">15% standard rate</p>
                </div>
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Skills Development</h4>
                  <p className="text-sm text-sierra-blue-700">2.5% of monthly payroll</p>
                </div>
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Withholding Tax</h4>
                  <p className="text-sm text-sierra-blue-700">15% on various payments</p>
                </div>
              </div>
            </TabsContent>
            
            <TabsContent value="thresholds" className="mt-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">GST Registration</h4>
                  <p className="text-sm text-sierra-blue-700">SLE 500,000,000 annual turnover</p>
                </div>
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Taxpayer Categories</h4>
                  <p className="text-sm text-sierra-blue-700">Large: >2B, Medium: 500M-2B, Small: 100M-500M, Micro: <100M</p>
                </div>
              </div>
            </TabsContent>
            
            <TabsContent value="deadlines" className="mt-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Monthly Returns</h4>
                  <p className="text-sm text-sierra-blue-700">GST and PAYE by 15th of following month</p>
                </div>
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Annual Returns</h4>
                  <p className="text-sm text-sierra-blue-700">Income tax by April 30th</p>
                </div>
              </div>
            </TabsContent>
            
            <TabsContent value="changes" className="mt-4">
              <div className="space-y-3">
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Enhanced Penalty Structure</h4>
                  <p className="text-sm text-sierra-blue-700">Increased penalties for late filing and payment</p>
                </div>
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Digital Filing Requirements</h4>
                  <p className="text-sm text-sierra-blue-700">Mandatory electronic filing for large taxpayers</p>
                </div>
                <div className="p-4 bg-white rounded-lg">
                  <h4 className="font-medium text-sierra-blue-800">Compliance Monitoring</h4>
                  <p className="text-sm text-sierra-blue-700">Enhanced monitoring and audit procedures</p>
                </div>
              </div>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      {/* Disclaimer */}
      <Card className="border-gray-200 bg-gray-50">
        <CardContent className="p-4">
          <div className="flex items-start gap-3">
            <Info className="h-5 w-5 text-gray-500 mt-0.5 flex-shrink-0" />
            <div className="text-sm text-gray-600">
              <p className="font-medium mb-1">Important Notice</p>
              <p>
                These calculators provide estimates based on current Sierra Leone tax legislation and Finance Act 2025. 
                Tax calculations may vary based on specific circumstances, exemptions, and regulatory changes. 
                Always consult with a qualified tax professional or the National Revenue Authority (NRA) for specific advice and verification of tax obligations.
              </p>
              <p className="mt-2 text-xs">
                Last updated: {new Date().toLocaleDateString()} | Version: 2025.1.0
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}