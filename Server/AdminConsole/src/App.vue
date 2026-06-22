<template>
  <div class="shell">
    <header class="hero">
      <div>
        <p class="eyebrow">AbilityKit Orleans</p>
        <h1>Server Admin Console</h1>
        <p class="sub">真正前后端分离后台：独立 Vite/Vue/TypeScript 工程，Gateway 只提供 API 与构建产物托管。</p>
      </div>
      <div class="hero-actions">
        <button class="secondary" :disabled="busy" @click="refreshDashboard">刷新 Dashboard</button>
        <a class="link-button" href="/debug/index.html">Debug Console</a>
      </div>
    </header>

    <section class="metrics">
      <article class="metric"><span>账号</span><strong>{{ accountLabel }}</strong></article>
      <article class="metric"><span>玩法数</span><strong>{{ gameplays.length }}</strong></article>
      <article class="metric"><span>房间数</span><strong>{{ rooms.length }}</strong></article>
      <article class="metric"><span>Sandbox</span><strong>{{ sandboxLabel }}</strong></article>
      <article class="metric"><span>服务器</span><strong>{{ serverModeLabel }}</strong></article>
    </section>

    <main class="grid">
      <section class="card span-4">
        <h2>会话</h2>
        <label>AccountId</label>
        <input v-model="account.accountId" placeholder="admin 或测试账号" />
        <div class="row">
          <div><label>ExpireSeconds</label><input v-model.number="account.expireSeconds" type="number" /></div>
          <div><label>KickExisting</label><select v-model="account.kickExisting"><option :value="true">true</option><option :value="false">false</option></select></div>
        </div>
        <label>SessionToken</label>
        <input v-model="sessionToken" placeholder="登录后自动保存" />
        <div class="actions">
          <button :disabled="busy" @click="guestLogin">游客登录</button>
          <button :disabled="busy || !account.accountId" @click="accountLogin">账号登录</button>
          <button class="secondary" :disabled="busy || !sessionToken" @click="validateSession">校验</button>
          <button class="warn" :disabled="busy || !sessionToken" @click="logout">登出</button>
        </div>
      </section>

      <section class="card span-4">
        <h2>环境</h2>
        <div class="row">
          <div><label>Region</label><input v-model="region" /></div>
          <div><label>ServerId</label><input v-model="serverId" /></div>
        </div>
        <label>玩法</label>
        <select v-model="selectedRoomType" @change="applyGameplayDefaults">
          <option v-for="gameplay in gameplays" :key="gameplay.roomType" :value="gameplay.roomType">{{ gameplay.displayName }} / {{ gameplay.roomType }}</option>
        </select>
        <p v-if="selectedGameplay" class="muted">默认 {{ selectedGameplay.defaultMaxPlayers }} 人，{{ selectedGameplay.defaultWorldType }}，{{ selectedGameplay.defaultSyncTemplateId }}</p>
      </section>

      <section class="card span-4">
        <h2>服务器运维</h2>
        <div v-if="serverStatus" class="ops-status">
          <div><span>环境</span><strong>{{ serverStatus.environmentName }}</strong></div>
          <div><span>进程</span><strong>{{ serverStatus.processName }} #{{ serverStatus.processId }}</strong></div>
          <div><span>机器</span><strong>{{ serverStatus.machineName }}</strong></div>
          <div><span>运行</span><strong>{{ formatDuration(serverStatus.uptimeSeconds) }}</strong></div>
          <div><span>内存</span><strong>{{ formatBytes(serverStatus.workingSetBytes) }}</strong></div>
          <div><span>GC</span><strong>{{ formatBytes(serverStatus.gcTotalMemoryBytes) }}</strong></div>
          <div><span>线程</span><strong>{{ serverStatus.threadCount }}</strong></div>
          <div><span>状态</span><strong>{{ serverModeLabel }}</strong></div>
        </div>
        <p v-else class="muted">等待服务器状态。</p>
        <label>操作原因</label>
        <input v-model="serverOperation.reason" placeholder="例如：版本发布、压测维护" />
        <div class="actions">
          <button class="secondary" :disabled="busy" @click="refreshServerStatus">刷新状态</button>
          <button :disabled="busy || !sessionToken" @click="setMaintenanceMode(!serverStatus?.maintenanceMode)">{{ serverStatus?.maintenanceMode ? '关闭维护' : '开启维护' }}</button>
          <button :disabled="busy || !sessionToken" @click="setDrainMode(!serverStatus?.drainMode)">{{ serverStatus?.drainMode ? '关闭 Drain' : '开启 Drain' }}</button>
          <button class="warn" :disabled="busy || !sessionToken" @click="requestRestart">请求重启</button>
        </div>
        <p v-if="serverStatus?.lastOperationId" class="muted">最近操作 {{ serverStatus.lastOperationId }} / {{ serverStatus.lastOperationRequestedBy }} / {{ serverStatus.lastOperationReason || '无原因' }}</p>
      </section>

      <section class="card span-4">
        <h2>Shooter Sandbox</h2>
        <div class="row">
          <div><label>SandboxId</label><input v-model="sandbox.sandboxId" /></div>
          <div><label>BotCount</label><input v-model.number="sandbox.botCount" type="number" /></div>
        </div>
        <div class="row">
          <div><label>MaxPlayers</label><input v-model.number="sandbox.maxPlayers" type="number" /></div>
          <div><label>TickRate</label><input v-model.number="sandbox.tickRate" type="number" /></div>
        </div>
        <div class="actions">
          <button class="success" :disabled="busy" @click="startShooterSandbox">启动</button>
          <button class="secondary" :disabled="busy" @click="refreshDashboard">刷新</button>
          <button class="warn" :disabled="busy" @click="stopShooterSandbox">停止</button>
        </div>
      </section>

      <section class="card span-6">
        <h2>创建房间</h2>
        <div class="row">
          <div><label>标题</label><input v-model="create.title" /></div>
          <div><label>最大人数</label><input v-model.number="create.maxPlayers" type="number" /></div>
        </div>
        <div class="row">
          <div><label>RoomType</label><input v-model="create.roomType" /></div>
          <div><label>公开房间</label><select v-model="create.isPublic"><option :value="true">true</option><option :value="false">false</option></select></div>
        </div>
        <label>Tags JSON</label>
        <textarea v-model="create.tagsJson"></textarea>
        <div class="actions">
          <button :disabled="busy || !sessionToken" @click="createRoom">创建并绑定</button>
          <button class="secondary" @click="applyGameplayDefaults">套用玩法默认值</button>
        </div>
      </section>

      <section class="card span-6">
        <h2>当前房间</h2>
        <label>RoomId</label>
        <input v-model="roomId" placeholder="创建、选择或恢复后填充" />
        <div class="actions">
          <button :disabled="busy || !sessionToken || !roomId" @click="joinRoom">加入</button>
          <button class="secondary" :disabled="busy || !sessionToken" @click="restoreCurrentRoom">恢复</button>
          <button class="secondary" :disabled="busy || !sessionToken || !roomId" @click="markOffline">标记离线</button>
          <button class="warn" :disabled="busy || !sessionToken || !roomId" @click="leaveRoom">离开</button>
          <button class="warn" :disabled="busy || !sessionToken || !roomId" @click="closeRoom">关闭</button>
        </div>
        <div v-if="snapshot && snapshot.summary" class="status-box">
          <strong>{{ snapshot.summary.title || snapshot.summary.roomId }}</strong>
          <span>{{ snapshot.summary.roomType }} / {{ snapshot.members?.length || 0 }}人 / CanStart {{ snapshot.canStart }}</span>
        </div>
      </section>

      <section class="card span-5">
        <h2>启动战斗</h2>
        <div class="row">
          <div><label>GameplayId</label><input v-model.number="battle.gameplayId" type="number" /></div>
          <div><label>RuleSetId</label><input v-model.number="battle.ruleSetId" type="number" /></div>
        </div>
        <label>WorldType</label>
        <input v-model="battle.worldType" />
        <label>SyncTemplateId</label>
        <select v-if="selectedGameplay && selectedGameplay.supportedSyncTemplateIds?.length" v-model="battle.syncTemplateId">
          <option v-for="syncTemplateId in selectedGameplay.supportedSyncTemplateIds" :key="syncTemplateId" :value="syncTemplateId">{{ syncTemplateId }}</option>
        </select>
        <input v-else v-model="battle.syncTemplateId" />
        <div class="actions">
          <button class="success" :disabled="busy || !sessionToken || !roomId" @click="startBattle">启动</button>
          <button class="secondary" :disabled="busy || !sessionToken || !roomId || selectedRoomType !== 'shooter'" @click="startShooterRoomQuick">Shooter 快速开战</button>
        </div>
      </section>

      <section class="card span-7">
        <h2>房间列表</h2>
        <div class="list">
          <article v-for="room in rooms" :key="room.roomId" class="room-item">
            <div><strong>{{ room.title || room.roomId }}</strong><small>{{ room.roomId }}</small></div>
            <div class="room-meta"><span>{{ room.roomType }}</span><span>{{ room.playerCount }}/{{ room.maxPlayers }}</span><span>{{ room.isPublic ? 'public' : 'private' }}</span></div>
            <button class="secondary" @click="selectRoom(room.roomId)">选择</button>
          </article>
          <p v-if="rooms.length === 0" class="muted">暂无房间。</p>
        </div>
      </section>

      <section class="card span-12">
        <h2>响应 / 聚合状态</h2>
        <pre>{{ lastResponse || '等待操作...' }}</pre>
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { AdminApiClient } from './services/adminApiClient';
import { adminStorage } from './services/storage';
import type { AdminDashboardResponse, AdminServerOperationResponse, AdminServerStatus, CreateRoomResponse, GameplayDescriptor, RestoreRoomResponse, RoomRuntimeState, RoomSnapshot, RoomSummary, SessionResponse, ShooterSandboxState } from './types';

