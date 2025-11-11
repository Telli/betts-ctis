import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  fetchDashboardMetrics,
  fetchFilingTrends,
  fetchComplianceDistribution,
  fetchUpcomingDeadlines,
  fetchRecentActivity,
  type DashboardMetrics,
} from "../services/dashboard";

/**
 * Hook to fetch dashboard metrics
 */
export function useDashboardMetrics(clientId?: number) {
  return useQuery({
    queryKey: ["dashboardMetrics", clientId],
    queryFn: () => fetchDashboardMetrics(clientId),
    staleTime: 2 * 60 * 1000, // Fresh for 2 minutes
  });
}

/**
 * Hook to fetch filing trends
 */
export function useFilingTrends(clientId?: number, months = 6) {
  return useQuery({
    queryKey: ["filingTrends", clientId, months],
    queryFn: () => fetchFilingTrends(clientId, months),
    staleTime: 5 * 60 * 1000, // Fresh for 5 minutes
  });
}

/**
 * Hook to fetch compliance distribution
 */
export function useComplianceDistribution(clientId?: number) {
  return useQuery({
    queryKey: ["complianceDistribution", clientId],
    queryFn: () => fetchComplianceDistribution(clientId),
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Hook to fetch upcoming deadlines
 */
export function useUpcomingDeadlines(clientId?: number, limit = 10) {
  return useQuery({
    queryKey: ["upcomingDeadlines", clientId, limit],
    queryFn: () => fetchUpcomingDeadlines(clientId, limit),
    staleTime: 1 * 60 * 1000, // Fresh for 1 minute (more frequent updates)
  });
}

/**
 * Hook to fetch recent activity
 */
export function useRecentActivity(clientId?: number, limit = 10) {
  return useQuery({
    queryKey: ["recentActivity", clientId, limit],
    queryFn: () => fetchRecentActivity(clientId, limit),
    staleTime: 30 * 1000, // Fresh for 30 seconds (real-time feel)
  });
}

/**
 * Hook to fetch all dashboard data at once
 */
export function useDashboardData(clientId?: number) {
  const metrics = useDashboardMetrics(clientId);
  const trends = useFilingTrends(clientId, 6);
  const compliance = useComplianceDistribution(clientId);
  const deadlines = useUpcomingDeadlines(clientId, 10);
  const activity = useRecentActivity(clientId, 10);

  return {
    metrics,
    trends,
    compliance,
    deadlines,
    activity,
    isLoading:
      metrics.isLoading ||
      trends.isLoading ||
      compliance.isLoading ||
      deadlines.isLoading ||
      activity.isLoading,
    isError:
      metrics.isError ||
      trends.isError ||
      compliance.isError ||
      deadlines.isError ||
      activity.isError,
    error: metrics.error || trends.error || compliance.error || deadlines.error || activity.error,
  };
}
