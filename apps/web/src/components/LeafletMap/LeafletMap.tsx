import L from 'leaflet';
import 'leaflet.markercluster';
import 'leaflet.markercluster/dist/MarkerCluster.css';
import 'leaflet.markercluster/dist/MarkerCluster.Default.css';
import 'leaflet/dist/leaflet.css';
import { useCallback, useEffect, useRef } from 'react';
import './LeafletMap.css';

interface MarkerInput {
  id: number;
  lat: number;
  lng: number;
  popupHtml: string;
}

interface LeafletMapProps {
  markers: MarkerInput[];
  className?: string;
  onBoundsChanged: (bounds: {
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  }) => void;
  onReady: (bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number }) => void;
  onMarkerClick?: (markerId: number) => void;
}

function LeafletMap({
  markers,
  className,
  onBoundsChanged,
  onReady,
  onMarkerClick,
}: LeafletMapProps) {
  const mapRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markerClusterRef = useRef<L.MarkerClusterGroup | null>(null);
  const markerInstancesRef = useRef<Map<number, L.Marker>>(new Map());
  const skipNextMoveEndRef = useRef(false);

  // Fix Leaflet default icon paths
  useEffect(() => {
    delete (L.Icon.Default.prototype as any)._getIconUrl;
    L.Icon.Default.mergeOptions({
      iconRetinaUrl:
        'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
      iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
      shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
    });
  }, []);

  const emitBounds = useCallback(() => {
    if (!mapInstanceRef.current) return;
    const b = mapInstanceRef.current.getBounds();
    onBoundsChanged({
      minLat: b.getSouth(),
      maxLat: b.getNorth(),
      minLng: b.getWest(),
      maxLng: b.getEast(),
    });
  }, [onBoundsChanged]);

  // Initialize map
  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;

    const map = L.map(mapRef.current).setView([51.1657, 10.4515], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution:
        '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> Contributors',
    }).addTo(map);

    // Create marker cluster group with custom options
    const markersCluster = L.markerClusterGroup({
      chunkedLoading: true,
      spiderfyOnMaxZoom: true,
      showCoverageOnHover: false,
      zoomToBoundsOnClick: true,
      maxClusterRadius: 50,
      iconCreateFunction: function (cluster) {
        const count = cluster.getChildCount();
        let className = 'marker-cluster-small';
        if (count >= 10) className = 'marker-cluster-medium';
        if (count >= 50) className = 'marker-cluster-large';

        return L.divIcon({
          html: `<div><span>${count}</span></div>`,
          className: `marker-cluster ${className}`,
          iconSize: L.point(40, 40),
        });
      },
    }).addTo(map);

    map.on('moveend', () => {
      if (skipNextMoveEndRef.current) {
        skipNextMoveEndRef.current = false;
        return;
      }
      emitBounds();
    });

    map.on('popupopen', () => {
      skipNextMoveEndRef.current = true;
    });

    map.on('popupclose', () => {
      skipNextMoveEndRef.current = false;
    });

    mapInstanceRef.current = map;
    markerClusterRef.current = markersCluster;

    // Initial bounds emit
    const bounds = map.getBounds();
    onReady({
      minLat: bounds.getSouth(),
      maxLat: bounds.getNorth(),
      minLng: bounds.getWest(),
      maxLng: bounds.getEast(),
    });

    return () => {
      map.remove();
      mapInstanceRef.current = null;
      markerClusterRef.current = null;
    };
  }, [onReady, emitBounds]);

  // Update markers when markers prop changes
  useEffect(() => {
    const markerCluster = markerClusterRef.current;
    if (!markerCluster) return;

    // Check if we can update existing markers instead of recreating
    const existingMarkers = markerInstancesRef.current;
    const newMarkerIds = new Set(markers.map((m) => m.id));
    const existingMarkerIds = new Set(existingMarkers.keys());

    // If the marker IDs are the same, just update popup content
    const sameMarkers =
      newMarkerIds.size === existingMarkerIds.size &&
      [...newMarkerIds].every((id) => existingMarkerIds.has(id));

    if (sameMarkers) {
      // Update popup content for existing markers
      markers.forEach(({ id, popupHtml }) => {
        const marker = existingMarkers.get(id);
        if (marker && popupHtml) {
          const popup = marker.getPopup();
          if (popup) {
            popup.setContent(popupHtml);
          } else {
            marker.bindPopup(popupHtml, { maxWidth: 380 });
          }
        }
      });
      return;
    }

    // Clear existing markers and create new ones
    markerCluster.clearLayers();
    markerInstancesRef.current.clear();

    // Add new markers
    markers.forEach(({ id, lat, lng, popupHtml }) => {
      const marker = L.marker([lat, lng]);
      if (popupHtml) {
        marker.bindPopup(popupHtml, { maxWidth: 380 });
      }

      if (onMarkerClick) {
        marker.on('popupopen', () => {
          // Only trigger if popup contains loading state
          const popup = marker.getPopup();
          if (popup && popup.getContent()?.toString().includes('loading')) {
            onMarkerClick(id);
          }
        });
      }

      markerCluster.addLayer(marker);
      markerInstancesRef.current.set(id, marker);
    });
  }, [markers, onMarkerClick]);

  return <div ref={mapRef} className={`leaflet-map ${className || ''}`} />;
}

export default LeafletMap;
