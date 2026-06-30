<template>
  <header class="topbar">
    <div>
      <p class="eyebrow">AbilityKit Orleans 管理后台</p>
      <h1>服务器管理控制台</h1>
      <p class="sub">正式后台布局框架：侧边导航、顶部操作栏、业务工作区与可扩展卡片分区。</p>
      <p class="muted helper-text">当前模块：{{ title }} / {{ description }}</p>
    </div>
    <div class="topbar-actions">
      <button class="secondary" :disabled="busy" @click="emit('refresh')">刷新仪表盘</button>
      <a class="link-button" href="/debug/index.html">调试控制台</a>
    </div>
  </header>
</template>

<script setup lang="ts">
defineProps<{
  title: string;
  description: string;
  busy: boolean;
}>();

const emit = defineEmits<{
  refresh: [];
}>();
</script>
