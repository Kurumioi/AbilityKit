<template>
  <section id="skill-acceptance" class="card span-12 acceptance-panel">
    <div class="card-head"><div><p class="section-kicker">场景产物</p><h2>场景验收报告</h2></div><span class="badge">{{ admin.acceptanceBatch.value?.cases?.length || 0 }} 个用例</span></div>
    <div class="acceptance-toolbar row"><div><label>产物目录</label><input v-model="admin.acceptance.artifactDirectory" placeholder="artifacts/moba-acceptance" /></div><div><label>追踪上限</label><input v-model.number="admin.acceptance.traceLimit" type="number" /></div></div>
    <div class="acceptance-filters row"><div><label>搜索用例 / 描述 / 路径</label><input v-model="admin.acceptance.searchText" placeholder="例如：projectile、damage、case id" /></div><div><label>状态过滤</label><select v-model="admin.acceptance.statusFilter"><option value="all">全部</option><option value="failed">失败优先排查</option><option value="passed">仅通过</option><option value="unknown">未知</option></select></div><div><label>排序</label><select v-model="admin.acceptance.sortKey"><option value="caseId">用例 ID</option><option value="failedFirst">失败优先</option><option value="duration">耗时</option><option value="trace">追踪数量</option><option value="status">状态</option></select></div></div>
    <div class="actions action-bar"><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshAcceptanceArtifacts">刷新场景产物</button><button class="secondary" :disabled="admin.busy.value" @click="admin.refreshAcceptanceRunPlan">查看执行入口边界</button></div>
    <div class="ops-status acceptance-summary"><div><span>产物目录</span><strong>{{ admin.acceptanceBatch.value?.artifactDirectory || admin.acceptance.artifactDirectory }}</strong></div><div><span>批次摘要</span><strong>{{ admin.acceptanceBatch.value?.hasBatchSummary ? '已找到' : '缺失' }}</strong></div><div><span>通过</span><strong>{{ admin.acceptancePassedCount.value }}</strong></div><div><span>失败</span><strong>{{ admin.acceptanceFailedCount.value }}</strong></div><div><span>未知</span><strong>{{ admin.acceptanceUnknownCount.value }}</strong></div><div><span>过滤后</span><strong>{{ admin.acceptanceFilteredCases.value.length }}</strong></div></div>
    <div class="acceptance-layout">
      <div class="acceptance-case-list">
        <h3>用例列表</h3>
        <article v-for="item in admin.acceptanceFilteredCases.value" :key="item.caseId" class="acceptance-case" :class="{ active: item.caseId === admin.acceptance.selectedCaseId, failed: item.passed === false }" @click="admin.refreshAcceptanceCase(item.caseId)">
          <div><strong>{{ item.caseId }}</strong><small>{{ item.description || item.worldId || item.summaryPath }}</small></div>
          <span class="badge" :class="item.passed === false ? 'danger' : ''">{{ item.passed === false ? '失败' : item.passed === true ? '通过' : '未知' }}</span>
          <p>帧 {{ item.finalFrame }} / {{ item.finalTimeMs }}ms / 追踪 {{ item.traceNodeCount }}</p>
        </article>
        <p v-if="!admin.acceptanceBatch.value?.cases?.length" class="muted">尚未读取到场景产物；先通过 Unity、单元测试或 CI 生成 batch_summary.json 与 *_trace.jsonl。</p>
        <p v-else-if="!admin.acceptanceFilteredCases.value.length" class="muted">当前过滤条件下没有匹配的用例。</p>
      </div>
      <div class="acceptance-detail">
        <h3>用例摘要</h3>
        <div v-if="admin.acceptanceCase.value" class="status-box"><strong>{{ admin.acceptanceCase.value.caseId }}</strong><span>{{ admin.acceptanceCase.value.summaryPath }}</span><span>{{ admin.acceptanceCase.value.tracePath }}</span></div>
        <ul class="diagnostic-list warnings"><li v-for="warning in [...(admin.acceptanceBatch.value?.warnings || []), ...(admin.acceptanceCase.value?.warnings || [])]" :key="warning">{{ warning }}</li><li v-if="!admin.acceptanceBatch.value?.warnings?.length && !admin.acceptanceCase.value?.warnings?.length">暂无 artifact 告警。</li></ul>
        <h3>断言分组</h3>
        <div class="assertion-groups">
          <article v-for="group in admin.acceptanceAssertionGroups.value" :key="group.key" class="assertion-group">
            <strong>{{ group.title }}</strong>
            <ul><li v-for="item in group.items" :key="item">{{ item }}</li></ul>
          </article>
        </div>
        <SkillTraceVisualizer title="统一追踪树" :nodes="admin.acceptanceAnalysisFilteredFlat.value" :total-node-count="admin.acceptanceAnalysisFlat.value.length" :selected-node="admin.selectedAcceptanceAnalysisNode.value" :timeline="admin.acceptanceAnalysisTimeline.value" :filter="admin.acceptanceAnalysisFilter" :filter-options="admin.acceptanceAnalysisFilterOptions.value" :entity-relations="admin.acceptanceAnalysisEntityRelations.value" empty-text="选择用例后显示根据统一节点投影构建的追踪树。" @select-node="admin.selectAcceptanceAnalysisNode" />
        <h3>追踪 JSONL 预览</h3>
        <div class="trace-preview"><article v-for="(record, index) in admin.acceptanceTracePreview.value" :key="index" class="trace-record"><strong>#{{ index + 1 }}</strong><pre>{{ JSON.stringify(record, null, 2) }}</pre></article><p v-if="!admin.acceptanceTracePreview.value.length" class="muted">选择用例后显示追踪记录预览。</p></div>
        <h3>执行入口边界</h3>
        <div v-if="admin.acceptanceRunPlan.value" class="run-boundary">
          <div class="status-box"><strong>{{ admin.acceptanceRunPlan.value.allowed ? '可执行' : '只读模式' }} / {{ admin.acceptanceRunPlan.value.executionMode }}</strong><span>{{ admin.acceptanceRunPlan.value.message }}</span><span>产物目录：{{ admin.acceptanceRunPlan.value.artifactDirectory }}</span><span>后台请求：{{ admin.acceptanceRunPlan.value.canRequestFromAdmin ? '已启用' : '已禁用' }}</span></div>
          <div class="run-boundary-grid">
            <article v-for="strategy in admin.acceptanceRunPlan.value.strategies" :key="strategy.id" class="run-boundary-card"><div><strong>{{ strategy.displayName }}</strong><span class="badge">{{ strategy.status }}</span></div><p>{{ strategy.description }}</p><small>{{ strategy.boundary }}</small></article>
          </div>
          <h4>白名单脚本 / 作业</h4>
          <article v-for="script in admin.acceptanceRunPlan.value.allowedScripts" :key="script.id" class="allowed-script"><div><strong>{{ script.displayName }}</strong><span class="badge" :class="script.exists ? '' : 'danger'">{{ script.exists ? '已找到' : '占位' }}</span></div><small>{{ script.id }} / {{ script.shell }} / {{ script.relativePath }}</small><p>参数：{{ script.arguments.join(' | ') }}</p><p>产物：{{ script.produces.join(', ') }}</p></article>
          <div class="run-boundary-grid compact"><article><strong>必需审批</strong><ul><li v-for="item in admin.acceptanceRunPlan.value.requiredApprovals" :key="item">{{ item }}</li></ul></article><article><strong>审计字段</strong><ul><li v-for="item in admin.acceptanceRunPlan.value.auditFields" :key="item">{{ item }}</li></ul></article></div>
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
