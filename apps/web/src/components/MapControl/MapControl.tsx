import { useMatches } from '@/hooks/useMatches';
import { buildMarkers, buildPopupHtml } from '@/utils/markers';
import { useCallback, useEffect, useRef, useState } from 'react';
import LeafletMap from '../LeafletMap/LeafletMap';
import './MapControl.css';

interface Bounds {
  minLat: number;
  maxLat: number;
  minLng: number;
  maxLng: number;
}

export default function MapControl() {
  const { locations, loading, loadLocations, loadVenueMatches } = useMatches();
  const [markers, setMarkers] = useState<ReturnType<typeof buildMarkers>>([]);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const DEBOUNCE_MS = 300;

  const expandAndLoadBounds = useCallback(
    (b: Bounds) => {
      // Expand bounds by 50%
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

      loadLocations(request).catch((e) => console.error('Error loading matches:', e));
    },
    [loadLocations],
  );

  const onInitialBounds = useCallback(
    (b: Bounds) => {
      expandAndLoadBounds(b);
    },
    [expandAndLoadBounds],
  );

  const onBoundsChanged = useCallback(
    (b: Bounds) => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }

      debounceTimerRef.current = setTimeout(() => {
        expandAndLoadBounds(b);
      }, DEBOUNCE_MS);
    },
    [expandAndLoadBounds],
  );

  const onMarkerClick = useCallback(
    async (markerId: number) => {
      const matches = await loadVenueMatches(markerId);
      const location = locations.find((loc) => loc.venue?.id === markerId);

      // Update the marker's popup with the loaded matches
      setMarkers((prev) =>
        prev.map((marker) =>
          marker.id === markerId
            ? { ...marker, popupHtml: buildPopupHtml(matches, location?.venue) }
            : marker,
        ),
      );
    },
    [loadVenueMatches, locations],
  );

  // Update markers when locations change
  useEffect(() => {
    const newMarkers = buildMarkers(locations);
    setMarkers(newMarkers);
  }, [locations]);

  return (
    <div className="map-container">
      <LeafletMap
        className="map"
        markers={markers}
        onBoundsChanged={onBoundsChanged}
        onReady={onInitialBounds}
        onMarkerClick={onMarkerClick}
      />
      {(loading || locations.length > 0) && (
        <div className="map-info">
          {loading ? (
            <div className="loading">Lade Spiele...</div>
          ) : (
            <div className="match-count">
              {locations.length} {locations.length === 1 ? 'Spielort' : 'Spielorte'} gefunden
            </div>
          )}
        </div>
      )}
    </div>
  );
}
