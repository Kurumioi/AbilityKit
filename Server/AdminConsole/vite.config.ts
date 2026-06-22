import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  base: '/admin/',
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    proxy: {
      '/api': {
        target: process.env.ABILITYKIT_GATEWAY_URL || 'http://localhost:5000',
        changeOrigin: true
      },
      '/debug': {
        target: process.env.ABILITYKIT_GATEWAY_URL || 'http://localhost:5000',
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: '../Orleans/src/AbilityKit.Orleans.Gateway/wwwroot/admin',
    emptyOutDir: true,
    sourcemap: true
  }
});
