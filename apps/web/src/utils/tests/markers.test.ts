import type { GroupedMatches, MatchDto, MatchLocationDto } from '@/types/api';
import { describe, expect, it } from 'vitest';
import { buildMarkers, buildPopupHtml } from '../markers';

function match(id: number, offsetHours: number): MatchDto {
  const date = new Date();
  date.setHours(date.getHours() + offsetHours);
  return {
    id,
    time: date.toISOString(),
    homeTeam: { id: id * 10 + 1, name: 'Heim' + id },
    awayTeam: { id: id * 10 + 2, name: 'Gast' + id },
    competition: { id: 1, name: 'Liga' },
    ageGroup: { id: 1, name: 'Herren' },
  } as MatchDto;
}

describe('buildPopupHtml', () => {
  it('generates popup HTML with grouped matches', () => {
    const grouped: GroupedMatches = {
      today: [match(1, 0)],
      upcoming: [match(2, 30)],
      past: [match(3, -30)],
    };
    const venue = { address: 'Test Stadium' };

    const html = buildPopupHtml(grouped, venue);

    expect(html).toContain('Test Stadium');
    expect(html).toContain('3 Spiele');
    expect(html).toContain('Heute');
    expect(html).toContain('Nächste Spiele');
    expect(html).toContain('Letzte Spiele');
  });

  it('shows correct singular/plural for match count', () => {
    const grouped: GroupedMatches = {
      today: [match(1, 0)],
      upcoming: [],
      past: [],
    };

    const html = buildPopupHtml(grouped);

    expect(html).toContain('1 Spiel');
    expect(html).not.toContain('1 Spiele');
  });

  it('uses fallback text when no venue provided', () => {
    const grouped: GroupedMatches = {
      today: [],
      upcoming: [],
      past: [],
    };

    const html = buildPopupHtml(grouped);

    expect(html).toContain('Spielort');
  });
});

describe('buildMarkers', () => {
  it('creates markers from location data with loading state', () => {
    const locations: MatchLocationDto[] = [
      {
        venue: { id: 1, latitude: 11, longitude: 21, address: 'Venue 1' },
      },
      {
        venue: { id: 2, latitude: 12, longitude: 22, address: 'Venue 2' },
      },
    ];

    const markers = buildMarkers(locations);

    expect(markers.length).toBe(2);

    const marker1 = markers.find((m) => m.id === 1)!;
    expect(marker1.lat).toBe(11);
    expect(marker1.lng).toBe(21);
    expect(marker1.popupHtml).toContain('loading');
    expect(marker1.popupHtml).toContain('Lade Spiele');

    const marker2 = markers.find((m) => m.id === 2)!;
    expect(marker2.lat).toBe(12);
    expect(marker2.lng).toBe(22);
    expect(marker2.popupHtml).toContain('loading');
  });

  it('filters out locations without coordinates', () => {
    const locations: MatchLocationDto[] = [
      {
        venue: { id: 1, latitude: 11, longitude: 21 },
      },
      {
        venue: { id: 2, latitude: undefined, longitude: 22 },
      },
      {
        venue: { id: 3, latitude: 13, longitude: undefined },
      },
      {
        venue: undefined,
      },
    ];

    const markers = buildMarkers(locations);

    expect(markers.length).toBe(1);
    expect(markers[0]!.id).toBe(1);
  });
});
