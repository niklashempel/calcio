import type { GetMatchesRequest, MatchDto } from '@/types/api';

const API_BASE_URL = 'http://localhost:5149/api'; // API URL from launchSettings.json

export class ApiService {
  private static async fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
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

  static async getMatches(request: GetMatchesRequest): Promise<MatchDto[]> {
    const params = new URLSearchParams();

    if (request.minLat !== undefined) params.append('minLat', request.minLat.toString());
    if (request.maxLat !== undefined) params.append('maxLat', request.maxLat.toString());
    if (request.minLng !== undefined) params.append('minLng', request.minLng.toString());
    if (request.maxLng !== undefined) params.append('maxLng', request.maxLng.toString());

    const queryString = params.toString();
    const endpoint = `/matches${queryString ? `?${queryString}` : ''}`;

    return this.fetchApi<MatchDto[]>(endpoint);
  }
}
