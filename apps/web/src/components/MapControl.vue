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
// Suppress Leaflet private property typing without using 'any'
delete (L.Icon.Default.prototype as unknown as { _getIconUrl?: unknown })._getIconUrl;
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
    attribution:
      '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
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
      maxLng: lngCenter + expandedLngRange / 2,
    };

    const fetchedMatches = await ApiService.getMatches(request);
    matches.value = fetchedMatches;

    // Clear existing markers
    if (markersLayer) {
      markersLayer.clearLayers();
    }

    // Group matches by venue id
    const matchesByVenue = new Map<number, MatchDto[]>();
    for (const m of fetchedMatches) {
      const vId = m.venue?.id;
      if (!vId || !m.venue?.latitude || !m.venue.longitude) continue;
      if (!matchesByVenue.has(vId)) {
        matchesByVenue.set(vId, []);
      }
      matchesByVenue.get(vId)!.push(m);
    }

    // Add one marker per venue with list of matches (grouped: Today, Upcoming, Past)
    for (const [, venueMatches] of matchesByVenue.entries()) {
      const v = venueMatches[0].venue!;
      if (!markersLayer) continue;
      const marker = L.marker([v.latitude!, v.longitude!]);

      // Prepare date boundaries
      const now = new Date();
      const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      const todayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);

      const today: MatchDto[] = [];
      const upcoming: MatchDto[] = [];
      const past: MatchDto[] = [];

      for (const m of venueMatches) {
        if (!m.time) {
          past.push(m);
          continue;
        }
        const t = new Date(m.time);
        if (t >= todayStart && t < todayEnd) today.push(m);
        else if (t >= todayEnd) upcoming.push(m);
        else past.push(m);
      }

      const fmt = (m: MatchDto) => {
        const t = m.time ? new Date(m.time) : null;
        const weekday = t
          ? t.toLocaleDateString('de-DE', { weekday: 'short' }).replace('.', '')
          : '';
        const dayMonth = t
          ? `${String(t.getDate()).padStart(2, '0')}.${String(t.getMonth() + 1).padStart(2, '0')}.`
          : '';
        const dateRight = t ? `${weekday}, ${dayMonth}` : '';
        const timeRight = t
          ? `${t.toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' })} Uhr`
          : '';
        const comp = m.competition?.name || '';
        const age = m.ageGroup?.name || '';
        const home = m.homeTeam?.name || 'Heim unbekannt';
        const away = m.awayTeam?.name || 'Gast unbekannt';
        const header = [age, comp].filter(Boolean).join(' | ');
        const openTag = m.url
          ? `<a href=\"${m.url}\" target=\"_blank\" rel=\"noopener noreferrer\">`
          : '';
        const closeTag = m.url ? '</a>' : '';
        return (
          `<li class=\"match-card\">` +
          openTag +
          `<div class=\"match-header\">${header}</div>` +
          `<div class=\"match-line\"><span class=\"team home\">${home}</span><span class=\"date\">${dateRight}</span></div>` +
          `<div class=\"match-line\"><span class=\"team away\">${away}</span><span class=\"time\">${timeRight}</span></div>` +
          closeTag +
          `</li>`
        );
      };

      const buildSection = (title: string, arr: MatchDto[], sortDesc = false) => {
        if (!arr.length) return '';
        arr.sort((a, b) => {
          if (!a.time && !b.time) return 0;
          if (!a.time) return 1;
          if (!b.time) return -1;
          const cmp = a.time.localeCompare(b.time);
          return sortDesc ? -cmp : cmp;
        });
        return `<h4 class=\"group\">${title} (${arr.length})</h4><ul class=\"matches match-cards\">${arr.map(fmt).join('')}</ul>`;
      };

      const popupContent = `
        <div class=\"match-popup\">
          <h3>${v.address || 'Venue'} (${venueMatches.length} Match${venueMatches.length !== 1 ? 'es' : ''})</h3>
          ${buildSection('Today', today)}
          ${buildSection('Upcoming', upcoming)}
          ${buildSection('Past', past, true)}
        </div>
      `;
      marker.bindPopup(popupContent, { maxWidth: 380 });
      markersLayer.addLayer(marker);
    }
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
