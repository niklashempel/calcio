<template>
  <div ref="mapEl" class="leaflet-map"></div>
</template>

<script setup lang="ts">
import type { MatchDto } from '@/types/api';
import L from 'leaflet';
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
  (e: 'bounds-changed', bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number }): void;
  (e: 'ready', bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number }): void;
}>();

const mapEl = ref<HTMLElement | null>(null);
let map: L.Map | null = null;
let markersLayer: L.LayerGroup | null = null;
const popupApps = new Map<number, { app: App; el: HTMLElement }>();
let skipNextMoveEnd = false;

// Fix Leaflet default icon paths (CDN)
// Suppress private property typing
delete (L.Icon.Default.prototype as unknown as { _getIconUrl?: unknown })._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png'
});

function emitBounds() {
  if (!map) return;
  const b = map.getBounds();
  emit('bounds-changed', {
    minLat: b.getSouth(),
    maxLat: b.getNorth(),
    minLng: b.getWest(),
    maxLng: b.getEast()
  });
}

onMounted(() => {
  if (!mapEl.value) return;
  map = L.map(mapEl.value).setView([51.1657, 10.4515], 6);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> Contributors'
  }).addTo(map);
  markersLayer = L.layerGroup().addTo(map);

  map.on('moveend', () => {
    if (skipNextMoveEnd) {
      skipNextMoveEnd = false;
      return;
    }
    emitBounds();
  });
  map.on('popupopen', () => { skipNextMoveEnd = true; });
  map.on('popupclose', () => { skipNextMoveEnd = false; });

  // Initial bounds emit
  emitBounds();
  const b = map.getBounds();
  emit('ready', {
    minLat: b.getSouth(),
    maxLat: b.getNorth(),
    minLng: b.getWest(),
    maxLng: b.getEast()
  });
});

onUnmounted(() => {
  map?.remove();
  map = null;
  markersLayer = null;
});

watch(
  () => props.markers,
  (list) => {
    if (!map || !markersLayer) return;
    // Unmount previous mounted popup apps
    for (const entry of popupApps.values()) entry.app.unmount();
    popupApps.clear();
    markersLayer.clearLayers();
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
      markersLayer.addLayer(marker);
    }
  },
  { deep: true }
);
</script>

<style scoped>
.leaflet-map { height: 100%; width: 100%; }
</style>