const api = new AdminApiClient();
const busy = ref(false);
const sessionToken = ref(adminStorage.get('sessionToken', ''));
const region = ref(adminStorage.get('region', 'cn'));
const serverId = ref(adminStorage.get('serverId', 'dev'));
const selectedRoomType = ref('shooter');
const roomId = ref(adminStorage.get('roomId', ''));
const gameplays = ref<GameplayDescriptor[]>([]);
const rooms = ref<RoomSummary[]>([]);
const snapshot = ref<RoomSnapshot | null>(null);
const runtimeState = ref<RoomRuntimeState | null>(null);
const dashboard = ref<AdminDashboardResponse | null>(null);
const serverStatus = ref<AdminServerStatus | null>(null);
const lastResponse = ref('');

const account = reactive({ accountId: adminStorage.get('accountId', ''), expireSeconds: 86400, kickExisting: true });
const create = reactive({ roomType: 'shooter', title: 'Shooter Admin Room', isPublic: true, maxPlayers: 4, tagsJson: '{\n  "source": "admin-console"\n}' });
const battle = reactive({ gameplayId: 2, ruleSetId: 0, configVersion: 1, protocolVersion: 1, worldType: 'shooter_battle', syncTemplateId: 'pure-state-authority' });
const sandbox = reactive<{ sandboxId: string; botCount: number; maxPlayers: number; tickRate: number; state: ShooterSandboxState | null }>({ sandboxId: 'default', botCount: 3, maxPlayers: 4, tickRate: 30, state: null });
const serverOperation = reactive({ reason: 'admin console operation' });

