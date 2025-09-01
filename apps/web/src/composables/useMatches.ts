import { ApiService } from '@/services/api';
import { type GetMatchesRequest, type GroupedMatches } from '@/types/api';
import { ref } from 'vue';

export function useMatches() {
  const matches = ref<GroupedMatches[]>([]);
  const loading = ref(false);
  const lastBounds = ref<GetMatchesRequest | null>(null);

  async function load(request: GetMatchesRequest) {
    if (
      lastBounds.value &&
      JSON.stringify(lastBounds.value) === JSON.stringify(request) &&
      matches.value
    )
      return;
    lastBounds.value = request;
    loading.value = true;
    try {
      matches.value = await ApiService.getMatches(request);
    } finally {
      loading.value = false;
    }
  }

  return { matches, loading, load };
}
