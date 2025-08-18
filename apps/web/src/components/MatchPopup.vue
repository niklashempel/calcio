<template>
  <div class="match-popup">
    <h3>{{ venueTitle }} ({{ total }} Spiel<span v-if="total !== 1">e</span>)</h3>
    <section v-if="today.length">
      <h4 class="group">Heute ({{ today.length }})</h4>
      <ul class="matches match-cards">
        <li v-for="m in todaySorted" :key="m.id" class="match-card">
          <component :is="linkTag(m)" :href="m.url" target="_blank" rel="noopener noreferrer">
            <div class="match-header">{{ format(m).header }}</div>
            <div class="match-line">
              <span class="team home">{{ format(m).home }}</span>
              <span class="date">{{ format(m).dateRight }}</span>
            </div>
            <div class="match-line">
              <span class="team away">{{ format(m).away }}</span>
              <span class="time">{{ format(m).timeRight }}</span>
            </div>
          </component>
        </li>
      </ul>
    </section>
    <section v-if="upcoming.length">
      <h4 class="group">Nächste Spiele ({{ upcoming.length }})</h4>
      <ul class="matches match-cards">
        <li v-for="m in upcomingSorted" :key="m.id" class="match-card">
          <component :is="linkTag(m)" :href="m.url" target="_blank" rel="noopener noreferrer">
            <div class="match-header">{{ format(m).header }}</div>
            <div class="match-line">
              <span class="team home">{{ format(m).home }}</span>
              <span class="date">{{ format(m).dateRight }}</span>
            </div>
            <div class="match-line">
              <span class="team away">{{ format(m).away }}</span>
              <span class="time">{{ format(m).timeRight }}</span>
            </div>
          </component>
        </li>
      </ul>
    </section>
    <section v-if="past.length">
      <h4 class="group">Letzte Spiele ({{ past.length }})</h4>
      <ul class="matches match-cards">
        <li v-for="m in pastSorted" :key="m.id" class="match-card">
          <component :is="linkTag(m)" :href="m.url" target="_blank" rel="noopener noreferrer">
            <div class="match-header">{{ format(m).header }}</div>
            <div class="match-line">
              <span class="team home">{{ format(m).home }}</span>
              <span class="date">{{ format(m).dateRight }}</span>
            </div>
            <div class="match-line">
              <span class="team away">{{ format(m).away }}</span>
              <span class="time">{{ format(m).timeRight }}</span>
            </div>
          </component>
        </li>
      </ul>
    </section>
  </div>
</template>

<script setup lang="ts">
import type { MatchDto } from '@/types/api';
import { formatMatch } from '@/utils/formatting';
import { sortMatchesChronologically } from '@/utils/grouping';
import { computed } from 'vue';

const props = defineProps<{ venueAddress?: string; matches: MatchDto[] }>();

const now = () => new Date();

const grouped = computed(() => {
  const n = now();
  const todayStart = new Date(n.getFullYear(), n.getMonth(), n.getDate());
  const todayEnd = new Date(n.getFullYear(), n.getMonth(), n.getDate() + 1);
  const today: MatchDto[] = [];
  const upcoming: MatchDto[] = [];
  const past: MatchDto[] = [];
  for (const m of props.matches) {
    if (!m.time) {
      past.push(m);
      continue;
    }
    const t = new Date(m.time);
    if (t >= todayStart && t < todayEnd) today.push(m);
    else if (t >= todayEnd) upcoming.push(m);
    else past.push(m);
  }
  sortMatchesChronologically(today);
  sortMatchesChronologically(upcoming);
  sortMatchesChronologically(past, true);
  return { today, upcoming, past };
});

const today = computed(() => grouped.value.today);
const upcoming = computed(() => grouped.value.upcoming);
const past = computed(() => grouped.value.past);
const todaySorted = today;
const upcomingSorted = upcoming;
const pastSorted = past;
const total = computed(() => props.matches.length);
const venueTitle = computed(() => props.venueAddress || 'Spielort');
const format = (m: MatchDto) => formatMatch(m);
const linkTag = (m: MatchDto) => (m.url ? 'a' : 'div');
</script>

<style scoped></style>
