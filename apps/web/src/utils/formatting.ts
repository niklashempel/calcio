import type { MatchDto } from '@/types/api';

export interface FormattedMatchBits {
  dateRight: string;
  timeRight: string;
  header: string;
  home: string;
  away: string;
}

export function formatMatch(m: MatchDto, locale = 'de-DE'): FormattedMatchBits {
  const t = m.time ? new Date(m.time) : null;
  const weekday = t ? t.toLocaleDateString(locale, { weekday: 'short' }).replace('.', '') : '';
  const dayMonth = t
    ? `${String(t.getDate()).padStart(2, '0')}.${String(t.getMonth() + 1).padStart(2, '0')}.`
    : '';
  const dateRight = t ? `${weekday}, ${dayMonth}` : '';
  const timeRight = t
    ? `${t.toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit' })} Uhr`
    : '';
  const comp = m.competition?.name || '';
  const age = m.ageGroup?.name || '';
  const header = [age, comp].filter(Boolean).join(' | ');
  const home = m.homeTeam?.name || 'Heim unbekannt';
  const away = m.awayTeam?.name || 'Gast unbekannt';
  return { dateRight, timeRight, header, home, away };
}
