<template>
  <section id="skills" class="card span-12 skill-panel">
    <div class="card-head">
      <div><p class="section-kicker">技能诊断</p><h2>技能分析</h2></div>
      <span class="badge">{{ admin.skillSummary.value?.diagnosticsStatus || '等待追踪' }}</span>
    </div>
    <div class="skill-layout">
      <div class="skill-column">
        <h3>当前上下文</h3>
        <div class="ops-status skill-context">
          <div><span>房间 ID</span><strong>{{ admin.skillSummary.value?.roomId || admin.runtimeState.value?.roomId || admin.roomId.value || '未选择' }}</strong></div>
          <div><span>房间类型</span><strong>{{ admin.skillSummary.value?.roomType || admin.runtimeState.value?.roomType || admin.selectedRoomType.value }}</strong></div>
          <div><span>战斗 ID</span><strong>{{ admin.skillSummary.value?.battleId || admin.runtimeState.value?.battleId || '未启动' }}</strong></div>
          <div><span>世界 ID</span><strong>{{ admin.skillSummary.value?.worldId ?? admin.runtimeState.value?.worldId ?? 0 }}</strong></div>
          <div><span>帧号</span><strong>{{ admin.skillSummary.value?.currentFrame ?? 0 }}</strong></div>
          <div><span>战斗中</span><strong>{{ admin.skillSummary.value?.isInBattle ?? admin.runtimeState.value?.isInBattle ?? false }}</strong></div>
        </div>
        <h3>MOBA 出战配置</h3>
        <p class="muted helper-text">衔接现有房间接口 /api/admin/rooms/pick-hero：先选择或创建 moba 房间，再提交英雄、队伍、出生点与技能槽配置。</p>
        <div class="row"><div><label>英雄 ID</label><input v-model.number="admin.skillLoadout.heroId" type="number" /></div><div><label>队伍 ID</label><input v-model.number="admin.skillLoadout.teamId" type="number" /></div></div>
        <div class="row"><div><label>出生点 ID</label><input v-model.number="admin.skillLoadout.spawnPointId" type="number" /></div><div><label>等级</label><input v-model.number="admin.skillLoadout.level" type="number" /></div></div>
        <div class="row"><div><label>属性模板 ID</label><input v-model.number="admin.skillLoadout.attributeTemplateId" type="number" /></div><div><label>普攻技能 ID</label><input v-model.number="admin.skillLoadout.basicAttackSkillId" type="number" /></div></div>
        <label>技能 ID（逗号分隔）</label><input v-model="admin.skillLoadout.skillIdsText" placeholder="例如：1001,1002,1003" />
        <div class="actions action-bar"><button class="success" :disabled="admin.busy.value || !admin.sessionToken.value || !admin.effectiveRoomId.value" @click="admin.submitMobaLoadout">提交 MOBA 出战配置</button><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshSkillDiagnostics">刷新技能诊断</button></div>
      </div>
      <div class="skill-column">
        <h3>运行指标</h3>
        <div class="skill-metrics">
          <article v-for="metric in admin.skillSummary.value?.metrics || []" :key="metric.name" class="metric-card"><span>{{ metric.name }}</span><strong>{{ metric.value }} {{ metric.unit }}</strong><small>{{ metric.source }}</small></article>
          <p v-if="!admin.skillSummary.value?.metrics?.length" class="muted">等待技能诊断指标。</p>
        </div>
        <h3>分析数据源</h3>
        <div v-if="admin.skillAnalysisModel.value" class="analysis-model compact">
          <div class="status-box"><strong>{{ admin.skillAnalysisModel.value.modelVersion }}</strong><span>当前运行态只展示房间、战斗帧与已接入事件；完整技能链路请先查看 Scenario 验收报告。</span></div>
          <div class="analysis-fields"><span v-for="field in admin.skillAnalysisModel.value.correlationFields" :key="field.name" class="badge" :class="field.requiredForCorrelation ? '' : 'secondary'" :title="field.description">{{ field.name }}</span></div>
          <ul class="diagnostic-list"><li v-for="note in admin.skillAnalysisModelNotes.value" :key="note">{{ note }}</li></ul>
        </div>
        <p v-else class="muted">等待分析数据源描述。</p>
        <h3>参与角色</h3>
        <article v-for="actor in admin.skillSummary.value?.actors || []" :key="actor.accountId" class="skill-actor"><div><strong>{{ actor.accountId }}</strong><small>角色 {{ actor.actorId }} / 普攻 {{ actor.basicAttackSkillId || '未投影' }}</small></div><p>{{ actor.skillIds.length ? actor.skillIds.join(', ') : actor.diagnostics }}</p></article>
        <h3>诊断提示</h3>
        <ul class="diagnostic-list warnings"><li v-for="warning in admin.skillDiagnosticsWarnings.value" :key="warning">{{ warning }}</li><li v-if="!admin.skillDiagnosticsWarnings.value.length">暂无技能诊断告警。</li></ul>
      </div>
    </div>
    <div class="event-filter row"><div><label>战斗 ID 过滤</label><input v-model="admin.skillEventFilter.battleId" placeholder="默认使用当前 battleId" /></div><div><label>数量上限</label><input v-model.number="admin.skillEventFilter.limit" type="number" /></div><div><label>角色 ID</label><input v-model.number="admin.skillEventFilter.actorId" type="number" /></div><div><label>技能 ID</label><input v-model.number="admin.skillEventFilter.skillId" type="number" /></div></div>
    <div class="actions action-bar"><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshSkillEvents">刷新事件时间线</button></div>
    <SkillTraceVisualizer title="运行态实时追踪" :nodes="admin.runtimeAnalysisFilteredFlat.value" :total-node-count="admin.runtimeAnalysisFlat.value.length" :selected-node="admin.selectedRuntimeAnalysisNode.value" :timeline="admin.runtimeAnalysisTimeline.value" :filter="admin.runtimeAnalysisFilter" :filter-options="admin.runtimeAnalysisFilterOptions.value" :entity-relations="admin.runtimeAnalysisEntityRelations.value" empty-text="当前运行态还没有可展开的技能链路节点；请在场景验收报告查看已有追踪，或刷新运行事件。" @select-node="admin.selectRuntimeAnalysisNode" />
    <div class="event-timeline"><article v-for="event in admin.skillEvents.value?.events || []" :key="`${event.frame}-${event.skillInstanceId}-${event.stage}`" class="event-item"><span class="badge">{{ event.severity }}</span><div><strong>F{{ event.frame }} 角色 {{ event.actorId }} 技能 {{ event.skillId }}</strong><small>{{ event.stage }} / {{ event.eventType }} / 目标 {{ event.targetActorId ?? '-' }}</small></div><p>{{ event.message || event.value || '无载荷' }}</p></article><p v-if="!admin.skillEvents.value?.events?.length" class="muted">当前没有运行态技能事件；可先通过场景验收报告查看已保存的技能流程追踪。</p></div>
  </section>
</template>

<script setup lang="ts">
import SkillTraceVisualizer from './SkillTraceVisualizer.vue';
import { useAdminConsoleStore } from '../../stores/adminConsoleStore';

const admin = useAdminConsoleStore();
</script>
