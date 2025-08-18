<template>
  <div class="map-container">
    <LeafletMap
      class="map"
      :markers="markers"
      @bounds-changed="onBoundsChanged"
      @ready="onInitialBounds"
    />
    <div class="map-info" v-if="loading || matches.length > 0">
      <div v-if="loading" class="loading">Lade Spiele...</div>
      <div v-else class="match-count">
        {{ matches.length }} {{ matches.length === 1 ? 'Spiel' : 'Spiele' }} gefunden
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useMatches } from '@/composables/useMatches';
import { buildMarkers } from '@/utils/markers';
import { ref } from 'vue';
import LeafletMap from './LeafletMap.vue';

const { matches, loading, load } = useMatches();
const markers = ref<ReturnType<typeof buildMarkers>>([]);
const currentBounds = ref<{ minLat: number; maxLat: number; minLng: number; maxLng: number } | null>(null);
let debounceTimer: ReturnType<typeof setTimeout> | null = null;
const DEBOUNCE_MS = 300;

function onInitialBounds(b: { minLat: number; maxLat: number; minLng: number; maxLng: number }) {
  currentBounds.value = b;
  loadMatches();
}

function onBoundsChanged(b: { minLat: number; maxLat: number; minLng: number; maxLng: number }) {
  currentBounds.value = b;
  if (debounceTimer) clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    loadMatches();
  }, DEBOUNCE_MS);
}

async function loadMatches() {
  if (!currentBounds.value) return;
  try {
    // Expand bounds by 50%
    const b = currentBounds.value;
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

  markers.value = buildMarkers(matches.value);
  } catch (e) {
    console.error('Error loading matches:', e);
  }
}
</script>

<style scoped>
.map-container {
  position: relative;
  height: 100%;
  width: 100%;
}

.map {
  height: 100%;
  width: 100%;
}

.map-info {
  position: absolute;
  top: 10px;
  right: 10px;
  background: white;
  padding: 8px 12px;
  border-radius: 4px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  z-index: 1000;
  font-size: 14px;
}

.loading {
  color: #666;
}

.match-count {
  color: #333;
  font-weight: 500;
}
</style>

<style>
/* Global styles for map popups */
.match-popup h3 {
  margin: 0 0 8px 0;
  font-size: 16px;
  color: #333;
}

.match-popup p {
  margin: 4px 0;
  font-size: 14px;
  color: #666;
}

.match-popup strong {
  color: #333;
}

.match-popup h4.group {
  margin: 10px 0 4px;
  font-size: 13px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: #555;
  border-bottom: 1px solid #ddd;
  padding-bottom: 2px;
}

.match-popup ul.matches {
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: 220px;
  overflow-y: auto;
  scrollbar-width: thin;
}

.match-popup ul.matches li {
  margin: 4px 0;
  padding: 4px 6px;
  border-radius: 4px;
  background: #f7f7f7;
}

.match-popup ul.matches li:nth-child(odd) {
  background: #eee;
}

.match-popup ul.matches .time {
  font-weight: 500;
  color: #222;
}

.match-popup ul.matches .comp {
  color: #2a5db0;
}

.match-popup ul.matches .age {
  color: #8a5bb3;
}

.match-popup ul.match-cards {
  padding-top: 4px;
}
.match-popup ul.match-cards li.match-card {
  background: #fff;
  border: 1px solid #e2e2e2;
  margin: 8px 0;
  padding: 6px 8px 8px;
  border-radius: 6px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  font-size: 13px;
}
.match-popup ul.match-cards li.match-card > a {
  color: inherit;
  text-decoration: none;
  display: block;
}
.match-popup ul.match-cards li.match-card:hover {
  border-color: #c9c9c9;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
  cursor: pointer;
}
.match-popup ul.match-cards li.match-card .match-header {
  font-weight: 600;
  font-size: 12px;
  color: #444;
  margin-bottom: 4px;
}
.match-popup ul.match-cards li.match-card .match-line {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  line-height: 1.2;
}
.match-popup ul.match-cards li.match-card .team {
  flex: 1 1 auto;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.match-popup ul.match-cards li.match-card .team.home {
  font-weight: 500;
}
.match-popup ul.match-cards li.match-card .date,
.match-popup ul.match-cards li.match-card .time {
  flex: 0 0 auto;
  min-width: 90px;
  text-align: right;
  color: #333;
  font-weight: 500;
}
</style>
