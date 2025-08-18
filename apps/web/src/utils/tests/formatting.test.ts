import type { MatchDto } from '@/types/api';
import { describe, expect, it } from 'vitest';
import { formatMatch } from '../formatting';

const base: MatchDto = {
  id: 1,
  time: new Date().toISOString(),
  homeTeam: { id: 1, name: 'Heim' },
  awayTeam: { id: 2, name: 'Gast' },
  competition: { id: 1, name: 'Liga' },
  ageGroup: { id: 1, name: 'Herren' },
  venue: { id: 5, latitude: 1, longitude: 1 },
};

describe('formatMatch', () => {
  it('returns header parts and formatted date/time', () => {
    const f = formatMatch(base, 'de-DE');
    expect(f.header).toContain('Herren');
    expect(f.header).toContain('Liga');
    expect(f.home).toBe('Heim');
    expect(f.away).toBe('Gast');
    expect(f.timeRight).toMatch(/Uhr$/);
  });
});
