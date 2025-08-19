import type { GroupedMatches, MatchDto } from '@/types/api';
import { formatMatch } from './formatting';

export interface MarkerData {
  id: number;
  lat: number;
  lng: number;
  popupHtml: string;
}

function buildPopupHtml(group: GroupedMatches): string {
  const v = group.venue;
  if (!v) return '';
  // Server already returns sorted ascending per bucket; we reverse past for display (latest first)
  const today = [...group.today];
  const upcoming = [...group.upcoming];
  const past = [...group.past].sort((a, b) => {
    if (!a.time && !b.time) return 0;
    if (!a.time) return 1;
    if (!b.time) return -1;
    return b.time.localeCompare(a.time); // desc
  });
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
  const buildSection = (title: string, arr: MatchDto[]) => {
    if (!arr.length) return '';
    return `<h4 class="group">${title} (${arr.length})</h4><ul class="matches match-cards">${arr.map(fmt).join('')}</ul>`;
  };
  return `
    <div class="match-popup">
      <h3>${v.address || 'Spielort'} (${group.count} Spiel${group.count !== 1 ? 'e' : ''})</h3>
      ${buildSection('Heute', today)}
      ${buildSection('Nächste Spiele', upcoming)}
      ${buildSection('Letzte Spiele', past)}
    </div>
  `;
}

export function buildMarkers(groups: GroupedMatches[]): MarkerData[] {
  return groups
    .filter((g) => g.venue && g.venue.latitude != null && g.venue.longitude != null)
    .map((g) => ({
      id: g.venueId,
      lat: g.venue!.latitude!,
      lng: g.venue!.longitude!,
      popupHtml: buildPopupHtml(g),
    }));
}
