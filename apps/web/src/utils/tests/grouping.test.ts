import type { MatchDto } from '@/types/api';
import { describe, expect, it } from 'vitest';
import { groupMatches, sortMatchesChronologically } from '../grouping';

function buildMatch(offsetHours: number): MatchDto {
  const date = new Date();
  date.setHours(date.getHours() + offsetHours);
  return {
    id: Math.random(),
    time: date.toISOString(),
    venue: { id: 1, latitude: 1, longitude: 1 },
  };
}

describe('groupMatches', () => {
  it('groups today, upcoming, past', () => {
    const past = buildMatch(-30); // definitely previous day
    const today = buildMatch(0);
    const upcoming = buildMatch(30); // definitely next day
    const now = new Date();
    const grouped = groupMatches([past, today, upcoming], now);
    expect(grouped.past.length).toBe(1);
    expect(grouped.today.length).toBe(1);
    expect(grouped.upcoming.length).toBe(1);
  });
});

describe('sortMatchesChronologically', () => {
  it('sorts ascending/descending', () => {
    const a = buildMatch(1);
    const b = buildMatch(2);
    const arr = [b, a];
    sortMatchesChronologically(arr);
    expect(arr[0]).toBe(a);
    sortMatchesChronologically(arr, true);
    expect(arr[0]).toBe(b);
  });
});
