"use client"

import { useState, useEffect, useCallback, useRef } from 'react';
import { DashboardService, NavigationCounts } from '@/lib/services/dashboard-service';
import { useAuth } from '@/context/auth-context';

// Simple in-memory cache with TTL
const CACHE_KEY = 'navigation_counts';
const CACHE_TTL = 2 * 60 * 1000; // 2 minutes
let cache: { data: NavigationCounts; timestamp: number } | null = null;

export function useNavigationCounts() {
  const [counts, setCounts] = useState<NavigationCounts | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { isLoggedIn } = useAuth();
  const retryTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const fetchCounts = useCallback(async (forceRefresh = false) => {
    if (!isLoggedIn) {
      setCounts(null);
      setLoading(false);
      return;
    }

    // Check cache first (unless force refresh)
    if (!forceRefresh && cache) {
      const now = Date.now();
      if (now - cache.timestamp < CACHE_TTL) {
        setCounts(cache.data);
        setLoading(false);
        return;
      }
    }

    try {
      setLoading(true);
      setError(null);
      
      const data = await DashboardService.getNavigationCounts();
      
      // Update cache
      cache = {
        data,
        timestamp: Date.now()
      };
      
      setCounts(data);
    } catch (err) {
      console.error('Failed to fetch navigation counts:', err);
      setError('Failed to load navigation counts');
      
      // Retry after 30 seconds on error
      if (retryTimeoutRef.current) {
        clearTimeout(retryTimeoutRef.current);
      }
      retryTimeoutRef.current = setTimeout(() => {
        fetchCounts(true);
      }, 30000);
      
      // Keep previous counts on error if available
      if (!counts && cache) {
        setCounts(cache.data);
      }
    } finally {
      setLoading(false);
    }
  }, [isLoggedIn, counts]);

  useEffect(() => {
    fetchCounts();
  }, [fetchCounts]);

  // Refresh counts every 5 minutes when user is active
  useEffect(() => {
    if (!isLoggedIn) return;

    const interval = setInterval(() => fetchCounts(true), 5 * 60 * 1000);
    return () => clearInterval(interval);
  }, [isLoggedIn, fetchCounts]);

  // Cleanup retry timeout on unmount
  useEffect(() => {
    return () => {
      if (retryTimeoutRef.current) {
        clearTimeout(retryTimeoutRef.current);
      }
    };
  }, []);

  return {
    counts,
    loading,
    error,
    refetch: () => fetchCounts(true)
  };
}