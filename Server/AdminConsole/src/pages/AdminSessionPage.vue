<template>
  <section v-if="routeKey === 'session'" id="session" class="card span-6">
    <div class="card-head"><div><p class="section-kicker">访问入口</p><h2>会话</h2></div><span class="badge">认证</span></div>
    <label>账号 ID</label>
    <input v-model="admin.account.accountId" placeholder="admin 或测试账号" />
    <div class="row">
      <div><label>过期秒数</label><input v-model.number="admin.account.expireSeconds" type="number" /></div>
      <div><label>踢掉已有会话</label><select v-model="admin.account.kickExisting"><option :value="true">是</option><option :value="false">否</option></select></div>
    </div>
    <label>会话令牌</label>
    <input v-model="admin.sessionToken.value" placeholder="登录后自动保存" />
    <div class="actions action-bar">
      <button :disabled="admin.busy.value" @click="admin.guestLogin">游客登录</button>
      <button :disabled="admin.busy.value || !admin.account.accountId" @click="admin.accountLogin">账号登录</button>
      <button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.validateSession">校验</button>
      <button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.logout">登出</button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useAdminConsoleStore } from '../stores/adminConsoleStore';
import type { AdminRouteKey } from '../navigation/adminNavigation';

defineProps<{ routeKey: AdminRouteKey }>();

const admin = useAdminConsoleStore();
</script>
