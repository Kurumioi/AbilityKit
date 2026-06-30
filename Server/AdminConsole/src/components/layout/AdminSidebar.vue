<template>
  <aside class="sidebar">
    <div class="brand">
      <div class="brand-mark">AK</div>
      <div>
        <strong>AbilityKit</strong>
        <span>管理后台</span>
      </div>
    </div>
    <nav class="sidebar-nav" aria-label="后台模块">
      <section v-for="group in groupedItems" :key="group.key" class="sidebar-nav-group">
        <p class="sidebar-group-title">{{ group.label }}</p>
        <a
          v-for="item in group.items"
          :key="item.key"
          :href="`#${item.path}`"
          :class="{ active: item.key === activeKey, danger: item.danger }"
          :title="item.description"
          @click.prevent="navigate(item.path)">
          <span>
            <strong>{{ item.label }}</strong>
            <small>{{ item.description }}</small>
          </span>
          <span v-if="item.requiresSession" class="nav-flag">会话</span>
        </a>
      </section>
    </nav>
    <div class="sidebar-footer">
      <span class="status-dot" :class="maintenanceMode ? 'warn-dot' : 'ok-dot'"></span>
      <div>
        <strong>{{ serverModeLabel }}</strong>
        <small>{{ environmentName || '未知环境' }}</small>
      </div>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { AdminNavigationItem, AdminRouteKey } from '../../navigation/adminNavigation';

const props = defineProps<{
  items: AdminNavigationItem[];
  activeKey: AdminRouteKey;
  serverModeLabel: string;
  environmentName?: string | null;
  maintenanceMode?: boolean;
}>();

const emit = defineEmits<{
  navigate: [path: string];
}>();

const groupOrder = ['dashboard', 'runtime', 'diagnostics', 'operations'] as const;
const groupLabels: Record<typeof groupOrder[number], string> = {
  dashboard: '概览',
  runtime: '运行',
  diagnostics: '诊断',
  operations: '运维'
};

const groupedItems = computed(() => groupOrder.map(groupKey => ({
  key: groupKey,
  label: groupLabels[groupKey],
  items: props.items.filter(item => item.group === groupKey)
})).filter(group => group.items.length > 0));

function navigate(path: string): void {
  emit('navigate', path);
}
</script>
