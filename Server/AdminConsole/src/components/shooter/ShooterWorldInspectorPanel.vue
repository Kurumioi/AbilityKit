<template>
  <section class="card span-12 shooter-world-panel">
    <div class="card-head">
      <div><p class="section-kicker">Shooter Svelto</p><h2>世界对象检查器</h2></div>
      <span class="badge">{{ admin.shooterWorldDiagnostics.value?.entityCount || 0 }} 个实体</span>
    </div>

    <div class="row shooter-world-toolbar">
      <div>
        <label>选择 Shooter 房间</label>
        <select v-model="admin.selectedShooterInspectorRoomId.value">
          <option value="">使用当前房间</option>
          <option v-for="room in admin.shooterInspectorRooms.value" :key="room.roomId" :value="room.roomId">{{ room.title || room.roomId }} / {{ room.playerCount }}人</option>
        </select>
      </div>
      <div>
        <label>当前 Battle</label>
        <input :value="admin.shooterWorldDiagnostics.value?.battleId || admin.runtimeState.value?.battleId || '未启动'" readonly />
      </div>
      <div>
        <label>实体搜索</label>
        <input v-model.trim="entitySearchText" placeholder="EntityId / Kind / Component / Field" />
      </div>
    </div>
    <div class="actions action-bar shooter-inspector-actions">
      <button class="secondary" :disabled="admin.busy.value || !canRefresh" @click="refreshWorldSnapshot">刷新世界状态</button>
      <button :disabled="admin.busy.value || !admin.selectedShooterInspectorRoomId.value" @click="admin.selectRoom(admin.selectedShooterInspectorRoomId.value)">绑定为当前房间</button>
      <label class="auto-refresh-toggle">
        <input v-model="autoRefreshEnabled" type="checkbox" />
        <span>定时刷新 2s</span>
      </label>
      <span class="refresh-hint">{{ refreshModeLabel }}</span>
    </div>

    <div v-if="diagnostics" class="shooter-world-summary ops-status cluster-summary">
      <div><span>Context</span><strong>{{ diagnostics.worldType }}</strong></div>
      <div><span>World ID</span><strong>{{ diagnostics.worldId }}</strong></div>
      <div><span>Frame</span><strong>{{ diagnostics.frame }}</strong></div>
      <div><span>State Hash</span><strong>{{ diagnostics.stateHash }}</strong></div>
      <div><span>Entities</span><strong>{{ filteredEntities.length }} / {{ diagnostics.entityCount }}</strong></div>
      <div><span>Last Pull</span><strong>{{ lastRefreshLabel }}</strong></div>
    </div>

    <div v-if="diagnostics" class="shooter-world-filterbar">
      <button type="button" :class="{ active: aliveFilter === 'all' }" @click="aliveFilter = 'all'">全部 {{ diagnostics.entities.length }}</button>
      <button type="button" :class="{ active: aliveFilter === 'alive' }" @click="aliveFilter = 'alive'">存活 {{ aliveCount }}</button>
      <button type="button" :class="{ active: aliveFilter === 'inactive' }" @click="aliveFilter = 'inactive'">非活跃 {{ inactiveCount }}</button>
      <select v-model="entityKindFilter" aria-label="实体类型过滤">
        <option value="all">全部类型</option>
        <option v-for="kind in entityKinds" :key="kind" :value="kind">{{ kind }}</option>
      </select>
    </div>

    <div v-if="diagnostics && diagnostics.warnings.length > 0" class="shooter-world-warnings">
      <span v-for="warning in diagnostics.warnings" :key="warning">{{ warning }}</span>
    </div>

    <div v-if="diagnostics" class="shooter-world-layout entitas-inspector">
      <ShooterWorldEntityTree
        :groups="worldGroups"
        :selected-entity-key="selectedEntity?.key"
        :entity-count="filteredEntities.length"
        @select-entity="admin.selectShooterWorldEntity" />
      <ShooterWorldComponentGraph :entity="selectedEntity" />
    </div>

    <ShooterWorldChunkStrip v-if="diagnostics" :chunks="diagnostics.componentChunks" />
    <p v-else class="muted helper-text">选择已进入战斗的 Shooter 房间后刷新，可查看当前 Svelto 世界对象、组件和字段值。</p>
  </section>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import './shooterWorld.css';
