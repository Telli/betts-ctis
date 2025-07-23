"use client"

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ComplianceOverview as ComplianceOverviewType } from '@/lib/services'
import { PieChart, Pie, BarChart, Bar, Cell, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from 'recharts'

interface ComplianceOverviewProps {
  complianceOverview: ComplianceOverviewType
  className?: string
}

export default function ComplianceOverview({ complianceOverview, className = '' }: ComplianceOverviewProps) {
  // Format data for the filing status pie chart with Sierra Leone colors
  const filingStatusData = [
    { name: 'Filed', value: complianceOverview.completedFilings, color: '#22c55e' }, // sierra-green
    { name: 'Pending', value: complianceOverview.pendingFilings, color: '#eab308' }, // sierra-gold
    { name: 'Overdue', value: complianceOverview.lateFilings, color: '#ef4444' }, // red
  ]

  // Format data for the tax type breakdown chart with Sierra Leone tax colors
  const sierraLeoneColors = ['#1e40af', '#eab308', '#22c55e', '#7c3aed'] // sierra-blue, sierra-gold, sierra-green, purple
  const taxTypeData = Object.entries(complianceOverview.taxTypeBreakdown).map(([name, value], index) => ({
    name: name.replace(/([A-Z])/g, ' $1').trim(), // Format tax type names
    value,
    color: sierraLeoneColors[index % sierraLeoneColors.length]
  }))

  // Format data for the monthly revenue chart
  const monthlyRevenueData = Object.entries(complianceOverview.monthlyRevenue)
    .map(([month, amount]) => ({
      month,
      amount
    }))
    .reverse() // Show oldest months first

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>Compliance Overview</CardTitle>
        <CardDescription>
          Filing status and revenue statistics
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <h4 className="text-sm font-semibold mb-3">Filing Status</h4>
            <div className="h-[200px]">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={filingStatusData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="value"
                    label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                  >
                    {filingStatusData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip 
                    formatter={(value: number) => [`${value} filings`, 'Count']} 
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="flex justify-center gap-4 mt-2">
              {filingStatusData.map((item) => (
                <div key={item.name} className="flex items-center gap-1">
                  <div className="w-3 h-3 rounded-full" style={{ backgroundColor: item.color }} />
                  <span className="text-xs">{item.name}: {item.value}</span>
                </div>
              ))}
            </div>
          </div>

          <div>
            <h4 className="text-sm font-semibold mb-3">Tax Types</h4>
            <div className="h-[200px]">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={taxTypeData}>
                  <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                  <YAxis tick={{ fontSize: 12 }} />
                  <Tooltip formatter={(value: number) => [`${value} filings`, 'Count']} />
                  <Bar dataKey="value">
                    {taxTypeData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </div>

        <div className="mt-6">
          <h4 className="text-sm font-semibold mb-3">Monthly Revenue</h4>
          <div className="h-[200px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={monthlyRevenueData}>
                <XAxis dataKey="month" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip formatter={(value: number) => [
                  value.toLocaleString('en-US', { style: 'currency', currency: 'SLE' }), 
                  'Revenue'
                ]} />
                <Bar dataKey="amount" fill="#1e40af" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
