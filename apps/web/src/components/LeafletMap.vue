<template>
  <div ref="mapEl" class="leaflet-map"></div>
</template>

<script setup lang="ts">
import type { MatchDto } from '@/types/api';
import L from 'leaflet';
import 'leaflet.markercluster';
import 'leaflet.markercluster/dist/MarkerCluster.css';
import 'leaflet.markercluster/dist/MarkerCluster.Default.css';
import 'leaflet/dist/leaflet.css';
import { createApp, onMounted, onUnmounted, ref, watch, type App } from 'vue';
import MatchPopup from './MatchPopup.vue';

interface MarkerInput {
  id: number;
  lat: number;
  lng: number;
  popupHtml?: string;
  venueAddress?: string;
  matches?: MatchDto[];
}

const props = defineProps<{ markers: MarkerInput[] }>();
const emit = defineEmits<{
  (
    e: 'bounds-changed',
    bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number },
  ): void;
  (e: 'ready', bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number }): void;
}>();

const mapEl = ref<HTMLElement | null>(null);
let map: L.Map | null = null;
let markersCluster: L.MarkerClusterGroup | null = null;
const popupApps = new Map<number, { app: App; el: HTMLElement }>();
let skipNextMoveEnd = false;

// Fix Leaflet default icon paths (CDN)
// Suppress private property typing
delete (L.Icon.Default.prototype as unknown as { _getIconUrl?: unknown })._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

function emitBounds() {
  if (!map) return;
  const b = map.getBounds();
  emit('bounds-changed', {
    minLat: b.getSouth(),
    maxLat: b.getNorth(),
    minLng: b.getWest(),
    maxLng: b.getEast(),
  });
}

onMounted(() => {
  if (!mapEl.value) return;
  map = L.map(mapEl.value).setView([51.1657, 10.4515], 6);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution:
      '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> Contributors',
  }).addTo(map);

  // Create marker cluster group with custom options
  markersCluster = L.markerClusterGroup({
    chunkedLoading: true,
    spiderfyOnMaxZoom: true,
    showCoverageOnHover: false,
    zoomToBoundsOnClick: true,
    maxClusterRadius: 50,
    iconCreateFunction: function(cluster) {
      const count = cluster.getChildCount();
      let className = 'marker-cluster-small';
      if (count >= 10) className = 'marker-cluster-medium';
      if (count >= 50) className = 'marker-cluster-large';

      return L.divIcon({
        html: `<div><span>${count}</span></div>`,
        className: `marker-cluster ${className}`,
        iconSize: L.point(40, 40)
      });
    }
  }).addTo(map);

  map.on('moveend', () => {
    if (skipNextMoveEnd) {
      skipNextMoveEnd = false;
      return;
    }
    emitBounds();
  });
  map.on('popupopen', () => {
    skipNextMoveEnd = true;
  });
  map.on('popupclose', () => {
    skipNextMoveEnd = false;
  });

  // Initial bounds emit
  emitBounds();
  const b = map.getBounds();
  emit('ready', {
    minLat: b.getSouth(),
    maxLat: b.getNorth(),
    minLng: b.getWest(),
    maxLng: b.getEast(),
  });
});

onUnmounted(() => {
  map?.remove();
  map = null;
  markersCluster = null;
});

watch(
  () => props.markers,
  (list) => {
    if (!map || !markersCluster) return;
    // Unmount previous mounted popup apps
    for (const entry of popupApps.values()) entry.app.unmount();
    popupApps.clear();
    markersCluster.clearLayers();
    for (const m of list) {
      const marker = L.marker([m.lat, m.lng]);
      if (m.popupHtml) {
        marker.bindPopup(m.popupHtml, { maxWidth: 380 });
      } else if (m.matches) {
        const container = document.createElement('div');
        marker.bindPopup(container, { maxWidth: 380 });
        marker.on('popupopen', () => {
          if (!popupApps.has(m.id)) {
            const app = createApp(MatchPopup, { matches: m.matches, venueAddress: m.venueAddress });
            app.mount(container);
            popupApps.set(m.id, { app, el: container });
          }
        });
        marker.on('popupclose', () => {
          const rec = popupApps.get(m.id);
          if (rec) {
            rec.app.unmount();
            popupApps.delete(m.id);
          }
        });
      }
      markersCluster.addLayer(marker);
    }
  },
  { deep: true },
);
</script>

<style scoped>
.leaflet-map {
  height: 100%;
  width: 100%;
}
</style>

<style>
/* Global styles for marker clusters */
.marker-cluster-small {
  background-color: rgba(181, 226, 140, 0.8);
}
.marker-cluster-small div {
  background-color: rgba(110, 204, 57, 0.8);
}

.marker-cluster-medium {
  background-color: rgba(241, 211, 87, 0.8);
}
.marker-cluster-medium div {
  background-color: rgba(240, 194, 12, 0.8);
}

.marker-cluster-large {
  background-color: rgba(253, 156, 115, 0.8);
}
.marker-cluster-large div {
  background-color: rgba(241, 128, 23, 0.8);
}

.marker-cluster {
  border-radius: 20px;
}
.marker-cluster div {
  width: 30px;
  height: 30px;
  margin-left: 5px;
  margin-top: 5px;
  text-align: center;
  border-radius: 15px;
  font: 12px "Helvetica Neue", Arial, Helvetica, sans-serif;
}
.marker-cluster span {
  line-height: 30px;
  color: #000;
  font-weight: bold;
}
</style>
