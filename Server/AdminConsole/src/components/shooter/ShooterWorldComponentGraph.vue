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

    <div v-if="entity" class="component-flow">
      <article v-for="component in entity.components" :key="`${entity.key}-${component.name}`" class="component-node-card">
        <div class="component-node-head">
          <strong>{{ component.name }}</strong>
          <span>{{ component.componentKind }}</span>
        </div>
        <div class="component-chip-grid">
          <span v-for="entry in fieldEntries(component.fields)" :key="entry.key" class="field-chip"><em>{{ entry.key }}</em>{{ entry.value }}</span>
        </div>
      </article>
    </div>
    <p v-if="!entity" class="muted">请选择一个世界对象。</p>
  </div>
</template>

<script setup lang="ts">
import { fieldEntries } from '../../composables/useShooterWorldProjection';
import type { ShooterWorldEntityDiagnostics } from '../../types';

defineProps<{
  entity: ShooterWorldEntityDiagnostics | null;
}>();
</script>
