<template>
  <section v-if="routeKey === 'battle'" id="battle" class="card span-6">
    <div class="card-head"><div><p class="section-kicker">战斗流程</p><h2>启动战斗</h2></div><span class="badge">启动</span></div>
    <div class="row"><div><label>玩法 ID</label><input v-model.number="admin.battle.gameplayId" type="number" /></div><div><label>规则集 ID</label><input v-model.number="admin.battle.ruleSetId" type="number" /></div></div>
    <label>世界类型</label><input v-model="admin.battle.worldType" />
    <label>同步模板 ID</label>
    <select v-if="admin.selectedGameplay.value && admin.selectedGameplay.value.supportedSyncTemplateIds?.length" v-model="admin.battle.syncTemplateId"><option v-for="syncTemplateId in admin.selectedGameplay.value.supportedSyncTemplateIds" :key="syncTemplateId" :value="syncTemplateId">{{ syncTemplateId }}</option></select>
    <input v-else v-model="admin.battle.syncTemplateId" />
    <div class="actions action-bar"><button class="success" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.startBattle">启动</button><button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value || admin.selectedRoomType.value !== 'shooter'" @click="admin.startShooterRoomQuick">Shooter 快速开战</button></div>
  </section>
</template>

<script setup lang="ts">
import { useAdminConsoleStore } from '../stores/adminConsoleStore';
import type { AdminRouteKey } from '../navigation/adminNavigation';

defineProps<{ routeKey: AdminRouteKey }>();

const admin = useAdminConsoleStore();
</script>
