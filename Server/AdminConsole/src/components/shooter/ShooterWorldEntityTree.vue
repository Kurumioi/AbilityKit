<template>
  <div class="shooter-entity-tree">
    <div class="trace-section-head"><h3>实体列表</h3><span class="badge">{{ entityCount }} Entities</span></div>
    <details v-for="group in groups" :key="group.key" class="entity-tree-group" open>
      <summary><span>{{ group.label }}</span><strong>{{ group.entities.length }}</strong></summary>
      <article
        v-for="entity in group.entities"
        :key="entity.key"
        class="tree-entity-row"
        :class="{ active: selectedEntityKey === entity.key, dead: !entity.alive }"
        @click="$emit('selectEntity', entity.key)">
        <div class="tree-branch"></div>
        <div>
          <strong>{{ entity.label }}</strong>
          <small>{{ componentSummary(entity) }}</small>
          <small class="entity-row-meta">{{ entity.group }} / {{ entity.components.length }} components</small>
        </div>
        <span class="badge">{{ entity.alive ? 'live' : 'off' }}</span>
      </article>
    </details>
    <p v-if="entityCount === 0" class="muted">当前快照没有实体。</p>
  </div>
</template>

<script setup lang="ts">
import { componentSummary, type ShooterWorldGroupProjection } from '../../composables/useShooterWorldProjection';

defineProps<{
  groups: ShooterWorldGroupProjection[];
  selectedEntityKey?: string;
  entityCount: number;
}>();

defineEmits<{
  selectEntity: [key: string];
}>();
</script>
