import { ApiService } from '@/services/api';
import { type GetMatchesRequest, type GroupedMatches } from '@/types/api';
import { useCallback, useRef, useState } from 'react';

export function useMatches() {
  const [matches, setMatches] = useState<GroupedMatches[]>([]);
  const [loading, setLoading] = useState(false);
  const lastBoundsRef = useRef<GetMatchesRequest | null>(null);

  const load = useCallback(
    async (request: GetMatchesRequest) => {
      if (
        lastBoundsRef.current &&
        JSON.stringify(lastBoundsRef.current) === JSON.stringify(request) &&
        matches.length > 0
      ) {
        return;
      }

      lastBoundsRef.current = request;
      setLoading(true);

      try {
        const result = await ApiService.getMatches(request);
        setMatches(result);
      } finally {
        setLoading(false);
      }
    },
    [matches.length],
  );

  return { matches, loading, load };
}
