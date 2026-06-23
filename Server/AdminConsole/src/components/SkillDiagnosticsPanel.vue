<template>
  <section id="skills" class="card span-12 skill-panel">
    <div class="card-head">
      <div><p class="section-kicker">Skill Diagnostics</p><h2>技能分析</h2></div>
      <span class="badge">{{ admin.skillSummary.value?.diagnosticsStatus || 'Trace Pending' }}</span>
    </div>
    <div class="skill-layout">
      <div class="skill-column">
        <h3>当前上下文</h3>
        <div class="ops-status skill-context">
          <div><span>RoomId</span><strong>{{ admin.skillSummary.value?.roomId || admin.runtimeState.value?.roomId || admin.roomId.value || '未选择' }}</strong></div>
          <div><span>RoomType</span><strong>{{ admin.skillSummary.value?.roomType || admin.runtimeState.value?.roomType || admin.selectedRoomType.value }}</strong></div>
          <div><span>BattleId</span><strong>{{ admin.skillSummary.value?.battleId || admin.runtimeState.value?.battleId || '未启动' }}</strong></div>
          <div><span>WorldId</span><strong>{{ admin.skillSummary.value?.worldId ?? admin.runtimeState.value?.worldId ?? 0 }}</strong></div>
          <div><span>Frame</span><strong>{{ admin.skillSummary.value?.currentFrame ?? 0 }}</strong></div>
          <div><span>InBattle</span><strong>{{ admin.skillSummary.value?.isInBattle ?? admin.runtimeState.value?.isInBattle ?? false }}</strong></div>
        </div>
        <h3>MOBA Loadout</h3>
        <p class="muted helper-text">衔接现有房间接口 /api/admin/rooms/pick-hero：先选择或创建 moba 房间，再提交英雄、队伍、出生点与技能槽配置。</p>
        <div class="row"><div><label>HeroId</label><input v-model.number="admin.skillLoadout.heroId" type="number" /></div><div><label>TeamId</label><input v-model.number="admin.skillLoadout.teamId" type="number" /></div></div>
        <div class="row"><div><label>SpawnPointId</label><input v-model.number="admin.skillLoadout.spawnPointId" type="number" /></div><div><label>Level</label><input v-model.number="admin.skillLoadout.level" type="number" /></div></div>
        <div class="row"><div><label>AttributeTemplateId</label><input v-model.number="admin.skillLoadout.attributeTemplateId" type="number" /></div><div><label>BasicAttackSkillId</label><input v-model.number="admin.skillLoadout.basicAttackSkillId" type="number" /></div></div>
        <label>SkillIds（逗号分隔）</label><input v-model="admin.skillLoadout.skillIdsText" placeholder="例如：1001,1002,1003" />
        <div class="actions action-bar"><button class="success" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.roomId.value" @click="admin.submitMobaLoadout">提交 MOBA Loadout</button><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshSkillDiagnostics">刷新技能诊断</button></div>
      </div>
      <div class="skill-column">
        <h3>运行指标</h3>
        <div class="skill-metrics">
          <article v-for="metric in admin.skillSummary.value?.metrics || []" :key="metric.name" class="metric-card"><span>{{ metric.name }}</span><strong>{{ metric.value }} {{ metric.unit }}</strong><small>{{ metric.source }}</small></article>
          <p v-if="!admin.skillSummary.value?.metrics?.length" class="muted">等待技能诊断指标。</p>
        </div>
        <h3>统一分析模型</h3>
        <div v-if="admin.skillAnalysisModel.value" class="analysis-model">
          <div class="status-box"><strong>{{ admin.skillAnalysisModel.value.modelVersion }}</strong><span>Sources: {{ admin.skillAnalysisModel.value.sources.join(' / ') }}</span></div>
          <article v-for="stage in admin.skillAnalysisModel.value.stages" :key="stage.id" class="analysis-stage"><strong>{{ stage.displayName }}</strong><small>{{ stage.runtimeSource }} ⇄ {{ stage.acceptanceSource }}</small><p>{{ stage.fields.join(', ') }}</p></article>
          <div class="analysis-fields"><span v-for="field in admin.skillAnalysisModel.value.correlationFields" :key="field.name" class="badge" :class="field.requiredForCorrelation ? '' : 'danger'" :title="field.description">{{ field.name }}</span></div>
            <article v-for="schema in admin.skillAnalysisModel.value.projectionSchemas || []" :key="schema.id" class="analysis-stage"><strong>{{ schema.displayName }}</strong><small>{{ schema.id }}</small><p>{{ schema.description }} / {{ schema.fields.join(', ') }}</p></article>
        </div>
        <p v-else class="muted">等待统一分析模型。</p>
        <h3>参与 Actor</h3>
        <article v-for="actor in admin.skillSummary.value?.actors || []" :key="actor.accountId" class="skill-actor"><div><strong>{{ actor.accountId }}</strong><small>Actor {{ actor.actorId }} / Basic {{ actor.basicAttackSkillId || '未投影' }}</small></div><p>{{ actor.skillIds.length ? actor.skillIds.join(', ') : actor.diagnostics }}</p></article>
        <h3>告警</h3>
        <ul class="diagnostic-list warnings"><li v-for="warning in [...(admin.skillSummary.value?.warnings || []), ...(admin.skillEvents.value?.warnings || []), ...(admin.skillAnalysisModel.value?.notes || [])]" :key="warning">{{ warning }}</li><li v-if="!admin.skillSummary.value?.warnings?.length && !admin.skillEvents.value?.warnings?.length && !admin.skillAnalysisModel.value?.notes?.length">暂无技能诊断告警。</li></ul>
      </div>
    </div>
    <div class="event-filter row"><div><label>BattleId Filter</label><input v-model="admin.skillEventFilter.battleId" placeholder="默认使用当前 battleId" /></div><div><label>Limit</label><input v-model.number="admin.skillEventFilter.limit" type="number" /></div><div><label>ActorId</label><input v-model.number="admin.skillEventFilter.actorId" type="number" /></div><div><label>SkillId</label><input v-model.number="admin.skillEventFilter.skillId" type="number" /></div></div>
    <div class="actions action-bar"><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshSkillEvents">刷新事件时间线</button></div>
    <SkillTraceVisualizer title="Runtime Live Trace" :nodes="admin.runtimeAnalysisFilteredFlat.value" :total-node-count="admin.runtimeAnalysisFlat.value.length" :selected-node="admin.selectedRuntimeAnalysisNode.value" :timeline="admin.runtimeAnalysisTimeline.value" :filter="admin.runtimeAnalysisFilter" :filter-options="admin.runtimeAnalysisFilterOptions.value" :entity-relations="admin.runtimeAnalysisEntityRelations.value" empty-text="runtime trace sink 尚未接入；当前仅展示事件时间轴投影占位。" />
    <div class="event-timeline"><article v-for="event in admin.skillEvents.value?.events || []" :key="`${event.frame}-${event.skillInstanceId}-${event.stage}`" class="event-item"><span class="badge">{{ event.severity }}</span><div><strong>F{{ event.frame }} Actor {{ event.actorId }} Skill {{ event.skillId }}</strong><small>{{ event.stage }} / {{ event.eventType }} / Target {{ event.targetActorId ?? '-' }}</small></div><p>{{ event.message || event.value || 'no payload' }}</p></article><p v-if="!admin.skillEvents.value?.events?.length" class="muted">技能 Trace 尚未接入，事件时间线当前为空；后续会接入 MOBA runtime trace sink。</p></div>
  </section>
</template>

<script setup lang="ts">
import SkillTraceVisualizer from './SkillTraceVisualizer.vue';
import { useAdminConsoleStore } from '../stores/adminConsoleStore';

const admin = useAdminConsoleStore();
</script>
