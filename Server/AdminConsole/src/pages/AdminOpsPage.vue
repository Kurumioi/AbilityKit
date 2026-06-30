<template>
  <section v-if="routeKey === 'ops'" id="ops" class="card span-8 featured-card">
    <div class="card-head"><div><p class="section-kicker">运维操作</p><h2>服务器运维</h2></div><span class="badge danger" v-if="admin.serverStatus.value?.restartRequested">已请求重启</span><span v-else class="badge">运维</span></div>
    <div v-if="admin.serverStatus.value" class="ops-status">
      <div><span>环境</span><strong>{{ admin.serverStatus.value.environmentName }}</strong></div>
      <div><span>进程</span><strong>{{ admin.serverStatus.value.processName }} #{{ admin.serverStatus.value.processId }}</strong></div>
      <div><span>机器</span><strong>{{ admin.serverStatus.value.machineName }}</strong></div>
      <div><span>运行</span><strong>{{ admin.formatDuration(admin.serverStatus.value.uptimeSeconds) }}</strong></div>
      <div><span>内存</span><strong>{{ admin.formatBytes(admin.serverStatus.value.workingSetBytes) }}</strong></div>
      <div><span>GC</span><strong>{{ admin.formatBytes(admin.serverStatus.value.gcTotalMemoryBytes) }}</strong></div>
      <div><span>线程</span><strong>{{ admin.serverStatus.value.threadCount }}</strong></div>
      <div><span>状态</span><strong>{{ admin.serverModeLabel.value }}</strong></div>
    </div>
    <p v-else class="muted">等待服务器状态。</p>
    <label>操作原因</label>
    <input v-model="admin.serverOperation.reason" placeholder="例如：版本发布、压测维护" />
    <div class="actions action-bar">
      <button class="secondary" :disabled="admin.busy.value" @click="admin.refreshServerStatus">刷新状态</button>
      <button :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.setMaintenanceMode(!admin.serverStatus.value?.maintenanceMode)">{{ admin.serverStatus.value?.maintenanceMode ? '关闭维护' : '开启维护' }}</button>
      <button :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.setDrainMode(!admin.serverStatus.value?.drainMode)">{{ admin.serverStatus.value?.drainMode ? '关闭排空' : '开启排空' }}</button>
      <button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.requestRestart">请求重启</button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useAdminConsoleStore } from '../stores/adminConsoleStore';
import type { AdminRouteKey } from '../navigation/adminNavigation';

defineProps<{ routeKey: AdminRouteKey }>();

const admin = useAdminConsoleStore();
</script>
