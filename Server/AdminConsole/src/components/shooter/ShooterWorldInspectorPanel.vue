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
    </div>
    <div class="actions action-bar">
      <button class="secondary" :disabled="admin.busy.value || !(admin.selectedShooterInspectorRoomId.value || admin.effectiveRoomId.value)" @click="admin.refreshShooterWorldDiagnostics()">刷新世界状态</button>
      <button :disabled="admin.busy.value || !admin.selectedShooterInspectorRoomId.value" @click="admin.selectRoom(admin.selectedShooterInspectorRoomId.value)">绑定为当前房间</button>
    </div>

    <div v-if="admin.shooterWorldDiagnostics.value" class="shooter-world-summary ops-status cluster-summary">
      <div><span>世界类型</span><strong>{{ admin.shooterWorldDiagnostics.value.worldType }}</strong></div>
      <div><span>世界 ID</span><strong>{{ admin.shooterWorldDiagnostics.value.worldId }}</strong></div>
      <div><span>帧号</span><strong>{{ admin.shooterWorldDiagnostics.value.frame }}</strong></div>
      <div><span>状态 Hash</span><strong>{{ admin.shooterWorldDiagnostics.value.stateHash }}</strong></div>
      <div><span>实体数</span><strong>{{ admin.shooterWorldDiagnostics.value.entityCount }}</strong></div>
      <div><span>组件块</span><strong>{{ admin.shooterWorldDiagnostics.value.componentChunks.length }}</strong></div>
    </div>

    <ShooterWorldGraph
      v-if="admin.shooterWorldDiagnostics.value"
      :groups="worldGroups"
      :component-counts="componentKindCounts"
      :selected-entity-key="admin.selectedShooterWorldEntity.value?.key"
      @select-entity="admin.selectShooterWorldEntity" />

    <div v-if="admin.shooterWorldDiagnostics.value" class="shooter-world-layout enhanced">
      <ShooterWorldEntityTree
        :groups="worldGroups"
        :selected-entity-key="admin.selectedShooterWorldEntity.value?.key"
        :entity-count="admin.shooterWorldDiagnostics.value.entities.length"
        @select-entity="admin.selectShooterWorldEntity" />
      <ShooterWorldComponentGraph :entity="admin.selectedShooterWorldEntity.value" />
    </div>

    <ShooterWorldChunkStrip v-if="admin.shooterWorldDiagnostics.value" :chunks="admin.shooterWorldDiagnostics.value.componentChunks" />
    <p v-else class="muted helper-text">选择已进入战斗的 Shooter 房间后刷新，可查看当前 Svelto 世界对象、组件和字段值。</p>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import './shooterWorld.css';
import ShooterWorldChunkStrip from './ShooterWorldChunkStrip.vue';
import ShooterWorldComponentGraph from './ShooterWorldComponentGraph.vue';
import ShooterWorldEntityTree from './ShooterWorldEntityTree.vue';
import ShooterWorldGraph from './ShooterWorldGraph.vue';
import { buildShooterComponentKindCounts, buildShooterWorldGroups } from '../../composables/useShooterWorldProjection';
import { useAdminConsoleStore } from '../../stores/adminConsoleStore';

const admin = useAdminConsoleStore();

const worldGroups = computed(() => buildShooterWorldGroups(admin.shooterWorldDiagnostics.value));
const componentKindCounts = computed(() => buildShooterComponentKindCounts(admin.shooterWorldDiagnostics.value));
</script>
