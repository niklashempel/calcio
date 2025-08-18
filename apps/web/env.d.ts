/// <reference types="vite/client" />

// Vue SFC shim
declare module '*.vue' {
  import type { DefineComponent } from 'vue';
  // Generic component type without using 'any'
  const component: DefineComponent<Record<string, unknown>, Record<string, unknown>, unknown>;
  export default component;
}
