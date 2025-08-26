'use client';

import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine } from 'recharts';
import { TrendDataPoint } from '@/lib/types/kpi';

interface ComplianceTrendChartProps {
  data: TrendDataPoint[];
  target?: number;
  height?: number;
}

export default function ComplianceTrendChart({ 
  data, 
  target = 85, 
  height = 300 
}: ComplianceTrendChartProps) {
  const chartData = data.map(point => ({
    name: point.label,
    value: point.value,
    date: new Date(point.date).toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric' 
    })
  }));

  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      const data = payload[0];
      return (
        <div className="bg-white p-3 border border-gray-200 rounded-lg shadow-md">
          <p className="font-medium text-gray-900">{label}</p>
          <p className="text-sierra-blue-600">
            Compliance: <span className="font-semibold">{data.value.toFixed(1)}%</span>
          </p>
          {target && (
            <p className="text-gray-500 text-sm">
              Target: {target}%
            </p>
          )}
        </div>
      );
    }
    return null;
  };

  const getLineColor = (value: number) => {
    if (value >= target) return '#10b981'; // Green
    if (value >= target * 0.9) return '#f59e0b'; // Yellow/Gold
    return '#ef4444'; // Red
  };

  if (!data || data.length === 0) {
    return (
      <div className="flex items-center justify-center h-[300px] text-gray-500">
        <div className="text-center">
          <p className="text-lg font-medium">No trend data available</p>
          <p className="text-sm">Data will appear as compliance metrics are calculated</p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full">
      <ResponsiveContainer width="100%" height={height}>
        <LineChart
          data={chartData}
          margin={{
            top: 5,
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
          
          {/* Target reference line */}
          {target && (
            <ReferenceLine 
              y={target} 
              stroke="#10b981" 
              strokeDasharray="5 5"
              strokeWidth={2}
              label={{ value: `Target: ${target}%`, position: "top" }}
            />
          )}
          
          {/* Main compliance line */}
          <Line
            type="monotone"
            dataKey="value"
            stroke="#3b82f6"
            strokeWidth={3}
            dot={{ fill: '#3b82f6', strokeWidth: 2, r: 4 }}
            activeDot={{ r: 6, stroke: '#3b82f6', strokeWidth: 2 }}
          />
        </LineChart>
      </ResponsiveContainer>
      
      {/* Chart Summary */}
      <div className="mt-4 grid grid-cols-3 gap-4 text-center">
        <div>
          <p className="text-2xl font-bold text-sierra-blue-600">
            {chartData.length > 0 ? chartData[chartData.length - 1].value.toFixed(1) : '0'}%
          </p>
          <p className="text-xs text-gray-500">Current</p>
        </div>
        <div>
          <p className="text-2xl font-bold text-sierra-green-600">
            {target}%
          </p>
          <p className="text-xs text-gray-500">Target</p>
        </div>
        <div>
          <p className={`text-2xl font-bold ${
            chartData.length > 1 
              ? (chartData[chartData.length - 1].value > chartData[chartData.length - 2].value 
                  ? 'text-sierra-green-600' 
                  : 'text-red-600')
              : 'text-gray-400'
          }`}>
            {chartData.length > 1 
              ? (chartData[chartData.length - 1].value - chartData[chartData.length - 2].value > 0 ? '+' : '') +
                (chartData[chartData.length - 1].value - chartData[chartData.length - 2].value).toFixed(1)
              : '0'}%
          </p>
          <p className="text-xs text-gray-500">Change</p>
        </div>
      </div>
    </div>
  );
}