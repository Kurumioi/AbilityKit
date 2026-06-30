<template>
  <section v-if="routeKey === 'cluster'" id="cluster" class="card span-12 cluster-panel">
    <div class="card-head"><div><p class="section-kicker">集群状态</p><h2>集群诊断</h2></div><span class="badge">{{ admin.clusterDiagnostics.value?.clientStatus || '等待数据' }}</span></div>
    <div v-if="admin.clusterDiagnostics.value" class="ops-status cluster-summary">
      <div><span>集群 ID</span><strong>{{ admin.clusterDiagnostics.value.clusterId }}</strong></div>
      <div><span>服务 ID</span><strong>{{ admin.clusterDiagnostics.value.serviceId }}</strong></div>
      <div><span>Silo 端口</span><strong>{{ admin.clusterDiagnostics.value.siloPort ?? '默认' }}</strong></div>
      <div><span>网关端口</span><strong>{{ admin.clusterDiagnostics.value.orleansGatewayPort ?? '默认' }}</strong></div>
      <div><span>客户端</span><strong>{{ admin.clusterDiagnostics.value.clientConnected ? '已连接' : '未知' }}</strong></div>
      <div><span>网关进程</span><strong>{{ admin.clusterDiagnostics.value.gatewayProcess.processName }} #{{ admin.clusterDiagnostics.value.gatewayProcess.processId }}</strong></div>
    </div>
    <p v-else class="muted">等待集群诊断数据。</p>
    <div class="diagnostic-grid">
      <div>
        <h3>节点探针</h3>
        <article v-for="node in admin.clusterDiagnostics.value?.nodes || []" :key="node.nodeId" class="probe-item">
          <div><strong>{{ node.nodeId }}</strong><small>{{ node.role }} / {{ node.endpoint }}</small></div>
          <span class="badge">{{ node.status }}</span>
          <p>{{ node.diagnostics }}</p>
        </article>
      </div>
      <div>
        <h3>运行态指标</h3>
        <ul class="diagnostic-list"><li v-for="metric in admin.clusterDiagnostics.value?.runtimeMetrics || []" :key="metric">{{ metric }}</li></ul>
        <h3>告警</h3>
        <ul class="diagnostic-list warnings">
          <li v-for="warning in admin.clusterDiagnostics.value?.warnings || []" :key="warning">{{ warning }}</li>
          <li v-if="!admin.clusterDiagnostics.value?.warnings?.length">暂无配置告警。</li>
        </ul>
      </div>
    </div>
    <div class="actions action-bar"><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshClusterDiagnostics">刷新集群诊断</button></div>
  </section>
</template>

<script setup lang="ts">
import { useAdminConsoleStore } from '../stores/adminConsoleStore';
import type { AdminRouteKey } from '../navigation/adminNavigation';

defineProps<{ routeKey: AdminRouteKey }>();

const admin = useAdminConsoleStore();
</script>
