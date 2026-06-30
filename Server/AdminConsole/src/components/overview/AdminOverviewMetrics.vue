<template>
  <section id="overview" class="metrics dashboard-strip">
    <article class="metric"><span>账号</span><strong>{{ admin.accountLabel.value }}</strong><small>会话身份</small></article>
    <article class="metric"><span>玩法数</span><strong>{{ admin.gameplays.value.length }}</strong><small>玩法目录</small></article>
    <article class="metric"><span>房间数</span><strong>{{ admin.rooms.value.length }}</strong><small>房间目录</small></article>
    <article class="metric"><span>沙盒</span><strong>{{ admin.sandboxLabel.value }}</strong><small>Shooter 运行态</small></article>
    <article class="metric"><span>服务器</span><strong>{{ admin.serverModeLabel.value }}</strong><small>运维模式</small></article>
    <article class="metric"><span>集群</span><strong>{{ admin.clusterLabel.value }}</strong><small>集群 / 服务</small></article>
  </section>
</template>

<script setup lang="ts">
import { useAdminConsoleStore } from '../../stores/adminConsoleStore';

const admin = useAdminConsoleStore();
</script>