const selectedGameplay = computed(() => gameplays.value.find(x => x.roomType === selectedRoomType.value) || null);
const accountLabel = computed(() => dashboard.value?.accountId || account.accountId || (sessionToken.value ? 'session' : '未登录'));
const sandboxLabel = computed(() => sandbox.state?.running ? 'Running' : 'Stopped');
const serverModeLabel = computed(() => {
  if (!serverStatus.value) return 'Unknown';
  const flags = [];
  if (serverStatus.value.restartRequested) flags.push('RestartRequested');
  if (serverStatus.value.maintenanceMode) flags.push('Maintenance');
  if (serverStatus.value.drainMode) flags.push('Drain');
  return flags.length > 0 ? flags.join(' / ') : 'Normal';
});

watch(sessionToken, value => adminStorage.set('sessionToken', value));
watch(() => account.accountId, value => adminStorage.set('accountId', value));
watch(region, value => adminStorage.set('region', value));
watch(serverId, value => adminStorage.set('serverId', value));
watch(roomId, value => adminStorage.set('roomId', value));

async function call<T>(url: string, body?: unknown, method?: string): Promise<T | null> {
  busy.value = true;
  try {
    const result = await api.request<T>(url, body, method);
    lastResponse.value = JSON.stringify(result, null, 2);
    return result.ok ? result.body as T : null;
  } catch (error) {
    lastResponse.value = String(error);
    return null;
  } finally {
    busy.value = false;
  }
}

