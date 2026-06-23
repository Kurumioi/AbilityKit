<template>
  <aside class="sidebar">
    <div class="brand">
      <div class="brand-mark">AK</div>
      <div>
        <strong>AbilityKit</strong>
        <span>Admin Console</span>
      </div>
    </div>
    <nav class="sidebar-nav" aria-label="Admin sections">
      <a
        v-for="item in items"
        :key="item.key"
        :href="`#${item.path}`"
        :class="{ active: item.key === activeKey }"
        :title="item.description"
        @click.prevent="navigate(item.path)">
        {{ item.label }}
      </a>
    </nav>
    <div class="sidebar-footer">
      <span class="status-dot" :class="maintenanceMode ? 'warn-dot' : 'ok-dot'"></span>
      <div>
        <strong>{{ serverModeLabel }}</strong>
        <small>{{ environmentName || 'Unknown Environment' }}</small>
      </div>
    </div>
  </aside>
</template>

<script setup lang="ts">
import type { AdminNavigationItem, AdminRouteKey } from '../navigation/adminNavigation';

defineProps<{
  items: AdminNavigationItem[];
  activeKey: AdminRouteKey;
  serverModeLabel: string;
  environmentName?: string | null;
  maintenanceMode?: boolean;
}>();

const emit = defineEmits<{
  navigate: [path: string];
}>();

function navigate(path: string): void {
  emit('navigate', path);
}
</script>
