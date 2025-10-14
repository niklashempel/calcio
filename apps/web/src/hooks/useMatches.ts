import { ApiService } from '@/services/api';
import { type GetMatchesRequest, type GroupedMatches } from '@/types/api';
import { type FilterParams, readFiltersFromUrl, writeFiltersToUrl, mergeFiltersWithRequest } from '@/utils/urlParams';
import { useCallback, useEffect, useRef, useState } from 'react';

export function useMatches() {
  const [matches, setMatches] = useState<GroupedMatches[]>([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<FilterParams>(() => readFiltersFromUrl());
  const lastBoundsRef = useRef<GetMatchesRequest | null>(null);

  // Sync filters to URL when they change
  useEffect(() => {
    writeFiltersToUrl(filters);
  }, [filters]);

  const load = useCallback(
    async (request: Omit<GetMatchesRequest, 'minDate' | 'maxDate' | 'competitions' | 'ageGroups'>) => {
      const fullRequest = mergeFiltersWithRequest(request, filters);
      
      if (
        lastBoundsRef.current &&
        JSON.stringify(lastBoundsRef.current) === JSON.stringify(fullRequest) &&
        matches.length > 0
      ) {
        return;
      }

      lastBoundsRef.current = fullRequest;
      setLoading(true);

      try {
        const result = await ApiService.getMatches(fullRequest);
        setMatches(result);
      } finally {
        setLoading(false);
      }
    },
    [matches.length, filters],
  );

  const updateFilters = useCallback((newFilters: FilterParams) => {
    setFilters(newFilters);
    // Reset cache when filters change
    lastBoundsRef.current = null;
  }, []);

  return { matches, loading, load, filters, updateFilters };
}
