import type { GroupedMatches, MatchDto, MatchLocationDto } from '@/types/api';
import { formatMatch } from './formatting';

export interface MarkerData {
  id: number;
  lat: number;
  lng: number;
  popupHtml: string;
}

export function buildPopupHtml(group: GroupedMatches, venue?: { address?: string }): string {
  const totalCount = group.today.length + group.upcoming.length + group.past.length;

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
      <h3>${venue?.address || 'Spielort'} (${totalCount} Spiel${totalCount !== 1 ? 'e' : ''})</h3>
      ${buildSection('Heute', today)}
      ${buildSection('Nächste Spiele', upcoming)}
      ${buildSection('Letzte Spiele', past)}
    </div>
  `;
}

export function buildMarkers(locations: MatchLocationDto[]): MarkerData[] {
  return locations
    .filter((loc) => loc.venue?.latitude != null && loc.venue?.longitude != null)
    .map((loc) => ({
      id: loc.venue!.id,
      lat: loc.venue!.latitude!,
      lng: loc.venue!.longitude!,
      popupHtml: '<div class="match-popup loading">Lade Spiele...</div>',
    }));
}
