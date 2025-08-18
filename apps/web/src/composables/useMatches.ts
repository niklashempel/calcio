import { ApiService } from '@/services/api';
import type { GetMatchesRequest, MatchDto } from '@/types/api';
import type { GroupedMatches } from '@/utils/grouping';
import { groupMatches } from '@/utils/grouping';
import { ref } from 'vue';

export function useMatches() {
  const matches = ref<MatchDto[]>([]);
  const loading = ref(false);
  const lastBounds = ref<GetMatchesRequest | null>(null);

  async function load(request: GetMatchesRequest) {
    // Avoid identical fetch if bounds unchanged
    if (lastBounds.value && JSON.stringify(lastBounds.value) === JSON.stringify(request)) return;
    lastBounds.value = request;
    loading.value = true;
    try {
      matches.value = await ApiService.getMatches(request);
    } finally {
      loading.value = false;
    }
  }

  function group(): Record<number, GroupedMatches & { venue: MatchDto['venue']; count: number }> {
    const byVenue: Record<number, GroupedMatches & { venue: MatchDto['venue']; count: number }> =
      {};
    for (const m of matches.value) {
      const v = m.venue;
      if (!v?.id || !v.latitude || !v.longitude) continue;
      if (!byVenue[v.id]) {
        byVenue[v.id] = { ...groupMatches([]), venue: v, count: 0 };
      }
      // accumulate temporarily in a flat list (we'll group after loop) -> simpler: push into temp array
    }
    // Actually we need to group per venue from original list
    const temp: Record<number, MatchDto[]> = {};
    for (const m of matches.value) {
      const v = m.venue;
      if (!v?.id || !v.latitude || !v.longitude) continue;
      if (!temp[v.id]) {
        temp[v.id] = [];
      }
      temp[v.id]!.push(m);
    }
    for (const idStr of Object.keys(temp)) {
      const arr = temp[Number(idStr)];
      if (!arr || arr.length === 0) continue;
      const first = arr[0];
      if (!first || !first.venue) continue;
      const id = Number(idStr);
      const grouped = groupMatches(arr);
      byVenue[id] = { ...grouped, venue: first.venue, count: arr.length };
    }
    return byVenue;
  }

  return { matches, loading, load, group };
}
