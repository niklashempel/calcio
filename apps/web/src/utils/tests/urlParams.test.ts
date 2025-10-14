import { describe, it, expect, beforeEach, vi } from 'vitest';
import { readFiltersFromUrl, writeFiltersToUrl, mergeFiltersWithRequest } from '../urlParams';
import type { FilterParams } from '../urlParams';

// Mock window.location and window.history
const mockLocation = {
  search: '',
  pathname: '/',
};

const mockHistory = {
  pushState: vi.fn(),
};

beforeEach(() => {
  // Reset mocks before each test
  mockLocation.search = '';
  mockLocation.pathname = '/';
  mockHistory.pushState.mockClear();

  // Override window properties
  Object.defineProperty(window, 'location', {
    value: mockLocation,
    writable: true,
    configurable: true,
  });

  Object.defineProperty(window, 'history', {
    value: mockHistory,
    writable: true,
    configurable: true,
  });
});

describe('readFiltersFromUrl', () => {
  it('should return empty filters when no URL parameters', () => {
    mockLocation.search = '';
    const filters = readFiltersFromUrl();
    expect(filters).toEqual({});
  });

  it('should read minDate and maxDate from URL', () => {
    mockLocation.search = '?minDate=2024-01-01&maxDate=2024-12-31';
    const filters = readFiltersFromUrl();
    expect(filters).toEqual({
      minDate: '2024-01-01',
      maxDate: '2024-12-31',
    });
  });

  it('should read competitions array from URL', () => {
    mockLocation.search = '?competitions=1&competitions=2&competitions=3';
    const filters = readFiltersFromUrl();
    expect(filters).toEqual({
      competitions: [1, 2, 3],
    });
  });

  it('should read ageGroups array from URL', () => {
    mockLocation.search = '?ageGroups=10&ageGroups=20';
    const filters = readFiltersFromUrl();
    expect(filters).toEqual({
      ageGroups: [10, 20],
    });
  });

  it('should read all filter types together', () => {
    mockLocation.search = '?minDate=2024-01-01&maxDate=2024-12-31&competitions=1&competitions=2&ageGroups=10';
    const filters = readFiltersFromUrl();
    expect(filters).toEqual({
      minDate: '2024-01-01',
      maxDate: '2024-12-31',
      competitions: [1, 2],
      ageGroups: [10],
    });
  });

  it('should filter out invalid competition IDs', () => {
    mockLocation.search = '?competitions=1&competitions=invalid&competitions=2';
    const filters = readFiltersFromUrl();
    expect(filters).toEqual({
      competitions: [1, 2],
    });
  });
});

describe('writeFiltersToUrl', () => {
  it('should clear URL when no filters provided', () => {
    mockLocation.search = '?minDate=2024-01-01';
    writeFiltersToUrl({});
    expect(mockHistory.pushState).toHaveBeenCalledWith({}, '', '/');
  });

  it('should write minDate and maxDate to URL', () => {
    const filters: FilterParams = {
      minDate: '2024-01-01',
      maxDate: '2024-12-31',
    };
    writeFiltersToUrl(filters);
    expect(mockHistory.pushState).toHaveBeenCalledWith(
      {},
      '',
      '?minDate=2024-01-01&maxDate=2024-12-31',
    );
  });

  it('should write competitions array to URL', () => {
    const filters: FilterParams = {
      competitions: [1, 2, 3],
    };
    writeFiltersToUrl(filters);
    expect(mockHistory.pushState).toHaveBeenCalledWith(
      {},
      '',
      '?competitions=1&competitions=2&competitions=3',
    );
  });

  it('should write ageGroups array to URL', () => {
    const filters: FilterParams = {
      ageGroups: [10, 20],
    };
    writeFiltersToUrl(filters);
    expect(mockHistory.pushState).toHaveBeenCalledWith({}, '', '?ageGroups=10&ageGroups=20');
  });

  it('should write all filter types together', () => {
    const filters: FilterParams = {
      minDate: '2024-01-01',
      maxDate: '2024-12-31',
      competitions: [1, 2],
      ageGroups: [10],
    };
    writeFiltersToUrl(filters);
    expect(mockHistory.pushState).toHaveBeenCalledWith(
      {},
      '',
      '?minDate=2024-01-01&maxDate=2024-12-31&competitions=1&competitions=2&ageGroups=10',
    );
  });

  it('should preserve non-filter parameters', () => {
    mockLocation.search = '?someOtherParam=value';
    const filters: FilterParams = {
      minDate: '2024-01-01',
    };
    writeFiltersToUrl(filters);
    expect(mockHistory.pushState).toHaveBeenCalledWith(
      {},
      '',
      '?someOtherParam=value&minDate=2024-01-01',
    );
  });

  it('should replace existing filter parameters', () => {
    mockLocation.search = '?minDate=2024-01-01&competitions=1';
    const filters: FilterParams = {
      minDate: '2024-06-01',
      competitions: [2, 3],
    };
    writeFiltersToUrl(filters);
    expect(mockHistory.pushState).toHaveBeenCalledWith(
      {},
      '',
      '?minDate=2024-06-01&competitions=2&competitions=3',
    );
  });
});

describe('mergeFiltersWithRequest', () => {
  it('should merge filters into request', () => {
    const request = {
      minLat: 52.4,
      maxLat: 52.6,
      minLng: 13.3,
      maxLng: 13.5,
    };
    const filters: FilterParams = {
      minDate: '2024-01-01',
      competitions: [1, 2],
    };
    const result = mergeFiltersWithRequest(request, filters);
    expect(result).toEqual({
      minLat: 52.4,
      maxLat: 52.6,
      minLng: 13.3,
      maxLng: 13.5,
      minDate: '2024-01-01',
      competitions: [1, 2],
    });
  });

  it('should handle empty filters', () => {
    const request = {
      minLat: 52.4,
      maxLat: 52.6,
      minLng: 13.3,
      maxLng: 13.5,
    };
    const filters: FilterParams = {};
    const result = mergeFiltersWithRequest(request, filters);
    expect(result).toEqual({
      minLat: 52.4,
      maxLat: 52.6,
      minLng: 13.3,
      maxLng: 13.5,
    });
  });
});
