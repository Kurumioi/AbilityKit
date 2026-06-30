<template>
  <section class="workspace-summary">
    <article class="summary-card emphasis">
      <span>当前页签</span>
      <strong>{{ currentRouteLabel }}</strong>
      <small>{{ currentRouteDescription }}</small>
    </article>
    <article class="summary-card">
      <span>会话</span>
      <strong>{{ admin.accountLabel.value }}</strong>
      <small>{{ admin.hasSession.value ? '已登录' : '未登录' }}</small>
    </article>
    <article class="summary-card">
      <span>房间</span>
      <strong>{{ admin.effectiveRoomId.value || '无当前房间' }}</strong>
      <small>{{ admin.rooms.value.length }} 个房间 / {{ admin.selectedRoomType.value }}</small>
    </article>
    <article class="summary-card">
      <span>技能诊断</span>
      <strong>{{ admin.skillSummary.value?.diagnosticsStatus || '待刷新' }}</strong>
      <small>{{ admin.skillEvents.value?.events?.length || 0 }} 条运行态事件</small>
    </article>
    <article class="summary-card">
      <span>验收产物</span>
      <strong>{{ admin.acceptanceBatch.value?.cases?.length || 0 }} 个用例</strong>
      <small>{{ admin.acceptanceBatch.value?.artifactDirectory || admin.acceptance.artifactDirectory }}</small>
    </article>
    <article class="summary-card">
      <span>运维 / 集群</span>
      <strong>{{ admin.serverModeLabel.value }}</strong>
      <small>{{ admin.clusterLabel.value }}</small>
    </article>
  </section>
</template>

<script setup lang="ts">
import { useAdminConsoleStore } from '../../stores/adminConsoleStore';

defineProps<{
  currentRouteLabel: string;
  currentRouteDescription: string;
}>();

const admin = useAdminConsoleStore();
</script>