async function refreshDashboard(): Promise<void> {
  const data = await call<AdminDashboardResponse>('/api/admin/dashboard', {
    sessionToken: sessionToken.value || null,
    region: region.value,
    serverId: serverId.value,
    roomType: selectedRoomType.value,
    limit: 50,
    sandboxId: sandbox.sandboxId || 'default'
  });
  if (!data) return;
  dashboard.value = data;
  gameplays.value = data.gameplays || [];
  rooms.value = data.rooms || [];
  sandbox.state = data.shooterSandbox || null;
  serverStatus.value = data.serverStatus || serverStatus.value;
  runtimeState.value = data.runtimeState || runtimeState.value;
  if (data.currentRoomId) roomId.value = data.currentRoomId;
  if (data.currentRoom?.snapshot) snapshot.value = data.currentRoom.snapshot;
  if (!gameplays.value.some(x => x.roomType === selectedRoomType.value) && gameplays.value.length > 0) {
    selectedRoomType.value = gameplays.value[0].roomType;
  }
  applyGameplayDefaults();
}

async function refreshServerStatus(): Promise<void> {
  const data = await call<AdminServerStatus>('/api/admin/server/status', undefined, 'GET');
  if (data) serverStatus.value = data;
}

async function setMaintenanceMode(enabled: boolean): Promise<void> {
  const data = await call<AdminServerOperationResponse>('/api/admin/server/maintenance', { sessionToken: sessionToken.value, enabled, reason: serverOperation.reason });
  if (data?.status) serverStatus.value = data.status;
}

async function setDrainMode(enabled: boolean): Promise<void> {
  const data = await call<AdminServerOperationResponse>('/api/admin/server/drain', { sessionToken: sessionToken.value, enabled, reason: serverOperation.reason });
  if (data?.status) serverStatus.value = data.status;
}

async function requestRestart(): Promise<void> {
  const data = await call<AdminServerOperationResponse>('/api/admin/server/restart-request', { sessionToken: sessionToken.value, enabled: true, reason: serverOperation.reason });
  if (data?.status) serverStatus.value = data.status;
}

