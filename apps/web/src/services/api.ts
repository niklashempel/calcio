import type { GetMatchesRequest, GroupedMatches } from '@/types/api';

declare global {
  interface Window {
    __CALCIO_API_BASE__?: string;
  }
}

// Resolve API base URL in order of precedence:
// 1. Runtime global (window.__CALCIO_API_BASE__) injected by an optional script for truly dynamic config
// 2. Vite build-time env var VITE_API_BASE_URL
// 3. Fallback local dev default
const API_BASE_URL: string =
  (typeof window !== 'undefined' && window.__CALCIO_API_BASE__) ||
  import.meta.env.VITE_API_BASE_URL ||
  'http://localhost:8080/api';

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

    const queryString = params.toString();
    const endpoint = `/matches${queryString ? `?${queryString}` : ''}`;

    return this.fetchApi<GroupedMatches[]>(endpoint);
  }
}
