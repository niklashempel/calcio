import type { MatchDto } from '@/types/api';
import { describe, expect, it } from 'vitest';
import { buildMarkers } from '../markers';

function match(id: number, venueId: number, offsetHours: number): MatchDto {
  const date = new Date();
  date.setHours(date.getHours() + offsetHours);
  return {
    id,
    time: date.toISOString(),
    venue: { id: venueId, latitude: 10 + venueId, longitude: 20 + venueId },
    homeTeam: { id: id * 10 + 1, name: 'Heim' + id },
    awayTeam: { id: id * 10 + 2, name: 'Gast' + id },
    competition: { id: 1, name: 'Liga' },
    ageGroup: { id: 1, name: 'Herren' },
  } as MatchDto;
}

describe('buildMarkers', () => {
  it('creates one marker per venue with grouped popup sections', () => {
    const ms: MatchDto[] = [match(1, 1, 0), match(2, 1, 30), match(3, 2, -30)];
    const markers = buildMarkers(ms);
    expect(markers.length).toBe(2);
    const popup1 = markers.find((m) => m.id === 1)!.popupHtml;
    expect(popup1).toContain('Heute');
    expect(popup1).toContain('Nächste Spiele');
    const popup2 = markers.find((m) => m.id === 2)!.popupHtml;
    expect(popup2).toContain('Letzte Spiele');
  });
});
