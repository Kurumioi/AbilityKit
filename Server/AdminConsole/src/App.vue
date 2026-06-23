<template>
  <div class="admin-layout shell">
    <AdminSidebar
      :items="adminNavigationItems"
      :active-key="routeKey"
      :server-mode-label="admin.serverModeLabel.value"
      :environment-name="admin.serverStatus.value?.environmentName"
      :maintenance-mode="admin.serverStatus.value?.maintenanceMode"
      @navigate="navigate" />

    <div class="workspace">
      <AdminTopbar :title="currentRoute.label" :description="currentRoute.description" :busy="admin.busy.value" @refresh="admin.refreshDashboard" />

      <section v-if="routeKey === 'overview'" id="overview" class="metrics dashboard-strip">
        <article class="metric"><span>账号</span><strong>{{ admin.accountLabel.value }}</strong><small>Session Identity</small></article>
        <article class="metric"><span>玩法数</span><strong>{{ admin.gameplays.value.length }}</strong><small>Gameplay Catalog</small></article>
        <article class="metric"><span>房间数</span><strong>{{ admin.rooms.value.length }}</strong><small>Room Directory</small></article>
        <article class="metric"><span>Sandbox</span><strong>{{ admin.sandboxLabel.value }}</strong><small>Shooter Runtime</small></article>
        <article class="metric"><span>服务器</span><strong>{{ admin.serverModeLabel.value }}</strong><small>Ops Mode</small></article>
        <article class="metric"><span>集群</span><strong>{{ admin.clusterLabel.value }}</strong><small>Cluster / Service</small></article>
      </section>

      <main class="content-grid grid">
        <template v-if="routeKey === 'overview'">
          <AdminApiBoundaryPanel />
        </template>

        <section v-if="routeKey === 'session'" id="session" class="card span-6">
          <div class="card-head"><div><p class="section-kicker">Access</p><h2>会话</h2></div><span class="badge">Auth</span></div>
          <label>AccountId</label>
          <input v-model="admin.account.accountId" placeholder="admin 或测试账号" />
          <div class="row">
            <div><label>ExpireSeconds</label><input v-model.number="admin.account.expireSeconds" type="number" /></div>
            <div><label>KickExisting</label><select v-model="admin.account.kickExisting"><option :value="true">true</option><option :value="false">false</option></select></div>
          </div>
          <label>SessionToken</label>
          <input v-model="admin.sessionToken.value" placeholder="登录后自动保存" />
          <div class="actions action-bar">
            <button :disabled="admin.busy.value" @click="admin.guestLogin">游客登录</button>
            <button :disabled="admin.busy.value || !admin.account.accountId" @click="admin.accountLogin">账号登录</button>
            <button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.validateSession">校验</button>
            <button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.logout">登出</button>
          </div>
        </section>

        <section v-if="routeKey === 'ops'" id="ops" class="card span-8 featured-card">
          <div class="card-head"><div><p class="section-kicker">Operations</p><h2>服务器运维</h2></div><span class="badge danger" v-if="admin.serverStatus.value?.restartRequested">Restart Requested</span><span v-else class="badge">Ops</span></div>
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
            <button :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.setDrainMode(!admin.serverStatus.value?.drainMode)">{{ admin.serverStatus.value?.drainMode ? '关闭 Drain' : '开启 Drain' }}</button>
            <button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.requestRestart">请求重启</button>
          </div>
        </section>

        <section v-if="routeKey === 'cluster'" id="cluster" class="card span-12 cluster-panel">
          <div class="card-head"><div><p class="section-kicker">Cluster</p><h2>集群诊断</h2></div><span class="badge">{{ admin.clusterDiagnostics.value?.clientStatus || 'Pending' }}</span></div>
          <div v-if="admin.clusterDiagnostics.value" class="ops-status cluster-summary">
            <div><span>ClusterId</span><strong>{{ admin.clusterDiagnostics.value.clusterId }}</strong></div>
            <div><span>ServiceId</span><strong>{{ admin.clusterDiagnostics.value.serviceId }}</strong></div>
            <div><span>SiloPort</span><strong>{{ admin.clusterDiagnostics.value.siloPort ?? 'default' }}</strong></div>
            <div><span>GatewayPort</span><strong>{{ admin.clusterDiagnostics.value.orleansGatewayPort ?? 'default' }}</strong></div>
            <div><span>Client</span><strong>{{ admin.clusterDiagnostics.value.clientConnected ? 'Connected' : 'Unknown' }}</strong></div>
            <div><span>Gateway Process</span><strong>{{ admin.clusterDiagnostics.value.gatewayProcess.processName }} #{{ admin.clusterDiagnostics.value.gatewayProcess.processId }}</strong></div>
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

        <template v-if="routeKey === 'rooms'">
          <section class="card span-4">
            <div class="card-head"><div><p class="section-kicker">Runtime</p><h2>环境</h2></div><span class="badge">Scope</span></div>
            <div class="row"><div><label>Region</label><input v-model="admin.region.value" /></div><div><label>ServerId</label><input v-model="admin.serverId.value" /></div></div>
            <label>玩法</label>
            <select v-model="admin.selectedRoomType.value" @change="admin.applyGameplayDefaults">
              <option v-for="gameplay in admin.gameplays.value" :key="gameplay.roomType" :value="gameplay.roomType">{{ gameplay.displayName }} / {{ gameplay.roomType }}</option>
            </select>
            <p v-if="admin.selectedGameplay.value" class="muted helper-text">默认 {{ admin.selectedGameplay.value.defaultMaxPlayers }} 人，{{ admin.selectedGameplay.value.defaultWorldType }}，{{ admin.selectedGameplay.value.defaultSyncTemplateId }}</p>
          </section>
          <section class="card span-4">
            <div class="card-head"><div><p class="section-kicker">Simulation</p><h2>Shooter Sandbox</h2></div><span class="badge">Sandbox</span></div>
            <div class="row"><div><label>SandboxId</label><input v-model="admin.sandbox.sandboxId" /></div><div><label>BotCount</label><input v-model.number="admin.sandbox.botCount" type="number" /></div></div>
            <div class="row"><div><label>MaxPlayers</label><input v-model.number="admin.sandbox.maxPlayers" type="number" /></div><div><label>TickRate</label><input v-model.number="admin.sandbox.tickRate" type="number" /></div></div>
            <div class="actions action-bar"><button class="success" :disabled="admin.busy.value" @click="admin.startShooterSandbox">启动</button><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshDashboard">刷新</button><button class="warn" :disabled="admin.busy.value" @click="admin.stopShooterSandbox">停止</button></div>
          </section>
          <section id="rooms" class="card span-4">
            <div class="card-head"><div><p class="section-kicker">Room Flow</p><h2>创建房间</h2></div><span class="badge">Create</span></div>
            <div class="row"><div><label>标题</label><input v-model="admin.create.title" /></div><div><label>最大人数</label><input v-model.number="admin.create.maxPlayers" type="number" /></div></div>
            <div class="row"><div><label>RoomType</label><input v-model="admin.create.roomType" /></div><div><label>公开房间</label><select v-model="admin.create.isPublic"><option :value="true">true</option><option :value="false">false</option></select></div></div>
            <label>Tags JSON</label><textarea v-model="admin.create.tagsJson"></textarea>
            <div class="actions action-bar"><button :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.createRoom">创建并绑定</button><button class="secondary" @click="admin.applyGameplayDefaults">套用玩法默认值</button></div>
          </section>
          <section class="card span-5">
            <div class="card-head"><div><p class="section-kicker">Room Flow</p><h2>当前房间</h2></div><span class="badge">Current</span></div>
            <label>RoomId</label><input v-model="admin.roomId.value" placeholder="创建、选择或恢复后填充" />
            <div class="actions action-bar"><button :disabled="admin.busy.value || !admin.sessionToken.value || !admin.roomId.value" @click="admin.joinRoom">加入</button><button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.restoreCurrentRoom">恢复</button><button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.roomId.value" @click="admin.markOffline">标记离线</button><button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.roomId.value" @click="admin.leaveRoom">离开</button><button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.roomId.value" @click="admin.closeRoom">关闭</button></div>
            <div v-if="admin.snapshot.value?.summary" class="status-box"><strong>{{ admin.snapshot.value.summary.title || admin.snapshot.value.summary.roomId }}</strong><span>{{ admin.snapshot.value.summary.roomType }} / {{ admin.snapshot.value.members?.length || 0 }}人 / CanStart {{ admin.snapshot.value.canStart }}</span></div>
          </section>
          <section class="card span-7">
            <div class="card-head"><div><p class="section-kicker">Directory</p><h2>房间列表</h2></div><span class="badge">{{ admin.rooms.value.length }} Rooms</span></div>
            <div class="list"><article v-for="room in admin.rooms.value" :key="room.roomId" class="room-item"><div><strong>{{ room.title || room.roomId }}</strong><small>{{ room.roomId }}</small></div><div class="room-meta"><span>{{ room.roomType }}</span><span>{{ room.playerCount }}/{{ room.maxPlayers }}</span><span>{{ room.isPublic ? 'public' : 'private' }}</span></div><button class="secondary" @click="admin.selectRoom(room.roomId)">选择</button></article><p v-if="admin.rooms.value.length === 0" class="muted">暂无房间。</p></div>
          </section>
        </template>

        <section v-if="routeKey === 'battle'" id="battle" class="card span-6">
          <div class="card-head"><div><p class="section-kicker">Battle</p><h2>启动战斗</h2></div><span class="badge">Start</span></div>
          <div class="row"><div><label>GameplayId</label><input v-model.number="admin.battle.gameplayId" type="number" /></div><div><label>RuleSetId</label><input v-model.number="admin.battle.ruleSetId" type="number" /></div></div>
          <label>WorldType</label><input v-model="admin.battle.worldType" />
          <label>SyncTemplateId</label>
          <select v-if="admin.selectedGameplay.value && admin.selectedGameplay.value.supportedSyncTemplateIds?.length" v-model="admin.battle.syncTemplateId"><option v-for="syncTemplateId in admin.selectedGameplay.value.supportedSyncTemplateIds" :key="syncTemplateId" :value="syncTemplateId">{{ syncTemplateId }}</option></select>
          <input v-else v-model="admin.battle.syncTemplateId" />
          <div class="actions action-bar"><button class="success" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.roomId.value" @click="admin.startBattle">启动</button><button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.roomId.value || admin.selectedRoomType.value !== 'shooter'" @click="admin.startShooterRoomQuick">Shooter 快速开战</button></div>
        </section>

        <template v-if="routeKey === 'skills'">
          <SkillDiagnosticsPanel />
          <SkillAcceptancePanel />
        </template>

        <section v-if="routeKey === 'debug'" id="debug" class="card span-12 debug-panel">
          <div class="card-head"><div><p class="section-kicker">Response</p><h2>响应 / 聚合状态</h2></div><span class="badge">JSON</span></div>
          <pre>{{ admin.lastResponse.value || '等待操作...' }}</pre>
        </section>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import AdminApiBoundaryPanel from './components/AdminApiBoundaryPanel.vue';
import AdminSidebar from './components/AdminSidebar.vue';
import AdminTopbar from './components/AdminTopbar.vue';
import SkillAcceptancePanel from './components/SkillAcceptancePanel.vue';
import SkillDiagnosticsPanel from './components/SkillDiagnosticsPanel.vue';
import { adminNavigationItems } from './navigation/adminNavigation';
import { useAdminRouter } from './router/adminRouter';
import { useAdminConsoleStore } from './stores/adminConsoleStore';

const admin = useAdminConsoleStore();
const { routeKey, currentRoute, navigate } = useAdminRouter();

onMounted(admin.refreshAdminWorkspace);
</script>
