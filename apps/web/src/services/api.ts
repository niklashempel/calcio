import type { GetMatchesRequest, GroupedMatches } from '@/types/api';

// Use relative base so nginx proxy (/api -> backend) avoids CORS
const API_BASE_URL = '/api';

export class ApiService {
  private static async fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const url = `${API_BASE_URL.replace(/\/$/, '')}${endpoint}`;
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
      ...options,
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  static async getMatches(request: GetMatchesRequest): Promise<GroupedMatches[]> {
    const params = new URLSearchParams();

    if (request.minLat !== undefined) params.append('minLat', request.minLat.toString());
    if (request.maxLat !== undefined) params.append('maxLat', request.maxLat.toString());
    if (request.minLng !== undefined) params.append('minLng', request.minLng.toString());
    if (request.maxLng !== undefined) params.append('maxLng', request.maxLng.toString());
    if (request.minDate !== undefined) params.append('minDate', request.minDate);
    if (request.maxDate !== undefined) params.append('maxDate', request.maxDate);
    if (request.competitions && request.competitions.length > 0) {
      request.competitions.forEach(id => params.append('competitions', id.toString()));
    }
    if (request.ageGroups && request.ageGroups.length > 0) {
      request.ageGroups.forEach(id => params.append('ageGroups', id.toString()));
    }

    const queryString = params.toString();
    const endpoint = `/matches${queryString ? `?${queryString}` : ''}`;

    return this.fetchApi<GroupedMatches[]>(endpoint);
  }
}
