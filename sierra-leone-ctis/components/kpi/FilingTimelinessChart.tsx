'use client';

import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine } from 'recharts';
import { TrendDataPoint } from '@/lib/types/kpi';

interface FilingTimelinessChartProps {
  data: TrendDataPoint[];
  height?: number;
}

export default function FilingTimelinessChart({ 
  data, 
  height = 200 
}: FilingTimelinessChartProps) {
  const chartData = data.map(point => ({
    name: point.label,
    days: point.value,
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
          <p className={`${
            data.value <= 3 ? 'text-sierra-green-600' :
            data.value <= 7 ? 'text-sierra-gold-600' : 'text-red-600'
          }`}>
            Filing Delay: <span className="font-semibold">{data.value.toFixed(1)} days</span>
          </p>
        </div>
      );
    }
    return null;
  };

  if (!data || data.length === 0) {
    return (
      <div className="flex items-center justify-center h-[200px] text-gray-500">
        <div className="text-center">
          <p className="font-medium">No filing history available</p>
          <p className="text-sm">Data will appear as you file tax returns</p>
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
            tickFormatter={(value) => `${value}d`}
          />
          <Tooltip content={<CustomTooltip />} />
          
          {/* Target reference lines */}
          <ReferenceLine 
            y={3} 
            stroke="#10b981" 
            strokeDasharray="5 5"
            strokeWidth={1}
            label={{ value: "Excellent (≤3 days)", position: "top", fontSize: 10 }}
          />
          <ReferenceLine 
            y={7} 
            stroke="#f59e0b" 
            strokeDasharray="5 5"
            strokeWidth={1}
            label={{ value: "Good (≤7 days)", position: "top", fontSize: 10 }}
          />
          
          {/* Filing timeliness line */}
          <Line
            type="monotone"
            dataKey="days"
            stroke="#3b82f6"
            strokeWidth={3}
            dot={{ fill: '#3b82f6', strokeWidth: 2, r: 4 }}
            activeDot={{ r: 6, stroke: '#3b82f6', strokeWidth: 2 }}
          />
        </LineChart>
      </ResponsiveContainer>
      
      {/* Performance Summary */}
      <div className="mt-3 text-center">
        <div className="flex justify-center space-x-6 text-sm">
          <div>
            <span className="font-medium text-sierra-blue-600">
              {chartData.length > 0 ? chartData[chartData.length - 1].days.toFixed(1) : '0'} days
            </span>
            <p className="text-xs text-gray-500">Latest</p>
          </div>
          <div>
            <span className="font-medium text-sierra-green-600">
              {chartData.length > 0 ? Math.min(...chartData.map(d => d.days)).toFixed(1) : '0'} days
            </span>
            <p className="text-xs text-gray-500">Best</p>
          </div>
          <div>
            <span className="font-medium text-gray-600">
              {chartData.length > 0 ? (chartData.reduce((sum, d) => sum + d.days, 0) / chartData.length).toFixed(1) : '0'} days
            </span>
            <p className="text-xs text-gray-500">Average</p>
          </div>
        </div>
      </div>
    </div>
  );
}