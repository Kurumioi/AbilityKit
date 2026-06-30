<template>
  <section class="card span-12 operations-center">
    <div class="card-head">
      <div><p class="section-kicker">操作中心</p><h2>可用性工作台</h2></div>
      <span class="badge" :class="admin.apiFailureCount.value > 0 ? 'danger' : ''">{{ admin.apiFailureCount.value }} 个失败请求</span>
    </div>

    <div class="operations-grid">
      <article class="operation-tile">
        <span>会话</span>
        <strong>{{ admin.accountLabel.value }}</strong>
        <small>{{ admin.hasSession.value ? '可执行后台操作' : '需要先登录' }}</small>
        <div class="actions compact-actions">
          <button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.validateSession">校验</button>
          <button :disabled="admin.busy.value" @click="admin.guestLogin">游客登录</button>
        </div>
      </article>

      <article class="operation-tile">
        <span>当前房间</span>
        <strong>{{ admin.effectiveRoomId.value || '未选择房间' }}</strong>
        <small>{{ admin.snapshot.value?.canStart ? '满足开战条件' : '等待准备或成员配置' }}</small>
        <div class="actions compact-actions">
          <button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.restoreCurrentRoom">恢复</button>
          <button :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.setRoomReady(true)">设为准备</button>
          <button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.setRoomReady(false)">取消准备</button>
        </div>
      </article>

      <article class="operation-tile">
        <span>Shooter 沙盒</span>
        <strong>{{ admin.sandboxLabel.value }}</strong>
        <small>{{ admin.sandbox.state?.roomId || admin.sandbox.sandboxId || 'default' }}</small>
        <div class="actions compact-actions">
          <button class="secondary" :disabled="admin.busy.value" @click="admin.refreshShooterSandboxState">刷新状态</button>
          <button class="success" :disabled="admin.busy.value" @click="admin.startShooterSandbox">启动</button>
          <button class="warn" :disabled="admin.busy.value" @click="admin.stopShooterSandbox">停止</button>
        </div>
      </article>

      <article class="operation-tile">
        <span>服务器</span>
        <strong>{{ admin.serverModeLabel.value }}</strong>
        <small>{{ admin.serverStatus.value?.lastOperationReason || '暂无最近运维原因' }}</small>
        <div class="actions compact-actions">
          <button class="secondary" :disabled="admin.busy.value" @click="admin.refreshServerStatus">刷新</button>
          <button :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.setMaintenanceMode(!admin.serverStatus.value?.maintenanceMode)">{{ admin.serverStatus.value?.maintenanceMode ? '关闭维护' : '开启维护' }}</button>
        </div>
      </article>
    </div>

    <div class="operation-log">
      <div class="trace-section-head"><h3>最近接口请求</h3><span class="badge">{{ admin.apiCallLog.value.length }} 条</span></div>
      <article v-for="item in admin.apiCallLog.value.slice(0, 8)" :key="item.id" class="api-log-item" :class="{ failed: !item.ok }">
        <span class="badge">{{ item.method }}</span>
        <div>
          <strong>{{ item.url }}</strong>
          <small>{{ item.occurredAt }} / {{ item.status || '-' }} {{ item.statusText }} / {{ item.durationMs }}ms</small>
          <p v-if="item.summary">{{ item.summary }}</p>
        </div>
      </article>
      <p v-if="admin.apiCallLog.value.length === 0" class="muted">尚无接口请求记录。</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useAdminConsoleStore } from '../../stores/adminConsoleStore';

const admin = useAdminConsoleStore();
</script>
