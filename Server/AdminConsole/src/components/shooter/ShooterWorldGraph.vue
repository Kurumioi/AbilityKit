<template>
  <div class="shooter-visual-stage">
    <div class="trace-section-head"><h3>Svelto 世界视图</h3><span class="badge">World Graph</span></div>
    <div class="world-canvas">
      <article v-for="group in groups" :key="group.key" class="world-lane">
        <div class="world-lane-head"><strong>{{ group.label }}</strong><small>{{ group.entities.length }} entities</small></div>
        <div class="world-lane-body">
          <button
            v-for="entity in group.entities"
            :key="entity.key"
            class="world-entity-node"
            :class="[entity.entityKind.toLowerCase(), { active: selectedEntityKey === entity.key, dead: !entity.alive }]"
            @click="$emit('selectEntity', entity.key)">
            <strong>{{ entity.entityKind }} #{{ entity.entityId }}</strong>
            <span>{{ entity.components.length }} components</span>
          </button>
        </div>
      </article>
    </div>

    <div class="component-map">
      <article v-for="item in componentCounts" :key="item.kind" class="component-map-card">
        <span>{{ item.kind }}</span>
        <strong>{{ item.count }}</strong>
        <small>{{ item.entities }} entities</small>
      </article>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ShooterComponentKindCountProjection, ShooterWorldGroupProjection } from '../../composables/useShooterWorldProjection';

defineProps<{
  groups: ShooterWorldGroupProjection[];
  componentCounts: ShooterComponentKindCountProjection[];
  selectedEntityKey?: string;
}>();

defineEmits<{
  selectEntity: [key: string];
}>();
</script>
