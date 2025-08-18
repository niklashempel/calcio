<script setup lang="ts">
import MapControl from './components/MapControl.vue';
</script>

<template>
  <div id="app">
    <header>
  <h1>Calcio - Karte</h1>
    </header>

    <main>
      <MapControl />
    </main>
  </div>
</template>

<style scoped>
#app {
  height: 100vh;
  display: flex;
  flex-direction: column;
}

header {
  line-height: 1.5;
  padding: 1rem 2rem;
  background-color: #f8f9fa;
  border-bottom: 1px solid #e9ecef;
  flex-shrink: 0;
}

header h1 {
  margin: 0;
  color: #333;
  font-size: 1.5rem;
}

main {
  flex: 1;
  padding: 1rem;
  overflow: hidden;
}
</style>
