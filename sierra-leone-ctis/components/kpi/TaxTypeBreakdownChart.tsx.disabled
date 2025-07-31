'use client';

import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';
import { Badge } from '@/components/ui/badge';
import { TaxTypeMetrics } from '@/lib/types/kpi';

interface TaxTypeBreakdownChartProps {
  data: TaxTypeMetrics[];
  height?: number;
}

export default function TaxTypeBreakdownChart({ 
  data, 
  height = 300 
}: TaxTypeBreakdownChartProps) {
  const chartData = data.map(item => ({
    name: item.taxType,
    compliance: item.complianceRate,
    filings: item.totalFilings,
    onTime: item.onTimeFilings,
    amount: item.totalAmount,
    clients: item.clientCount
  }));

  const getBarColor = (compliance: number) => {
    if (compliance >= 95) return '#10b981'; // Green
    if (compliance >= 85) return '#f59e0b'; // Gold
    if (compliance >= 70) return '#ef4444'; // Red
    return '#6b7280'; // Gray
  };

  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      const data = payload[0].payload;
      return (
        <div className="bg-white p-4 border border-gray-200 rounded-lg shadow-md">
          <p className="font-medium text-gray-900 mb-2">{label}</p>
          <div className="space-y-1 text-sm">
            <p className="text-sierra-blue-600">
              Compliance: <span className="font-semibold">{data.compliance.toFixed(1)}%</span>
            </p>
            <p className="text-gray-600">
              Filings: <span className="font-semibold">{data.onTime}/{data.filings}</span>
            </p>
            <p className="text-gray-600">
              Amount: <span className="font-semibold">SLE {data.amount.toLocaleString()}</span>
            </p>
            <p className="text-gray-600">
              Clients: <span className="font-semibold">{data.clients}</span>
            </p>
          </div>
        </div>
      );
    }
    return null;
  };

  if (!data || data.length === 0) {
    return (
      <div className="flex items-center justify-center h-[300px] text-gray-500">
        <div className="text-center">
          <p className="text-lg font-medium">No tax type data available</p>
          <p className="text-sm">Data will appear as tax filings are processed</p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <ResponsiveContainer width="100%" height={height}>
        <BarChart
          data={chartData}
          margin={{
            top: 20,
            right: 30,
            left: 20,
            bottom: 5,
          }}
        >
          <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
          <XAxis 
            dataKey="name"
            stroke="#6b7280"
            fontSize={12}
            tickLine={false}
            axisLine={false}
          />
          <YAxis 
            stroke="#6b7280"
            fontSize={12}
            tickLine={false}
            axisLine={false}
            domain={[0, 100]}
            tickFormatter={(value) => `${value}%`}
          />
          <Tooltip content={<CustomTooltip />} />
          <Bar dataKey="compliance" radius={[4, 4, 0, 0]}>
            {chartData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={getBarColor(entry.compliance)} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
      
      {/* Tax Type Summary Cards */}
      <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {data.map((item, index) => (
          <div key={item.taxType} className="p-3 border rounded-lg bg-gray-50">
            <div className="flex items-center justify-between mb-2">
              <h4 className="font-medium text-gray-900">{item.taxType}</h4>
              <Badge 
                variant={
                  item.complianceRate >= 95 ? 'default' :
                  item.complianceRate >= 85 ? 'secondary' : 'destructive'
                }
                className="text-xs"
              >
                {item.complianceRate.toFixed(1)}%
              </Badge>
            </div>
            
            <div className="space-y-1 text-xs text-gray-600">
              <div className="flex justify-between">
                <span>Filings:</span>
                <span className="font-medium">{item.onTimeFilings}/{item.totalFilings}</span>
              </div>
              <div className="flex justify-between">
                <span>Amount:</span>
                <span className="font-medium">SLE {item.totalAmount.toLocaleString()}</span>
              </div>
              <div className="flex justify-between">
                <span>Clients:</span>
                <span className="font-medium">{item.clientCount}</span>
              </div>
            </div>
            
            {/* Progress bar */}
            <div className="mt-2">
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className="h-2 rounded-full transition-all duration-300"
                  style={{
                    width: `${item.complianceRate}%`,
                    backgroundColor: getBarColor(item.complianceRate)
                  }}
                />
              </div>
            </div>
          </div>
        ))}
      </div>
      
      {/* Overall Summary */}
      <div className="mt-4 p-4 bg-sierra-blue-50 rounded-lg">
        <h4 className="font-medium text-sierra-blue-900 mb-2">Overall Summary</h4>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
          <div>
            <p className="text-2xl font-bold text-sierra-blue-600">
              {data.reduce((sum, item) => sum + item.totalFilings, 0)}
            </p>
            <p className="text-xs text-gray-600">Total Filings</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-sierra-green-600">
              {data.reduce((sum, item) => sum + item.onTimeFilings, 0)}
            </p>
            <p className="text-xs text-gray-600">On Time</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-sierra-gold-600">
              SLE {data.reduce((sum, item) => sum + item.totalAmount, 0).toLocaleString()}
            </p>
            <p className="text-xs text-gray-600">Total Amount</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-sierra-blue-600">
              {Math.max(...data.map(item => item.clientCount))}
            </p>
            <p className="text-xs text-gray-600">Active Clients</p>
          </div>
        </div>
      </div>
    </div>
  );
}