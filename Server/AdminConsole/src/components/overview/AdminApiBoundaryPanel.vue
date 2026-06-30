<template>
  <section class="card span-12 boundary-panel">
    <div class="card-head"><div><p class="section-kicker">接口边界</p><h2>后台 API 职责边界</h2></div><span class="badge">聚合门面</span></div>
    <div class="ops-status boundary-grid">
      <div v-for="boundary in boundaries" :key="boundary.prefix">
        <span>{{ boundary.prefix }}</span>
        <strong>{{ boundary.responsibility }}</strong>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { adminApiBoundaries } from '../../services/adminApiBoundaries';

const boundaries = computed(() => Object.values(adminApiBoundaries));
</script>
