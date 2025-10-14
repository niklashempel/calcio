import type { GetMatchesRequest } from '@/types/api';

export interface FilterParams {
  minDate?: string;
  maxDate?: string;
  competitions?: number[];
  ageGroups?: number[];
}

export function readFiltersFromUrl(): FilterParams {
  const params = new URLSearchParams(window.location.search);
  const filters: FilterParams = {};

  const minDate = params.get('minDate');
  if (minDate) filters.minDate = minDate;

  const maxDate = params.get('maxDate');
  if (maxDate) filters.maxDate = maxDate;

  const competitions = params.getAll('competitions');
  if (competitions.length > 0) {
    filters.competitions = competitions.map((id) => parseInt(id, 10)).filter((id) => !isNaN(id));
  }

  const ageGroups = params.getAll('ageGroups');
  if (ageGroups.length > 0) {
    filters.ageGroups = ageGroups.map((id) => parseInt(id, 10)).filter((id) => !isNaN(id));
  }

  return filters;
}

export function writeFiltersToUrl(filters: FilterParams): void {
  const params = new URLSearchParams(window.location.search);

  // Remove existing filter parameters
  params.delete('minDate');
  params.delete('maxDate');
  params.delete('competitions');
  params.delete('ageGroups');

  // Add new filter parameters
  if (filters.minDate) params.set('minDate', filters.minDate);
  if (filters.maxDate) params.set('maxDate', filters.maxDate);
  if (filters.competitions && filters.competitions.length > 0) {
    filters.competitions.forEach((id) => params.append('competitions', id.toString()));
  }
  if (filters.ageGroups && filters.ageGroups.length > 0) {
    filters.ageGroups.forEach((id) => params.append('ageGroups', id.toString()));
  }

  // Update URL without reloading the page
  const newUrl = params.toString() ? `?${params.toString()}` : window.location.pathname;
  window.history.pushState({}, '', newUrl);
}

export function mergeFiltersWithRequest(
  request: Omit<GetMatchesRequest, 'minDate' | 'maxDate' | 'competitions' | 'ageGroups'>,
  filters: FilterParams,
): GetMatchesRequest {
  return {
    ...request,
    ...filters,
  };
}
