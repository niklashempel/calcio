import { useMatches } from '@/hooks/useMatches';
import { buildMarkers } from '@/utils/markers';
import { useCallback, useEffect, useRef, useState } from 'react';
import LeafletMap from '../LeafletMap/LeafletMap';
import './MapControl.css';

interface Bounds {
  minLat: number;
  maxLat: number;
  minLng: number;
  maxLng: number;
}

function MapControl() {
  const { matches, loading, load } = useMatches();
  const [markers, setMarkers] = useState<ReturnType<typeof buildMarkers>>([]);
  const [currentBounds, setCurrentBounds] = useState<Bounds | null>(null);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const DEBOUNCE_MS = 300;

  const loadMatches = useCallback(async () => {
    if (!currentBounds) return;

    try {
      // Expand bounds by 50%
      const b = currentBounds;
      const latCenter = (b.minLat + b.maxLat) / 2;
      const lngCenter = (b.minLng + b.maxLng) / 2;
      const latRange = b.maxLat - b.minLat;
      const lngRange = b.maxLng - b.minLng;
      const expandFactor = 1.5;
      const expandedLatRange = latRange * expandFactor;
      const expandedLngRange = lngRange * expandFactor;

      const request = {
        minLat: latCenter - expandedLatRange / 2,
        maxLat: latCenter + expandedLatRange / 2,
        minLng: lngCenter - expandedLngRange / 2,
        maxLng: lngCenter + expandedLngRange / 2,
      };

      await load(request);
    } catch (e) {
      console.error('Error loading matches:', e);
    }
  }, [currentBounds, load]);

  const onInitialBounds = useCallback((b: Bounds) => {
    setCurrentBounds(b);
  }, []);

  const onBoundsChanged = useCallback(
    (b: Bounds) => {
      setCurrentBounds(b);

      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }

      debounceTimerRef.current = setTimeout(() => {
        loadMatches();
      }, DEBOUNCE_MS);
    },
    [loadMatches],
  );

  // Update markers when matches change
  useEffect(() => {
    setMarkers(buildMarkers(matches));
  }, [matches]);

  // Load matches when bounds change
  useEffect(() => {
    if (currentBounds) {
      loadMatches();
    }
  }, [currentBounds, loadMatches]);

  return (
    <div className="map-container">
      <LeafletMap
        className="map"
        markers={markers}
        onBoundsChanged={onBoundsChanged}
        onReady={onInitialBounds}
      />
      {(loading || matches.length > 0) && (
        <div className="map-info">
          {loading ? (
            <div className="loading">Lade Spiele...</div>
          ) : (
            <div className="match-count">
              {matches.length} {matches.length === 1 ? 'Spiel' : 'Spiele'} gefunden
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default MapControl;
