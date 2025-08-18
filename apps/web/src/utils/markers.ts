import type { MatchDto } from '@/types/api';
import { formatMatch } from './formatting';
import { sortMatchesChronologically } from './grouping';

export interface MarkerData {
  id: number;
  lat: number;
  lng: number;
  popupHtml: string;
}

function buildPopupHtml(venueMatches: MatchDto[]): string {
  const first = venueMatches[0];
  if (!first?.venue) return '';
  const v = first.venue;
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
    const bits = formatMatch(m);
    const openTag = m.url ? `<a href="${m.url}" target="_blank" rel="noopener noreferrer">` : '';
    const closeTag = m.url ? '</a>' : '';
    return (
      `<li class="match-card">${openTag}` +
      `<div class="match-header">${bits.header}</div>` +
      `<div class="match-line"><span class="team home">${bits.home}</span><span class="date">${bits.dateRight}</span></div>` +
      `<div class="match-line"><span class="team away">${bits.away}</span><span class="time">${bits.timeRight}</span></div>` +
      `${closeTag}</li>`
    );
  };
  const buildSection = (title: string, arr: MatchDto[], sortDesc = false) => {
    if (!arr.length) return '';
    sortMatchesChronologically(arr, sortDesc);
    return `<h4 class="group">${title} (${arr.length})</h4><ul class="matches match-cards">${arr.map(fmt).join('')}</ul>`;
  };
  return `
    <div class="match-popup">
      <h3>${v.address || 'Spielort'} (${venueMatches.length} Spiel${venueMatches.length !== 1 ? 'e' : ''})</h3>
      ${buildSection('Heute', today)}
      ${buildSection('Nächste Spiele', upcoming)}
      ${buildSection('Letzte Spiele', past, true)}
    </div>
  `;
}

export function buildMarkers(allMatches: MatchDto[]): MarkerData[] {
  const matchesByVenue = new Map<number, MatchDto[]>();
  for (const m of allMatches) {
    const v = m.venue;
    if (!v || v.latitude == null || v.longitude == null) continue;
    if (!matchesByVenue.has(v.id)) matchesByVenue.set(v.id, []);
    matchesByVenue.get(v.id)!.push(m);
  }
  const markers: MarkerData[] = [];
  for (const [venueId, venueMatches] of matchesByVenue.entries()) {
    const first = venueMatches[0];
    if (!first?.venue) continue;
    const v = first.venue;
    const popupHtml = buildPopupHtml(venueMatches);
    markers.push({ id: venueId, lat: v.latitude!, lng: v.longitude!, popupHtml });
  }
  return markers;
}
