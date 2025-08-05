<template>
  <div class="map-container">
    <div ref="mapContainer" class="map"></div>
    <div class="map-info" v-if="loading || matches.length > 0">
      <div v-if="loading" class="loading">Loading matches...</div>
      <div v-else class="match-count">{{ matches.length }} matches found</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ApiService } from '@/services/api';
import type { MatchDto } from '@/types/api';
import L from 'leaflet';
import { onMounted, onUnmounted, ref } from 'vue';

// Fix for default markers in Leaflet with Vite
import 'leaflet/dist/leaflet.css';

// Fix for marker icons
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

const mapContainer = ref<HTMLElement>();
const loading = ref(false);
const matches = ref<MatchDto[]>([]);

let map: L.Map | null = null;
let markersLayer: L.LayerGroup | null = null;

const initMap = () => {
  if (!mapContainer.value) return;

  // Initialize map centered on Germany
  map = L.map(mapContainer.value).setView([51.1657, 10.4515], 6);

  // Add OpenStreetMap tiles
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
  }).addTo(map);

  // Initialize markers layer
  markersLayer = L.layerGroup().addTo(map);

  // Load initial matches
  loadMatches();

  // Add event listener for map movement
  map.on('moveend', loadMatches);
};

const loadMatches = async () => {
  if (!map) return;

  loading.value = true;

  try {
    const bounds = map.getBounds();

    // Expand the bounding box by 50% in each direction
    const latCenter = (bounds.getNorth() + bounds.getSouth()) / 2;
    const lngCenter = (bounds.getEast() + bounds.getWest()) / 2;
    const latRange = bounds.getNorth() - bounds.getSouth();
    const lngRange = bounds.getEast() - bounds.getWest();

    const expandFactor = 1.5; // 50% expansion
    const expandedLatRange = latRange * expandFactor;
    const expandedLngRange = lngRange * expandFactor;

    const request = {
      minLat: latCenter - expandedLatRange / 2,
      maxLat: latCenter + expandedLatRange / 2,
      minLng: lngCenter - expandedLngRange / 2,
      maxLng: lngCenter + expandedLngRange / 2
    };

    const fetchedMatches = await ApiService.getMatches(request);
    matches.value = fetchedMatches;

    // Clear existing markers
    if (markersLayer) {
      markersLayer.clearLayers();
    }

    // Add new markers for venues
    fetchedMatches.forEach(match => {
      if (match.venue?.latitude && match.venue?.longitude && markersLayer) {
        const marker = L.marker([match.venue.latitude, match.venue.longitude]);

        // Create popup content
        const popupContent = `
          <div class="match-popup">
            <h3>${match.homeTeam?.name || 'Unknown'} vs ${match.awayTeam?.name || 'Unknown'}</h3>
            ${match.time ? `<p><strong>Time:</strong> ${new Date(match.time).toLocaleString()}</p>` : ''}
            ${match.venue?.address ? `<p><strong>Venue:</strong> ${match.venue.address}</p>` : ''}
            ${match.competition?.name ? `<p><strong>Competition:</strong> ${match.competition.name}</p>` : ''}
            ${match.ageGroup?.name ? `<p><strong>Age Group:</strong> ${match.ageGroup.name}</p>` : ''}
          </div>
        `;

        marker.bindPopup(popupContent);
        markersLayer.addLayer(marker);
      }
    });
  } catch (error) {
    console.error('Error loading matches:', error);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  initMap();
});

onUnmounted(() => {
  if (map) {
    map.remove();
    map = null;
  }
});
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
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
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
</style>
