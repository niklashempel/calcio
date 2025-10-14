import { ApiService } from '@/services/api';
import { type GetMatchesRequest, type GroupedMatches, type MatchLocationDto } from '@/types/api';
import { useCallback, useRef, useState } from 'react';

const areBoundsSimilar = (a: GetMatchesRequest, b: GetMatchesRequest, tolerance = 0.01) => {
  if (
    a.minLat == null ||
    a.maxLat == null ||
    a.minLng == null ||
    a.maxLng == null ||
    b.minLat == null ||
    b.maxLat == null ||
    b.minLng == null ||
    b.maxLng == null
  ) {
    return false;
  }

  return (
    Math.abs(a.minLat - b.minLat) < tolerance &&
    Math.abs(a.maxLat - b.maxLat) < tolerance &&
    Math.abs(a.minLng - b.minLng) < tolerance &&
    Math.abs(a.maxLng - b.maxLng) < tolerance
  );
};

export function useMatches() {
  const [locations, setLocations] = useState<MatchLocationDto[]>([]);
  const [venueMatches, setVenueMatches] = useState<Map<number, GroupedMatches>>(new Map());
  const [loading, setLoading] = useState(false);
  const [loadingVenue, setLoadingVenue] = useState<number | null>(null);
  const lastBoundsRef = useRef<GetMatchesRequest | null>(null);
  const venueMatchesCacheRef = useRef<Map<number, GroupedMatches>>(new Map());

  const loadLocations = useCallback(async (request: GetMatchesRequest) => {
    if (lastBoundsRef.current && areBoundsSimilar(lastBoundsRef.current, request)) {
      return;
    }

    lastBoundsRef.current = request;
    setLoading(true);

    try {
      const result = await ApiService.getMatchLocations(request);
      setLocations(result);
    } catch (error) {
      console.error('Error loading locations:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  const loadVenueMatches = useCallback(async (venueId: number) => {
    // Check cache first
    if (venueMatchesCacheRef.current.has(venueId)) {
      return venueMatchesCacheRef.current.get(venueId)!;
    }

    setLoadingVenue(venueId);
    try {
      const result = await ApiService.getMatchesByVenue(venueId);
      venueMatchesCacheRef.current.set(venueId, result);
      setVenueMatches(new Map(venueMatchesCacheRef.current));
      return result;
    } catch (error) {
      console.error('Error loading venue matches:', error);
      throw error;
    } finally {
      setLoadingVenue(null);
    }
  }, []);

  return { locations, venueMatches, loading, loadingVenue, loadLocations, loadVenueMatches };
}
