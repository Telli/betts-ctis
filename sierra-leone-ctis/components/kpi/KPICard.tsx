'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { TrendingUp, TrendingDown, Minus, Target, Clock, AlertTriangle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { TrendDataPoint } from '@/lib/types/kpi';

interface KPICardProps {
  title: string;
  value: number;
  format: 'percentage' | 'number' | 'currency' | 'days';
  trend?: TrendDataPoint[];
  target?: number;
  threshold?: number;
  isReversed?: boolean; // For metrics where lower is better (like days overdue)
  description?: string;
  className?: string;
}

export default function KPICard({
  title,
  value,
  format,
  trend,
  target,
  threshold,
  isReversed = false,
  description,
  className
}: KPICardProps) {
  const formatValue = (val: number) => {
    switch (format) {
      case 'percentage':
        return `${val.toFixed(1)}%`;
      case 'currency':
        return `SLE ${val.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
      case 'days':
        return `${val.toFixed(1)} days`;
      default:
        return val.toLocaleString('en-US');
    }
  };

  const getStatusColor = () => {
    if (target) {
      const achievementRate = isReversed ? target / value : value / target;
      if (achievementRate >= 1) return 'text-sierra-green-600';
      if (achievementRate >= 0.9) return 'text-sierra-gold-500';
      return 'text-red-600';
    }
    
    if (threshold) {
      const withinThreshold = isReversed ? value <= threshold : value >= threshold;
      return withinThreshold ? 'text-sierra-green-600' : 'text-red-600';
    }
    
    return 'text-sierra-blue-600';
  };

  const getStatusIcon = () => {
    if (target) {
      const achievementRate = isReversed ? target / value : value / target;
      if (achievementRate >= 1) return <Target className="h-4 w-4" />;
      if (achievementRate >= 0.9) return <TrendingUp className="h-4 w-4" />;
      return <AlertTriangle className="h-4 w-4" />;
    }
    
    if (threshold) {
      const withinThreshold = isReversed ? value <= threshold : value >= threshold;
      return withinThreshold ? <Target className="h-4 w-4" /> : <AlertTriangle className="h-4 w-4" />;
    }
    
    return <Minus className="h-4 w-4" />;
  };

  const getTrendDirection = () => {
    if (!trend || trend.length < 2) return null;
    
    const latest = trend[trend.length - 1].value;
    const previous = trend[trend.length - 2].value;
    
    if (latest > previous) {
      return isReversed ? 'down' : 'up';
    } else if (latest < previous) {
      return isReversed ? 'up' : 'down';
    }
    return 'stable';
  };

  const getTrendIcon = () => {
    const direction = getTrendDirection();
    switch (direction) {
      case 'up':
        return <TrendingUp className="h-3 w-3 text-sierra-green-600" />;
      case 'down':
        return <TrendingDown className="h-3 w-3 text-red-600" />;
      default:
        return <Minus className="h-3 w-3 text-gray-400" />;
    }
  };

  const getTrendPercentage = () => {
    if (!trend || trend.length < 2) return null;
    
    const latest = trend[trend.length - 1].value;
    const previous = trend[trend.length - 2].value;
    
    if (previous === 0) return null;
    
    const change = ((latest - previous) / previous) * 100;
    return Math.abs(change).toFixed(1);
  };

  const getTargetBadge = () => {
    if (!target) return null;
    
    const achievementRate = isReversed ? (target / value) * 100 : (value / target) * 100;
    const label = achievementRate >= 100 ? 'Target Met' : `${achievementRate.toFixed(0)}% to target`;
    const variant = achievementRate >= 100 ? 'default' : achievementRate >= 90 ? 'secondary' : 'destructive';
    
    return (
      <Badge variant={variant} className="text-xs">
        {label}
      </Badge>
    );
  };

  return (
    <Card className={cn("relative overflow-hidden", className)}>
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-medium text-gray-600">
            {title}
          </CardTitle>
          <div className={cn("flex items-center", getStatusColor())}>
            {getStatusIcon()}
          </div>
        </div>
        {description && (
          <CardDescription className="text-xs">
            {description}
          </CardDescription>
        )}
      </CardHeader>
      
      <CardContent className="pt-0">
        <div className="space-y-2">
          {/* Main Value */}
          <div className={cn("text-2xl font-bold", getStatusColor())}>
            {formatValue(value)}
          </div>
          
          {/* Trend and Target Information */}
          <div className="flex items-center justify-between text-xs">
            {/* Trend */}
            {trend && trend.length >= 2 && (
              <div className="flex items-center space-x-1">
                {getTrendIcon()}
                <span className={cn(
                  getTrendDirection() === 'up' ? 'text-sierra-green-600' : 
                  getTrendDirection() === 'down' ? 'text-red-600' : 'text-gray-400'
                )}>
                  {getTrendPercentage()}%
                </span>
                <span className="text-gray-500">vs last period</span>
              </div>
            )}
            
            {/* Target Badge */}
            {getTargetBadge()}
          </div>
          
          {/* Threshold Information */}
          {threshold && (
            <div className="text-xs text-gray-500">
              {isReversed ? 'Target' : 'Threshold'}: {formatValue(threshold)}
            </div>
          )}
          
          {/* Simple Trend Visualization */}
          {trend && trend.length > 1 && (
            <div className="mt-3">
              <div className="flex items-end space-x-1 h-8">
                {trend.slice(-8).map((point, index) => {
                  const maxValue = Math.max(...trend.map(t => t.value));
                  const height = (point.value / maxValue) * 100;
                  return (
                    <div
                      key={index}
                      className={cn(
                        "flex-1 bg-gradient-to-t rounded-sm",
                        getStatusColor().includes('sierra-green') 
                          ? 'from-sierra-green-200 to-sierra-green-400'
                          : getStatusColor().includes('sierra-gold')
                          ? 'from-sierra-gold-200 to-sierra-gold-400'
                          : getStatusColor().includes('red')
                          ? 'from-red-200 to-red-400'
                          : 'from-sierra-blue-200 to-sierra-blue-400'
                      )}
                      style={{ height: `${Math.max(height * 0.3, 10)}%` }}
                      title={`${point.label}: ${formatValue(point.value)}`}
                    />
                  );
                })}
              </div>
            </div>
          )}
        </div>
      </CardContent>
      
      {/* Status Indicator Bar */}
      <div className={cn(
        "absolute bottom-0 left-0 right-0 h-1",
        getStatusColor().includes('sierra-green') ? 'bg-sierra-green-500' :
        getStatusColor().includes('sierra-gold') ? 'bg-sierra-gold-500' :
        getStatusColor().includes('red') ? 'bg-red-500' :
        'bg-sierra-blue-500'
      )} />
    </Card>
  );
}