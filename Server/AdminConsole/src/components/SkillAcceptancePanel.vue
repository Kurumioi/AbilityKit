<template>
  <section id="skill-acceptance" class="card span-12 acceptance-panel">
    <div class="card-head"><div><p class="section-kicker">Scenario Artifacts</p><h2>Scenario 验收报告</h2></div><span class="badge">{{ admin.acceptanceBatch.value?.cases?.length || 0 }} Cases</span></div>
    <div class="acceptance-toolbar row"><div><label>ArtifactDirectory</label><input v-model="admin.acceptance.artifactDirectory" placeholder="artifacts/moba-acceptance" /></div><div><label>TraceLimit</label><input v-model.number="admin.acceptance.traceLimit" type="number" /></div></div>
    <div class="acceptance-filters row"><div><label>搜索 Case / 描述 / 路径</label><input v-model="admin.acceptance.searchText" placeholder="例如：projectile、damage、case id" /></div><div><label>状态过滤</label><select v-model="admin.acceptance.statusFilter"><option value="all">全部</option><option value="failed">失败优先排查</option><option value="passed">仅通过</option><option value="unknown">未知</option></select></div><div><label>排序</label><select v-model="admin.acceptance.sortKey"><option value="caseId">CaseId</option><option value="failedFirst">失败优先</option><option value="duration">耗时</option><option value="trace">Trace 数量</option><option value="status">状态</option></select></div></div>
    <div class="actions action-bar"><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshAcceptanceArtifacts">刷新 Scenario artifacts</button><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshAcceptanceRunPlan">查看执行入口边界</button></div>
    <div class="ops-status acceptance-summary"><div><span>Artifact</span><strong>{{ admin.acceptanceBatch.value?.artifactDirectory || admin.acceptance.artifactDirectory }}</strong></div><div><span>BatchSummary</span><strong>{{ admin.acceptanceBatch.value?.hasBatchSummary ? 'Found' : 'Missing' }}</strong></div><div><span>Passed</span><strong>{{ admin.acceptancePassedCount.value }}</strong></div><div><span>Failed</span><strong>{{ admin.acceptanceFailedCount.value }}</strong></div><div><span>Unknown</span><strong>{{ admin.acceptanceUnknownCount.value }}</strong></div><div><span>Filtered</span><strong>{{ admin.acceptanceFilteredCases.value.length }}</strong></div></div>
    <div class="acceptance-layout">
      <div class="acceptance-case-list">
        <h3>Case 列表</h3>
        <article v-for="item in admin.acceptanceFilteredCases.value" :key="item.caseId" class="acceptance-case" :class="{ active: item.caseId === admin.acceptance.selectedCaseId, failed: item.passed === false }" @click="admin.refreshAcceptanceCase(item.caseId)">
          <div><strong>{{ item.caseId }}</strong><small>{{ item.description || item.worldId || item.summaryPath }}</small></div>
          <span class="badge" :class="item.passed === false ? 'danger' : ''">{{ item.passed === false ? 'Failed' : item.passed === true ? 'Passed' : 'Unknown' }}</span>
          <p>Frame {{ item.finalFrame }} / {{ item.finalTimeMs }}ms / Trace {{ item.traceNodeCount }}</p>
        </article>
        <p v-if="!admin.acceptanceBatch.value?.cases?.length" class="muted">尚未读取到 Scenario artifact；先通过 Unity/unit-test 或 CI 生成 batch_summary.json 与 *_trace.jsonl。</p>
        <p v-else-if="!admin.acceptanceFilteredCases.value.length" class="muted">当前过滤条件下没有匹配的 case。</p>
      </div>
      <div class="acceptance-detail">
        <h3>Case Summary</h3>
        <div v-if="admin.acceptanceCase.value" class="status-box"><strong>{{ admin.acceptanceCase.value.caseId }}</strong><span>{{ admin.acceptanceCase.value.summaryPath }}</span><span>{{ admin.acceptanceCase.value.tracePath }}</span></div>
        <ul class="diagnostic-list warnings"><li v-for="warning in [...(admin.acceptanceBatch.value?.warnings || []), ...(admin.acceptanceCase.value?.warnings || [])]" :key="warning">{{ warning }}</li><li v-if="!admin.acceptanceBatch.value?.warnings?.length && !admin.acceptanceCase.value?.warnings?.length">暂无 artifact 告警。</li></ul>
        <h3>断言分组</h3>
        <div class="assertion-groups">
          <article v-for="group in admin.acceptanceAssertionGroups.value" :key="group.key" class="assertion-group">
            <strong>{{ group.title }}</strong>
            <ul><li v-for="item in group.items" :key="item">{{ item }}</li></ul>
          </article>
        </div>
        <SkillTraceVisualizer title="统一 Trace Tree" :nodes="admin.acceptanceAnalysisFilteredFlat.value" :total-node-count="admin.acceptanceAnalysisFlat.value.length" :selected-node="admin.selectedAcceptanceAnalysisNode.value" :timeline="admin.acceptanceAnalysisTimeline.value" :filter="admin.acceptanceAnalysisFilter" :filter-options="admin.acceptanceAnalysisFilterOptions.value" :entity-relations="admin.acceptanceAnalysisEntityRelations.value" empty-text="选择 case 后显示根据统一 node projection 构建的 trace tree。" />
        <h3>Trace JSONL 预览</h3>
        <div class="trace-preview"><article v-for="(record, index) in admin.acceptanceTracePreview.value" :key="index" class="trace-record"><strong>#{{ index + 1 }}</strong><pre>{{ JSON.stringify(record, null, 2) }}</pre></article><p v-if="!admin.acceptanceTracePreview.value.length" class="muted">选择 case 后显示 trace 记录预览。</p></div>
        <h3>执行入口边界</h3>
        <div v-if="admin.acceptanceRunPlan.value" class="run-boundary">
          <div class="status-box"><strong>{{ admin.acceptanceRunPlan.value.allowed ? '可执行' : '只读模式' }} / {{ admin.acceptanceRunPlan.value.executionMode }}</strong><span>{{ admin.acceptanceRunPlan.value.message }}</span><span>ArtifactDirectory: {{ admin.acceptanceRunPlan.value.artifactDirectory }}</span><span>Admin Request: {{ admin.acceptanceRunPlan.value.canRequestFromAdmin ? 'enabled' : 'disabled' }}</span></div>
          <div class="run-boundary-grid">
            <article v-for="strategy in admin.acceptanceRunPlan.value.strategies" :key="strategy.id" class="run-boundary-card"><div><strong>{{ strategy.displayName }}</strong><span class="badge">{{ strategy.status }}</span></div><p>{{ strategy.description }}</p><small>{{ strategy.boundary }}</small></article>
          </div>
          <h4>Allow-listed Scripts / Jobs</h4>
          <article v-for="script in admin.acceptanceRunPlan.value.allowedScripts" :key="script.id" class="allowed-script"><div><strong>{{ script.displayName }}</strong><span class="badge" :class="script.exists ? '' : 'danger'">{{ script.exists ? 'Found' : 'Placeholder' }}</span></div><small>{{ script.id }} / {{ script.shell }} / {{ script.relativePath }}</small><p>Args: {{ script.arguments.join(' | ') }}</p><p>Produces: {{ script.produces.join(', ') }}</p></article>
          <div class="run-boundary-grid compact"><article><strong>Required Approvals</strong><ul><li v-for="item in admin.acceptanceRunPlan.value.requiredApprovals" :key="item">{{ item }}</li></ul></article><article><strong>Audit Fields</strong><ul><li v-for="item in admin.acceptanceRunPlan.value.auditFields" :key="item">{{ item }}</li></ul></article></div>
          <ul class="diagnostic-list"><li v-for="note in admin.acceptanceRunPlan.value.notes" :key="note">{{ note }}</li></ul>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import SkillTraceVisualizer from './SkillTraceVisualizer.vue';
import { useAdminConsoleStore } from '../stores/adminConsoleStore';

const admin = useAdminConsoleStore();
</script>