import ShooterWorldChunkStrip from './ShooterWorldChunkStrip.vue';
import ShooterWorldComponentGraph from './ShooterWorldComponentGraph.vue';
import ShooterWorldEntityTree from './ShooterWorldEntityTree.vue';
import { buildShooterWorldGroups } from '../../composables/useShooterWorldProjection';
import { useAdminConsoleStore } from '../../stores/adminConsoleStore';
import type { ShooterWorldDiagnostics, ShooterWorldEntityDiagnostics } from '../../types';

const admin = useAdminConsoleStore();
const entitySearchText = ref('');
const entityKindFilter = ref('all');
const aliveFilter = ref<'all' | 'alive' | 'inactive'>('all');
const autoRefreshEnabled = ref(false);
const lastRefreshedAt = ref<number | null>(null);
let refreshTimer: number | undefined;

const diagnostics = computed(() => admin.shooterWorldDiagnostics.value);
const aliveCount = computed(() => diagnostics.value?.entities.filter(entity => entity.alive).length || 0);
const inactiveCount = computed(() => diagnostics.value?.entities.filter(entity => !entity.alive).length || 0);
const entityKinds = computed(() => [...new Set((diagnostics.value?.entities || []).map(entity => entity.entityKind))].sort((a, b) => a.localeCompare(b)));
const filteredEntities = computed(() => (diagnostics.value?.entities || []).filter(matchesFilters));
const filteredDiagnostics = computed<ShooterWorldDiagnostics | null>(() => diagnostics.value ? { ...diagnostics.value, entities: filteredEntities.value, entityCount: filteredEntities.value.length } : null);
const worldGroups = computed(() => buildShooterWorldGroups(filteredDiagnostics.value));
const canRefresh = computed(() => Boolean(admin.selectedShooterInspectorRoomId.value || admin.effectiveRoomId.value));
const lastRefreshLabel = computed(() => lastRefreshedAt.value ? new Date(lastRefreshedAt.value).toLocaleTimeString() : '未刷新');
const refreshModeLabel = computed(() => autoRefreshEnabled.value ? `轮询中 / ${lastRefreshLabel.value}` : `手动快照 / ${lastRefreshLabel.value}`);
const selectedEntity = computed(() => {
  const current = admin.selectedShooterWorldEntity.value;
  if (current && filteredEntities.value.some(entity => entity.key === current.key)) return current;
  return filteredEntities.value[0] || null;
});

watch(autoRefreshEnabled, enabled => {
  if (enabled) {
    refreshWorldSnapshot();
    startRefreshTimer();
    return;
  }

  stopRefreshTimer();
});

watch(() => admin.selectedShooterInspectorRoomId.value || admin.effectiveRoomId.value, () => {
  if (autoRefreshEnabled.value) refreshWorldSnapshot();
});

onBeforeUnmount(stopRefreshTimer);

async function refreshWorldSnapshot(): Promise<void> {
  if (!canRefresh.value) return;
  await admin.refreshShooterWorldDiagnostics();
  lastRefreshedAt.value = Date.now();
}

function startRefreshTimer(): void {
  stopRefreshTimer();
  refreshTimer = window.setInterval(() => {
    if (!admin.busy.value && canRefresh.value) refreshWorldSnapshot();
  }, 2000);
}

function stopRefreshTimer(): void {
  if (refreshTimer === undefined) return;
  window.clearInterval(refreshTimer);
  refreshTimer = undefined;
}

function matchesFilters(entity: ShooterWorldEntityDiagnostics): boolean {
  if (aliveFilter.value === 'alive' && !entity.alive) return false;
  if (aliveFilter.value === 'inactive' && entity.alive) return false;
  if (entityKindFilter.value !== 'all' && entity.entityKind !== entityKindFilter.value) return false;
  const keyword = entitySearchText.value.toLowerCase();
  if (!keyword) return true;
  return [
    entity.key,
    entity.label,
    entity.group,
    entity.entityKind,
    String(entity.entityId),
    ...entity.components.flatMap(component => [
      component.name,
      component.componentKind,
      ...Object.entries(component.fields || {}).flatMap(([key, value]) => [key, String(value)])
    ])
  ].some(value => value.toLowerCase().includes(keyword));
}
</script>
