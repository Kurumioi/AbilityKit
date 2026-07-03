<template>
  <div class="shooter-component-detail enhanced">
    <div class="trace-section-head"><h3>组件结构</h3><span class="badge">Component Graph</span></div>
    <div v-if="entity" class="selected-entity-hero">
      <div>
        <span>{{ entity.entityKind }}</span>
        <strong>{{ entity.label }}</strong>
        <small>{{ entity.group }} / {{ entity.alive ? 'alive' : 'inactive' }}</small>
      </div>
      <div class="entity-pulse" :class="entity.entityKind.toLowerCase()">{{ entity.entityId }}</div>
    </div>

    <div v-if="entity" class="selected-entity-stats">
      <div><span>Entity Key</span><strong>{{ entity.key }}</strong></div>
      <div><span>组件数</span><strong>{{ entity.components.length }}</strong></div>
      <div><span>字段数</span><strong>{{ fieldCount }}</strong></div>
      <div><span>状态</span><strong>{{ entity.alive ? 'Alive' : 'Inactive' }}</strong></div>
    </div>

    <div v-if="entity" class="component-flow">
      <article v-for="component in entity.components" :key="`${entity.key}-${component.name}`" class="component-node-card">
        <div class="component-node-head">
          <strong>{{ component.name }}</strong>
          <span>{{ component.componentKind }}</span>
        </div>
        <div class="component-field-table">
          <div v-for="entry in fieldEntries(component.fields)" :key="entry.key" class="component-field-row">
            <span>{{ entry.key }}</span>
            <code>{{ entry.value }}</code>
          </div>
          <p v-if="fieldEntries(component.fields).length === 0" class="muted">该组件没有导出的字段。</p>
        </div>
      </article>
    </div>
    <p v-if="!entity" class="muted">请选择一个世界对象。</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { fieldEntries } from '../../composables/useShooterWorldProjection';
import type { ShooterWorldEntityDiagnostics } from '../../types';

const props = defineProps<{
  entity: ShooterWorldEntityDiagnostics | null;
}>();

const fieldCount = computed(() => props.entity?.components.reduce((total, component) => total + fieldEntries(component.fields).length, 0) || 0);
</script>
