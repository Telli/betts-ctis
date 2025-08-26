'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { CheckCircle, AlertTriangle, XCircle, Target, TrendingUp } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ComplianceLevel } from '@/lib/types/kpi';

interface ComplianceScoreCardProps {
  score: number;
  level: ComplianceLevel;
  trend?: number; // percentage change from previous period
  description?: string;
  showDetails?: boolean;
  className?: string;
}

export default function ComplianceScoreCard({
  score,
  level,
  trend,
  description,
  showDetails = true,
  className
}: ComplianceScoreCardProps) {
  const getScoreColor = () => {
    switch (level) {
      case ComplianceLevel.Green:
        return 'text-sierra-green-600';
      case ComplianceLevel.Yellow:
        return 'text-sierra-gold-600';
      case ComplianceLevel.Red:
        return 'text-red-600';
      default:
        return 'text-gray-600';
    }
  };

  const getProgressColor = () => {
    switch (level) {
      case ComplianceLevel.Green:
        return 'bg-sierra-green-500';
      case ComplianceLevel.Yellow:
        return 'bg-sierra-gold-500';
      case ComplianceLevel.Red:
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  };

  const getStatusIcon = () => {
    switch (level) {
      case ComplianceLevel.Green:
        return <CheckCircle className="h-5 w-5 text-sierra-green-600" />;
      case ComplianceLevel.Yellow:
        return <AlertTriangle className="h-5 w-5 text-sierra-gold-600" />;
      case ComplianceLevel.Red:
        return <XCircle className="h-5 w-5 text-red-600" />;
      default:
        return <Target className="h-5 w-5 text-gray-600" />;
    }
  };

  const getBadgeVariant = () => {
    switch (level) {
      case ComplianceLevel.Green:
        return 'default';
      case ComplianceLevel.Yellow:
        return 'secondary';
      case ComplianceLevel.Red:
        return 'destructive';
      default:
        return 'outline';
    }
  };

  const getLevelLabel = () => {
    switch (level) {
      case ComplianceLevel.Green:
        return 'Excellent';
      case ComplianceLevel.Yellow:
        return 'Good';
      case ComplianceLevel.Red:
        return 'Needs Attention';
      default:
        return 'Unknown';
    }
  };

  const getTrendIcon = () => {
    if (!trend) return null;
    
    return trend > 0 ? (
      <div className="flex items-center text-sierra-green-600">
        <TrendingUp className="h-3 w-3 mr-1" />
        <span className="text-xs">+{trend.toFixed(1)}%</span>
      </div>
    ) : (
      <div className="flex items-center text-red-600">
        <TrendingUp className="h-3 w-3 mr-1 rotate-180" />
        <span className="text-xs">{trend.toFixed(1)}%</span>
      </div>
    );
  };

  return (
    <Card className={cn("relative overflow-hidden", className)}>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg font-semibold">
            Compliance Score
          </CardTitle>
          {getStatusIcon()}
        </div>
        {description && (
          <CardDescription className="text-sm">
            {description}
          </CardDescription>
        )}
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Main Score Display */}
        <div className="text-center space-y-2">
          <div className={cn("text-4xl font-bold", getScoreColor())}>
            {score.toFixed(1)}
          </div>
          <div className="flex items-center justify-center gap-2">
            <Badge variant={getBadgeVariant()}>
              {getLevelLabel()}
            </Badge>
            {getTrendIcon()}
          </div>
        </div>

        {/* Progress Bar */}
        <div className="space-y-2">
          <div className="flex justify-between text-sm text-muted-foreground">
            <span>Compliance Progress</span>
            <span>{score.toFixed(0)}%</span>
          </div>
          <Progress 
            value={score} 
            className="h-2"
            // Apply custom color through CSS custom properties
            style={{
              '--progress-background': level === ComplianceLevel.Green ? '#10b981' : 
                                     level === ComplianceLevel.Yellow ? '#f59e0b' : '#ef4444'
            } as any}
          />
        </div>

        {/* Score Ranges */}
        {showDetails && (
          <div className="space-y-2 text-xs">
            <div className="flex items-center justify-between py-1">
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full bg-sierra-green-500"></div>
                <span>Excellent (80-100)</span>
              </div>
              <span className={score >= 80 ? 'font-medium text-sierra-green-700' : 'text-muted-foreground'}>
                {score >= 80 ? '✓' : ''}
              </span>
            </div>
            <div className="flex items-center justify-between py-1">
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full bg-sierra-gold-500"></div>
                <span>Good (60-79)</span>
              </div>
              <span className={score >= 60 && score < 80 ? 'font-medium text-sierra-gold-700' : 'text-muted-foreground'}>
                {score >= 60 && score < 80 ? '✓' : ''}
              </span>
            </div>
            <div className="flex items-center justify-between py-1">
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full bg-red-500"></div>
                <span>Needs Attention (0-59)</span>
              </div>
              <span className={score < 60 ? 'font-medium text-red-700' : 'text-muted-foreground'}>
                {score < 60 ? '✓' : ''}
              </span>
            </div>
          </div>
        )}

        {/* Improvement Suggestions */}
        {level !== ComplianceLevel.Green && showDetails && (
          <div className="mt-4 p-3 bg-muted rounded-lg">
            <div className="text-sm font-medium mb-1">
              {level === ComplianceLevel.Yellow ? 'Suggestions for Excellence:' : 'Immediate Actions Required:'}
            </div>
            <div className="text-xs text-muted-foreground">
              {level === ComplianceLevel.Yellow 
                ? 'Focus on timely filing and complete documentation to reach excellent compliance.'
                : 'Review overdue filings, missing payments, and incomplete documents immediately.'}
            </div>
          </div>
        )}
      </CardContent>
      
      {/* Status Indicator Bar */}
      <div className={cn(
        "absolute bottom-0 left-0 right-0 h-1",
        level === ComplianceLevel.Green ? 'bg-sierra-green-500' :
        level === ComplianceLevel.Yellow ? 'bg-sierra-gold-500' :
        'bg-red-500'
      )} />
    </Card>
  );
}