import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';

export interface MetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  trend?: 'up' | 'down' | 'neutral';
  trendValue?: string;
  icon?: React.ReactNode;
  color?: 'primary' | 'success' | 'warning' | 'danger' | 'info';
  className?: string;
}

export function MetricCard({
  title,
  value,
  subtitle,
  trend,
  trendValue,
  icon,
  color = 'primary',
  className,
}: MetricCardProps) {
  const borderColors = {
    primary: 'border-t-4 border-t-blue-600',
    success: 'border-t-4 border-t-green-600',
    warning: 'border-t-4 border-t-amber-500',
    danger: 'border-t-4 border-t-red-600',
    info: 'border-t-4 border-t-sky-500',
  };

  const TrendIcon = trend === 'up' ? TrendingUp : trend === 'down' ? TrendingDown : Minus;
  const trendColorClass =
    trend === 'up'
      ? 'text-green-600'
      : trend === 'down'
      ? 'text-red-600'
      : 'text-gray-500';

  return (
    <Card className={cn(borderColors[color], 'shadow-sm hover:shadow-md transition-shadow', className)}>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-gray-600">{title}</CardTitle>
        {icon && <div className="text-gray-400">{icon}</div>}
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-gray-900">{value}</div>
        {(subtitle || trend) && (
          <div className="flex items-center gap-2 mt-1">
            {trend && trendValue && (
              <div className={cn('flex items-center gap-1', trendColorClass)}>
                <TrendIcon className="w-3 h-3" aria-hidden="true" />
                <span className="text-xs font-medium">{trendValue}</span>
              </div>
            )}
            {subtitle && <p className="text-xs text-gray-600">{subtitle}</p>}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
