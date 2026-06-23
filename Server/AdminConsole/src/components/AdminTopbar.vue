<template>
  <header class="topbar">
    <div>
      <p class="eyebrow">AbilityKit Orleans</p>
      <h1>Server Admin Console</h1>
      <p class="sub">正式后台布局框架：侧边导航、顶部操作栏、业务工作区与可扩展卡片分区。</p>
      <p class="muted helper-text">当前模块：{{ title }} / {{ description }}</p>
    </div>
    <div class="topbar-actions">
      <button class="secondary" :disabled="busy" @click="emit('refresh')">刷新 Dashboard</button>
      <a class="link-button" href="/debug/index.html">Debug Console</a>
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