function formatBytes(value: number): string {
  if (!Number.isFinite(value) || value <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  let size = value;
  let unit = 0;
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024;
    unit += 1;
  }
  return `${size.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
}

function formatDuration(seconds: number): string {
  const value = Math.max(0, Math.floor(seconds || 0));
  const days = Math.floor(value / 86400);
  const hours = Math.floor((value % 86400) / 3600);
  const minutes = Math.floor((value % 3600) / 60);
  if (days > 0) return `${days}d ${hours}h`;
  if (hours > 0) return `${hours}h ${minutes}m`;
  return `${minutes}m`;
}

function applyGameplayDefaults(): void {
  const gameplay = selectedGameplay.value;
  if (!gameplay) return;
  create.roomType = gameplay.roomType;
  create.maxPlayers = gameplay.defaultMaxPlayers || create.maxPlayers;
  battle.gameplayId = Number(gameplay.gameplayId || battle.gameplayId || 0);
  battle.worldType = gameplay.defaultWorldType || gameplay.roomType;
  battle.syncTemplateId = gameplay.defaultSyncTemplateId || '';
}

async function guestLogin(): Promise<void> {
  const data = await call<SessionResponse>('/api/guest/login', {}, 'POST');
  if (data?.sessionToken) sessionToken.value = data.sessionToken;
  await refreshDashboard();
}

async function accountLogin(): Promise<void> {
  const data = await call<SessionResponse>('/api/accounts/login', { accountId: account.accountId, expireSeconds: Number(account.expireSeconds || 86400), kickExisting: account.kickExisting === true });
  if (data?.sessionToken) sessionToken.value = data.sessionToken;
  await refreshDashboard();
}

async function validateSession(): Promise<void> {
  await call<SessionResponse>('/api/session/validate', { sessionToken: sessionToken.value });
}

async function logout(): Promise<void> {
  await call('/api/session/logout', { sessionToken: sessionToken.value });
  sessionToken.value = '';
  await refreshDashboard();
}

function parseTags(): Record<string, string> | null {
  const text = create.tagsJson.trim();
  return text ? JSON.parse(text) as Record<string, string> : null;
}

async function createRoom(): Promise<void> {
  let tags: Record<string, string> | null = null;
  try { tags = parseTags(); } catch { lastResponse.value = 'Invalid tags JSON'; return; }
  const data = await call<CreateRoomResponse>('/api/rooms/create', { sessionToken: sessionToken.value, region: region.value, serverId: serverId.value, roomType: create.roomType, title: create.title, isPublic: create.isPublic === true, maxPlayers: Number(create.maxPlayers || 0), tags });
  if (data?.roomId) roomId.value = data.roomId;
  await refreshDashboard();
}

function selectRoom(value: string): void {
  roomId.value = value;
}

async function joinRoom(): Promise<void> {
  const data = await call<CreateRoomResponse>('/api/rooms/join', { sessionToken: sessionToken.value, roomId: roomId.value });
  if (data?.snapshot) snapshot.value = data.snapshot;
  await refreshDashboard();
}

async function restoreCurrentRoom(): Promise<void> {
  const data = await call<RestoreRoomResponse>('/api/rooms/restore-current', { sessionToken: sessionToken.value });
  if (data?.hasActiveRoom && data.snapshot) {
    snapshot.value = data.snapshot;
    roomId.value = data.snapshot.summary?.roomId || roomId.value;
  }
}

async function leaveRoom(): Promise<void> {
  await call('/api/rooms/leave', { sessionToken: sessionToken.value, roomId: roomId.value });
  snapshot.value = null;
  runtimeState.value = null;
  roomId.value = '';
  await refreshDashboard();
}

async function markOffline(): Promise<void> {
  const data = await call<RoomRuntimeState>('/api/rooms/mark-offline', { sessionToken: sessionToken.value, roomId: roomId.value });
  if (data) runtimeState.value = data;
}

async function closeRoom(): Promise<void> {
  await call('/api/rooms/close', { sessionToken: sessionToken.value, roomId: roomId.value });
  snapshot.value = null;
  runtimeState.value = null;
  roomId.value = '';
  await refreshDashboard();
}

async function startShooterRoomQuick(): Promise<void> {
  battle.gameplayId = 2;
  battle.ruleSetId = 0;
  battle.configVersion = 1;
  battle.protocolVersion = 1;
  battle.worldType = 'shooter_battle';
  battle.syncTemplateId = battle.syncTemplateId || 'pure-state-authority';
  await call('/api/rooms/ready', { sessionToken: sessionToken.value, roomId: roomId.value, ready: true });
  await startBattle();
}

async function startBattle(): Promise<void> {
  await call('/api/rooms/start-battle', { sessionToken: sessionToken.value, roomId: roomId.value, gameplayId: Number(battle.gameplayId || 0), ruleSetId: Number(battle.ruleSetId || 0), configVersion: Number(battle.configVersion || 1), protocolVersion: Number(battle.protocolVersion || 1), worldType: battle.worldType || null, clientId: 'admin-console', syncTemplateId: battle.syncTemplateId || null, syncModel: null, networkEnvironmentId: 'admin-console', carrierName: 'admin', enableAuthoritativeWorld: true, interpolationEnabled: true, inputDelayFrames: 0 });
  await refreshDashboard();
}

async function startShooterSandbox(): Promise<void> {
  await call('/api/shooter-sandbox/start', { sandboxId: sandbox.sandboxId || 'default', region: region.value, serverId: serverId.value, botCount: Number(sandbox.botCount || 3), maxPlayers: Number(sandbox.maxPlayers || 4), tickRate: Number(sandbox.tickRate || 30), title: 'Shooter Admin Sandbox', tags: { source: 'admin-console' } });
  await refreshDashboard();
}

async function stopShooterSandbox(): Promise<void> {
  await call('/api/shooter-sandbox/stop', { sandboxId: sandbox.sandboxId || 'default' });
  await refreshDashboard();
}

onMounted(refreshDashboard);
</script>
