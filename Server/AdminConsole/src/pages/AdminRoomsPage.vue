<template>
  <template v-if="routeKey === 'rooms'">
    <section class="card span-4">
      <div class="card-head"><div><p class="section-kicker">运行环境</p><h2>环境</h2></div><span class="badge">作用域</span></div>
      <div class="row"><div><label>区域</label><input v-model="admin.region.value" /></div><div><label>服务器 ID</label><input v-model="admin.serverId.value" /></div></div>
      <label>玩法</label>
      <select v-model="admin.selectedRoomType.value" @change="admin.applyGameplayDefaults">
        <option v-for="gameplay in admin.gameplays.value" :key="gameplay.roomType" :value="gameplay.roomType">{{ gameplay.displayName }} / {{ gameplay.roomType }}</option>
      </select>
      <p v-if="admin.selectedGameplay.value" class="muted helper-text">默认 {{ admin.selectedGameplay.value.defaultMaxPlayers }} 人，{{ admin.selectedGameplay.value.defaultWorldType }}，{{ admin.selectedGameplay.value.defaultSyncTemplateId }}</p>
    </section>

    <section class="card span-4">
      <div class="card-head"><div><p class="section-kicker">模拟运行</p><h2>Shooter 沙盒</h2></div><span class="badge">沙盒</span></div>
      <div class="row"><div><label>沙盒 ID</label><input v-model="admin.sandbox.sandboxId" /></div><div><label>机器人数量</label><input v-model.number="admin.sandbox.botCount" type="number" /></div></div>
      <div class="row"><div><label>最大人数</label><input v-model.number="admin.sandbox.maxPlayers" type="number" /></div><div><label>帧率</label><input v-model.number="admin.sandbox.tickRate" type="number" /></div></div>
      <div class="actions action-bar"><button class="success" :disabled="admin.busy.value" @click="admin.startShooterSandbox">启动</button><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshDashboard">刷新</button><button class="warn" :disabled="admin.busy.value" @click="admin.stopShooterSandbox">停止</button></div>
    </section>

    <section id="rooms" class="card span-4">
      <div class="card-head"><div><p class="section-kicker">房间流程</p><h2>创建房间</h2></div><span class="badge">创建</span></div>
      <div class="row"><div><label>标题</label><input v-model="admin.create.title" /></div><div><label>最大人数</label><input v-model.number="admin.create.maxPlayers" type="number" /></div></div>
      <div class="row"><div><label>房间类型</label><input v-model="admin.create.roomType" /></div><div><label>公开房间</label><select v-model="admin.create.isPublic"><option :value="true">是</option><option :value="false">否</option></select></div></div>
      <label>标签 JSON</label><textarea v-model="admin.create.tagsJson"></textarea>
      <div class="actions action-bar"><button :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.createRoom">创建并绑定</button><button class="secondary" @click="admin.applyGameplayDefaults">套用玩法默认值</button></div>
    </section>

    <section class="card span-5">
      <div class="card-head"><div><p class="section-kicker">房间流程</p><h2>当前房间</h2></div><span class="badge">当前</span></div>
      <label>房间 ID</label><input v-model="admin.roomId.value" placeholder="创建、选择或恢复后填充" />
      <div class="actions action-bar"><button :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.joinRoom()">加入/绑定当前房间</button><button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.restoreCurrentRoom">恢复</button><button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.markOffline()">标记离线</button><button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.leaveRoom()">离开</button><button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.closeRoom()">关闭当前房间</button></div>
      <div class="actions compact-actions"><button :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.setRoomReady(true)">设为准备</button><button class="secondary" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.setRoomReady(false)">取消准备</button></div>
      <div v-if="admin.snapshot.value?.summary" class="status-box"><strong>{{ admin.snapshot.value.summary.title || admin.snapshot.value.summary.roomId }}</strong><span>{{ admin.snapshot.value.summary.roomType }} / {{ admin.snapshot.value.members?.length || 0 }}人 / 可开战 {{ admin.snapshot.value.canStart ? '是' : '否' }}</span></div>
    </section>

    <section class="card span-7">
      <div class="card-head"><div><p class="section-kicker">模拟玩家</p><h2>房间机器人</h2></div><span class="badge">Robot</span></div>
      <div class="row"><div><label>添加数量</label><input v-model.number="admin.roomRobots.count" type="number" min="1" max="64" /></div><div><label>账号前缀</label><input v-model="admin.roomRobots.accountPrefix" /></div></div>
      <div class="row"><div><label>AI Profile</label><input v-model="admin.roomRobots.battleAiProfileId" /></div><div><label>自动准备</label><select v-model="admin.roomRobots.autoReady"><option :value="true">是</option><option :value="false">否</option></select></div></div>
      <label class="check-row"><input v-model="admin.roomRobots.mountBattleAi" type="checkbox" /> 进入战斗后挂载局内 AI</label>
      <div class="actions action-bar"><button class="success" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value || admin.selectedRoomType.value !== 'shooter'" @click="admin.addRoomRobots">添加到当前房间</button><button class="secondary" :disabled="admin.busy.value || !admin.effectiveRoomId.value" @click="admin.refreshDashboard">刷新</button></div>
      <div v-if="admin.lastRobotAdd.value" class="status-box"><strong>已添加 {{ admin.lastRobotAdd.value.addedCount }} 个机器人</strong><span>{{ admin.lastRobotAdd.value.robotAccounts.join(', ') }}</span><small v-if="admin.lastRobotAdd.value.battleAiMounts.length">AI 挂载 {{ admin.lastRobotAdd.value.battleAiMounts.filter(x => x.accepted).length }}/{{ admin.lastRobotAdd.value.battleAiMounts.length }}</small></div>
    </section>

    <section class="card span-7">
      <div class="card-head"><div><p class="section-kicker">房间目录</p><h2>房间列表</h2></div><span class="badge">{{ admin.rooms.value.length }} 个房间</span></div>
      <div class="list"><article v-for="room in admin.rooms.value" :key="room.roomId" class="room-item" :class="{ active: admin.effectiveRoomId.value === room.roomId }"><div><strong>{{ room.title || room.roomId }}</strong><small>{{ room.roomId }}</small></div><div class="room-meta"><span>{{ room.roomType }}</span><span>{{ room.playerCount }}/{{ room.maxPlayers }}</span><span>{{ room.isPublic ? '公开' : '私有' }}</span><span v-if="admin.effectiveRoomId.value === room.roomId">当前</span></div><button class="secondary" :disabled="admin.busy.value" @click="admin.selectRoom(room.roomId)">选择并绑定</button><button class="warn" :disabled="admin.busy.value || !admin.sessionToken.value" @click="admin.closeRoom(room.roomId)">关闭</button></article><p v-if="admin.rooms.value.length === 0" class="muted">暂无房间。</p></div>
    </section>

    <ShooterWorldInspectorPanel />
  </template>
</template>

<script setup lang="ts">
import ShooterWorldInspectorPanel from '../components/shooter/ShooterWorldInspectorPanel.vue';
import { useAdminConsoleStore } from '../stores/adminConsoleStore';
import type { AdminRouteKey } from '../navigation/adminNavigation';

defineProps<{ routeKey: AdminRouteKey }>();

const admin = useAdminConsoleStore();
</script>
