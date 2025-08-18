import type { MatchDto } from '@/types/api';

export interface GroupedMatches {
  today: MatchDto[];
  upcoming: MatchDto[];
  past: MatchDto[];
}

export function groupMatches(matches: MatchDto[], now: Date = new Date()): GroupedMatches {
  const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const todayEnd = new Date(todayStart);
  todayEnd.setDate(todayStart.getDate() + 1);

  const today: MatchDto[] = [];
  const upcoming: MatchDto[] = [];
  const past: MatchDto[] = [];

  for (const m of matches) {
    if (!m.time) {
      past.push(m);
      continue;
    }
    const t = new Date(m.time);
    if (t >= todayStart && t < todayEnd) today.push(m);
    else if (t >= todayEnd) upcoming.push(m);
    else past.push(m);
  }
  return { today, upcoming, past };
}

export function sortMatchesChronologically(arr: MatchDto[], desc = false): void {
  arr.sort((a, b) => {
    if (!a.time && !b.time) return 0;
    if (!a.time) return 1;
    if (!b.time) return -1;
    const cmp = a.time.localeCompare(b.time);
    return desc ? -cmp : cmp;
  });
}
