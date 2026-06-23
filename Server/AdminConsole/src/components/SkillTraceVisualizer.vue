<template>
  <div class="skill-trace-visualizer">
    <section class="trace-filter-panel">
      <div class="trace-section-head">
        <h3>{{ title }} Filter</h3>
        <span class="badge">{{ nodes.length }} / {{ totalNodeCount }} Nodes</span>
      </div>
      <div class="trace-filter-grid">
        <div><label>全文搜索</label><input v-model="filter.searchText" placeholder="kind / failure / nodeId / contextId / raw payload" /></div>
        <div><label>Severity</label><select v-model="filter.severity"><option value="all">全部</option><option v-for="item in filterOptions.severities" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>Stage</label><select v-model="filter.stage"><option value="all">全部</option><option v-for="item in filterOptions.stages" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>Kind</label><select v-model="filter.kind"><option value="all">全部</option><option v-for="item in filterOptions.kinds" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>实体类型</label><select v-model="filter.entityKind"><option value="all">全部</option><option v-for="item in filterOptions.entityKinds" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>ActorId</label><input v-model="filter.actorId" placeholder="source / target / actor" /></div>
        <div><label>SkillId</label><input v-model="filter.skillId" placeholder="技能配置" /></div>
        <div><label>ConfigId</label><input v-model="filter.configId" placeholder="effect / action / buff" /></div>
        <div><label>RootId</label><select v-model="filter.rootId"><option value="">全部</option><option v-for="item in filterOptions.rootIds" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>ContextId</label><input v-model="filter.contextId" placeholder="node / parent / root / owner" /></div>
        <label class="trace-check"><input v-model="filter.onlyFailures" type="checkbox" /> 仅看失败/异常链路</label>
      </div>
    </section>

    <div class="trace-visualizer-grid">
      <section class="trace-visualizer-tree">
        <div class="trace-section-head"><h3>{{ title }}</h3><span class="badge">Filtered {{ nodes.length }}</span></div>
        <div class="trace-tree">
          <article v-for="node in nodes" :key="`${node.source}-${node.nodeId}`" class="trace-tree-node analysis-node" :class="{ danger: node.severity === 'error' || node.severity === 'failed' }" :style="{ '--depth': node.depth }">
            <span class="trace-branch"></span>
            <div><strong>{{ node.label }}</strong><small>{{ node.stage }} / {{ node.entityKind }} / node {{ node.nodeId }} / parent {{ node.parentId || '-' }} / root {{ node.rootId || '-' }}</small></div>
            <span class="badge" :class="node.severity === 'error' || node.severity === 'failed' ? 'danger' : ''">F{{ node.frame }} / {{ node.timeMs }}ms</span>
            <p>{{ node.summary }}<template v-if="node.failure"> / failure={{ node.failure }}</template></p>
          </article>
          <p v-if="!nodes.length" class="muted">{{ emptyText }}</p>
        </div>
      </section>
      <section class="trace-inspector">
        <div class="trace-section-head"><h3>Inspector</h3><span class="badge">{{ selectedNode?.source || 'none' }}</span></div>
        <div v-if="selectedNode" class="inspector-card">
          <strong>{{ selectedNode.label }}</strong>
          <div class="inspector-grid">
            <span>nodeId</span><code>{{ selectedNode.nodeId }}</code>
            <span>stage</span><code>{{ selectedNode.stage }}</code>
            <span>kind</span><code>{{ selectedNode.kind }}</code>
            <span>entity</span><code>{{ selectedNode.entityKind }} / {{ selectedNode.entityKey }}</code>
            <span>status</span><code>{{ selectedNode.status }}</code>
            <span>actorId</span><code>{{ selectedNode.actorId ?? '-' }}</code>
            <span>sourceActor</span><code>{{ selectedNode.sourceActorId ?? '-' }}</code>
            <span>targetActor</span><code>{{ selectedNode.targetActorId ?? '-' }}</code>
            <span>skillId</span><code>{{ selectedNode.skillId ?? '-' }}</code>
            <span>configId</span><code>{{ selectedNode.configId ?? '-' }}</code>
            <span>sourceContext</span><code>{{ selectedNode.sourceContextId ?? '-' }}</code>
            <span>ownerContext</span><code>{{ selectedNode.ownerContextId ?? '-' }}</code>
            <span>rootContext</span><code>{{ selectedNode.rootContextId ?? '-' }}</code>
            <span>source</span><code>{{ selectedNode.source }}</code>
          </div>
          <p v-if="selectedNode.failure" class="failure-text">{{ selectedNode.failure }}</p>
          <pre>{{ JSON.stringify(selectedNode.raw, null, 2) }}</pre>
        </div>
        <p v-else class="muted">暂无可检查节点；选择带 traceRecords 的 Scenario case 或等待 runtime trace sink。</p>
      </section>
    </div>

    <section class="trace-entity-panel">
      <div class="trace-section-head"><h3>战斗实体关联</h3><span class="badge">{{ entityRelations.length }} Owners</span></div>
      <div class="entity-relation-grid">
        <article v-for="group in entityRelations" :key="group.id" class="entity-group" :class="{ danger: group.failureCount > 0 }">
          <div class="entity-group-head"><strong>{{ group.label }}</strong><span class="badge" :class="group.failureCount > 0 ? 'danger' : ''">{{ group.totalNodes }} nodes / {{ group.failureCount }} failures</span></div>
          <small>Actor {{ group.actorId ?? '-' }} / Root {{ group.rootId ?? '-' }}</small>
          <div class="entity-relation-list">
            <div v-for="relation in group.relations" :key="relation.id" class="entity-relation-row" :class="{ danger: relation.failureCount > 0 }">
              <span class="entity-kind">{{ relation.entityKind }}</span>
              <div><strong>{{ relation.entityKey }}</strong><small>F{{ relation.firstFrame }} → F{{ relation.lastFrame }} / {{ relation.nodeCount }} nodes</small></div>
              <span class="badge" :class="relation.failureCount > 0 ? 'danger' : ''">{{ relation.failureCount }}</span>
            </div>
          </div>
        </article>
        <p v-if="!entityRelations.length" class="muted">暂无可关联实体；过滤条件过窄或 trace sink 尚未投影 source/root/owner context。</p>
      </div>
    </section>

    <section class="trace-timeline-placeholder">
      <div class="trace-section-head"><h3>Timeline</h3><span class="badge">{{ timeline.length }} Events</span></div>
      <div class="timeline-lanes">
        <article v-for="event in timeline" :key="event.id" class="timeline-event" :class="{ danger: event.severity === 'error' || event.severity === 'failed' }">
          <span class="badge">{{ event.lane }}</span>
          <div><strong>{{ event.label }}</strong><small>F{{ event.frame }} / {{ event.timeMs }}ms / {{ event.source }}</small></div>
        </article>
        <p v-if="!timeline.length" class="muted">时间轴 schema 已预留；runtime live trace 接入后将按 frame/timeMs 展示 cast、trigger、effect、assertion lane。</p>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import type { SkillAnalysisEntityGroupProjection, SkillAnalysisFilterOptions, SkillAnalysisFilterState, SkillAnalysisFlatNodeProjection, SkillAnalysisTimelineEventProjection } from '../types';

defineProps<{
  title: string;
  nodes: SkillAnalysisFlatNodeProjection[];
  totalNodeCount: number;
  selectedNode: SkillAnalysisFlatNodeProjection | null;
  timeline: SkillAnalysisTimelineEventProjection[];
  filter: SkillAnalysisFilterState;
  filterOptions: SkillAnalysisFilterOptions;
  entityRelations: SkillAnalysisEntityGroupProjection[];
  emptyText: string;
}>();
</script>
