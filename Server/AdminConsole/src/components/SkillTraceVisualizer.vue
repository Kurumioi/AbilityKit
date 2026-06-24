<template>
  <div class="skill-trace-visualizer">
    <section class="trace-filter-panel">
      <div class="trace-section-head">
        <h3>{{ title }}过滤</h3>
        <span class="badge">{{ nodes.length }} / {{ totalNodeCount }} 个节点</span>
      </div>
      <div class="trace-filter-grid">
        <div><label>全文搜索</label><input v-model="filter.searchText" placeholder="类型 / 失败原因 / 节点 ID / 上下文 ID / 原始载荷" /></div>
        <div><label>严重级别</label><select v-model="filter.severity"><option value="all">全部</option><option v-for="item in filterOptions.severities" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>阶段</label><select v-model="filter.stage"><option value="all">全部</option><option v-for="item in filterOptions.stages" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>类型</label><select v-model="filter.kind"><option value="all">全部</option><option v-for="item in filterOptions.kinds" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>分析域</label><select v-model="filter.entityKind"><option value="all">全部</option><option v-for="item in filterOptions.entityKinds" :key="item" :value="item">{{ domainOptionLabel(item) }}</option></select></div>
        <div><label>角色 ID</label><input v-model="filter.actorId" placeholder="来源 / 目标 / 角色" /></div>
        <div><label>技能 ID</label><input v-model="filter.skillId" placeholder="技能配置" /></div>
        <div><label>配置 ID</label><input v-model="filter.configId" placeholder="效果 / 行为 / Buff" /></div>
        <div><label>根节点 ID</label><select v-model="filter.rootId"><option value="">全部</option><option v-for="item in filterOptions.rootIds" :key="item" :value="item">{{ item }}</option></select></div>
        <div><label>上下文 ID</label><input v-model="filter.contextId" placeholder="节点 / 父级 / 根节点 / 所有者" /></div>
        <label class="trace-check"><input v-model="filter.onlyFailures" type="checkbox" /> 仅看失败/异常链路</label>
      </div>
    </section>

    <section class="trace-domain-panel">
      <div class="trace-section-head">
        <h3>技能分析导览</h3>
        <span class="badge">{{ domainOverview.length }} 个分析域</span>
      </div>
      <p class="muted">按主动/被动技能、触发源、条件、行为、Buff、投射物、位移、伤害和表现事件重新组织追踪节点，优先定位“为什么触发、条件是否满足、后续行为做了什么”。</p>
      <div class="trace-domain-grid">
        <article v-for="domain in domainOverview" :key="domain.domain" class="trace-domain-card" :class="{ danger: domain.failureCount > 0, active: selectedNode?.domain === domain.domain }" role="button" tabindex="0" @click="selectDomain(domain.domain)" @keydown.enter.prevent="selectDomain(domain.domain)" @keydown.space.prevent="selectDomain(domain.domain)">
          <strong>{{ domain.label }}</strong>
          <small>{{ domain.lane }}</small>
          <div><span>{{ domain.count }} 节点</span><span :class="domain.failureCount > 0 ? 'danger-text' : ''">{{ domain.failureCount }} 异常</span></div>
        </article>
      </div>
      <div v-if="failureInsights.length" class="trace-failure-strip">
        <strong>异常热点</strong>
        <button v-for="node in failureInsights" :key="`${node.source}-${node.nodeId}`" class="secondary trace-chip-button" @click="selectNode(node)">{{ node.domainLabel }} · F{{ node.frame }} · {{ truncateLabel(node.failure || node.displayName, 22) }}</button>
      </div>
    </section>

    <section class="trace-flow-panel">
      <div class="trace-section-head"><h3>技能流图</h3><span class="badge">{{ flowGraph.nodes.length }} 个节点 / {{ flowGraph.edges.length }} 条连线</span></div>
      <div class="trace-flow-canvas" :style="{ minHeight: `${flowGraph.height}px` }">
        <svg class="trace-flow-svg" :viewBox="`0 0 ${flowGraph.width} ${flowGraph.height}`" preserveAspectRatio="xMinYMin meet" role="img" aria-label="技能执行流 SVG 图">
          <defs>
            <marker id="trace-arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto" markerUnits="strokeWidth">
              <path d="M0,0 L0,6 L8,3 z" class="trace-flow-arrow" />
            </marker>
          </defs>
          <g class="trace-flow-grid-lines">
            <line v-for="lane in flowGraph.lanes" :key="lane.stage" :x1="20" :x2="flowGraph.width - 20" :y1="lane.y" :y2="lane.y" />
            <text v-for="lane in flowGraph.lanes" :key="`${lane.stage}-label`" :x="24" :y="lane.y - 10">{{ lane.stage }}</text>
          </g>
          <g class="trace-flow-edges">
            <path v-for="edge in flowGraph.edges" :key="edge.id" :d="edge.path" :class="{ danger: edge.danger }" marker-end="url(#trace-arrow)" />
          </g>
          <g class="trace-flow-nodes">
            <g v-for="item in flowGraph.nodes" :key="item.key" class="trace-flow-node" :class="{ active: selectedNodeKey === buildNodeKey(item.node), danger: item.danger }" role="button" tabindex="0" @click="selectNode(item.node)" @keydown.enter.prevent="selectNode(item.node)" @keydown.space.prevent="selectNode(item.node)">
              <rect :x="item.x" :y="item.y" :width="flowGraph.nodeWidth" :height="flowGraph.nodeHeight" rx="14" />
              <text class="trace-flow-title" :x="item.x + 14" :y="item.y + 24">{{ truncateLabel(item.node.displayName || item.node.label, 24) }}</text>
              <text class="trace-flow-meta" :x="item.x + 14" :y="item.y + 44">{{ truncateLabel(item.node.domainLabel || item.node.configLabel || item.node.kind, 30) }}</text>
              <text class="trace-flow-meta" :x="item.x + 14" :y="item.y + 63">{{ truncateLabel(item.node.conditionLabel || item.node.actionLabel || `${item.node.sourceActorLabel || item.node.actorLabel || '角色 -'} → ${item.node.targetActorLabel || '-'}`, 32) }}</text>
            </g>
          </g>
        </svg>
        <p v-if="!flowGraph.nodes.length" class="muted">{{ emptyText }}</p>
      </div>
    </section>

    <div class="trace-visualizer-grid">
      <section class="trace-visualizer-tree">
        <div class="trace-section-head"><h3>{{ title }}</h3><span class="badge">已过滤 {{ nodes.length }}</span></div>
        <div class="trace-tree">
          <article v-for="node in nodes" :key="`${node.source}-${node.nodeId}`" class="trace-tree-node analysis-node" :class="{ active: selectedNodeKey === buildNodeKey(node), danger: node.severity === 'error' || node.severity === 'failed' }" :style="{ '--depth': node.depth }" role="button" tabindex="0" @click="selectNode(node)" @keydown.enter.prevent="selectNode(node)" @keydown.space.prevent="selectNode(node)">
            <span class="trace-branch"></span>
            <div><strong><span class="domain-pill">{{ node.domainLabel }}</span>{{ node.displayName || node.label }}</strong><small>{{ node.triggerLabel || node.detailLabel || `${node.stage} / ${node.entityKind}` }} / 节点 {{ node.nodeId }} / 父级 {{ node.parentId || '-' }} / 根节点 {{ node.rootId || '-' }}</small></div>
            <span class="badge" :class="node.severity === 'error' || node.severity === 'failed' ? 'danger' : ''">F{{ node.frame }} / {{ node.timeMs }}ms</span>
            <p>{{ node.conditionLabel || node.actionLabel || node.summary }}<template v-if="node.failure"> / 失败={{ node.failure }}</template></p>
          </article>
          <p v-if="!nodes.length" class="muted">{{ emptyText }}</p>
        </div>
      </section>
      <section class="trace-inspector">
        <div class="trace-section-head"><h3>检查器</h3><span class="badge">{{ selectedNode?.source || '无' }}</span></div>
        <div v-if="selectedNode" class="inspector-card">
          <strong>{{ selectedNode.displayName || selectedNode.label }}</strong>
          <p class="muted">{{ selectedNode.detailLabel || selectedNode.summary }}</p>
          <div class="inspector-section">
            <h4>归因 / 触发链路</h4>
            <div class="inspector-grid">
              <span>分析域</span><code>{{ selectedNode.domainLabel || selectedNode.domain }}</code>
              <span>泳道</span><code>{{ selectedNode.laneLabel }}</code>
              <span>触发源</span><code>{{ selectedNode.triggerLabel || '-' }}</code>
              <span>条件判定</span><code>{{ selectedNode.conditionLabel || '-' }}</code>
              <span>后续行为</span><code>{{ selectedNode.actionLabel || '-' }}</code>
            </div>
          </div>
          <div class="inspector-section">
            <h4>运行时上下文</h4>
            <div class="inspector-grid">
            <span>节点 ID</span><code>{{ selectedNode.nodeId }}</code>
            <span>阶段</span><code>{{ selectedNode.stage }}</code>
            <span>类型</span><code>{{ selectedNode.kind }}</code>
            <span>实体</span><code>{{ selectedNode.runtimeLabel || `${selectedNode.entityKind} / ${selectedNode.entityKey}` }}</code>
            <span>状态</span><code>{{ selectedNode.status }}</code>
            <span>角色</span><code>{{ selectedNode.actorLabel || selectedNode.actorId || '-' }}</code>
            <span>来源角色</span><code>{{ selectedNode.sourceActorLabel || selectedNode.sourceActorId || '-' }}</code>
            <span>目标角色</span><code>{{ selectedNode.targetActorLabel || selectedNode.targetActorId || '-' }}</code>
            <span>技能</span><code>{{ selectedNode.skillId ? `技能 #${selectedNode.skillId}` : '-' }}</code>
            <span>配置</span><code>{{ selectedNode.configLabel || selectedNode.configId || '-' }}</code>
            <span>来源上下文</span><code>{{ selectedNode.sourceContextId ?? '-' }}</code>
            <span>所有者上下文</span><code>{{ selectedNode.ownerContextId ?? '-' }}</code>
            <span>根上下文</span><code>{{ selectedNode.rootContextId ?? '-' }}</code>
            <span>数据源</span><code>{{ selectedNode.source }}</code>
            </div>
          </div>
          <p v-if="selectedNode.failure" class="failure-text">{{ selectedNode.failure }}</p>
          <details class="raw-details" open><summary>原始追踪数据</summary><pre>{{ JSON.stringify(selectedNode.raw, null, 2) }}</pre></details>
        </div>
        <p v-else class="muted">暂无可检查节点；请选择带追踪记录的场景用例，或等待运行态追踪接入。</p>
      </section>
    </div>

    <section class="trace-entity-panel">
      <div class="trace-section-head"><h3>战斗实体关联</h3><span class="badge">{{ entityRelations.length }} 个所有者</span></div>
      <div class="entity-relation-grid">
        <article v-for="group in entityRelations" :key="group.id" class="entity-group" :class="{ danger: group.failureCount > 0 }">
          <div class="entity-group-head"><strong>{{ group.label }}</strong><span class="badge" :class="group.failureCount > 0 ? 'danger' : ''">{{ group.totalNodes }} 个节点 / {{ group.failureCount }} 个失败</span></div>
          <small>角色 {{ group.actorId ?? '-' }} / 根节点 {{ group.rootId ?? '-' }}</small>
          <div class="entity-relation-list">
          <div v-for="relation in group.relations" :key="relation.id" class="entity-relation-row" :class="{ danger: relation.failureCount > 0 }" role="button" tabindex="0" @click="selectRelation(relation)" @keydown.enter.prevent="selectRelation(relation)" @keydown.space.prevent="selectRelation(relation)">
            <span class="entity-kind">{{ relation.entityKind }}</span>
            <div><strong>{{ relation.label }}</strong><small>{{ relation.entityKey }} / F{{ relation.firstFrame }} → F{{ relation.lastFrame }} / {{ relation.nodeCount }} 个节点</small></div>
              <span class="badge" :class="relation.failureCount > 0 ? 'danger' : ''">{{ relation.failureCount }}</span>
            </div>
          </div>
        </article>
        <p v-if="!entityRelations.length" class="muted">暂无可关联实体；过滤条件过窄或追踪接收端尚未投影来源、根节点、所有者上下文。</p>
      </div>
    </section>

    <section class="trace-timeline-placeholder">
      <div class="trace-section-head"><h3>时间轴</h3><span class="badge">{{ timeline.length }} 个事件</span></div>
      <div class="timeline-canvas">
        <svg v-if="timelineGraph.events.length" class="timeline-svg" :viewBox="`0 0 ${timelineGraph.width} ${timelineGraph.height}`" preserveAspectRatio="xMinYMin meet" role="img" aria-label="技能执行时间轴 SVG 图">
          <g class="timeline-axis">
            <line :x1="timelineGraph.marginX" :x2="timelineGraph.width - timelineGraph.marginX" :y1="timelineGraph.axisY" :y2="timelineGraph.axisY" />
            <g v-for="tick in timelineGraph.ticks" :key="tick.frame">
              <line :x1="tick.x" :x2="tick.x" :y1="timelineGraph.axisY - 6" :y2="timelineGraph.height - timelineGraph.marginY + 6" />
              <text :x="tick.x" :y="timelineGraph.axisY - 10">F{{ tick.frame }}</text>
            </g>
          </g>
          <g class="timeline-lane-lines">
            <line v-for="lane in timelineGraph.lanes" :key="`${lane.name}-line`" :x1="timelineGraph.marginX" :x2="timelineGraph.width - timelineGraph.marginX" :y1="lane.y" :y2="lane.y" />
          </g>
          <g class="timeline-lane-labels">
            <text v-for="lane in timelineGraph.lanes" :key="lane.name" :x="18" :y="lane.y + 5">{{ truncateLabel(lane.name, 13) }}</text>
          </g>
          <g class="timeline-event-nodes">
            <g v-for="event in timelineGraph.events" :key="event.id" class="timeline-event-node" :class="{ active: event.nodeId && selectedNode?.nodeId === event.nodeId, danger: event.danger }" role="button" tabindex="0" @click="selectTimelineEvent(event.event)" @keydown.enter.prevent="selectTimelineEvent(event.event)" @keydown.space.prevent="selectTimelineEvent(event.event)">
              <line class="timeline-event-stem" :x1="event.x" :x2="event.x" :y1="timelineGraph.axisY" :y2="event.y" />
              <circle :cx="event.x" :cy="event.y" :r="10" />
              <rect :x="event.x + 16" :y="event.y - 18" :width="timelineGraph.eventWidth" :height="36" rx="11" />
              <text class="timeline-event-title" :x="event.x + 28" :y="event.y - 2">{{ truncateLabel(event.event.label, 26) }}</text>
              <text class="timeline-event-meta" :x="event.x + 28" :y="event.y + 12">{{ event.event.source }} · {{ event.event.timeMs }}ms</text>
            </g>
          </g>
        </svg>
        <p v-else class="muted">时间轴结构已预留；运行态实时追踪接入后将按主动释放、触发条件、行为计划、Buff、投射物、位移、伤害、表现等业务泳道展示。</p>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { SkillAnalysisEntityGroupProjection, SkillAnalysisEntityRelationProjection, SkillAnalysisFilterOptions, SkillAnalysisFilterState, SkillAnalysisFlatNodeProjection, SkillAnalysisTimelineEventProjection } from '../types';

const props = defineProps<{
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

const emit = defineEmits<{
  (event: 'select-node', node: SkillAnalysisFlatNodeProjection): void;
}>();

const selectedNodeKey = computed(() => props.selectedNode ? buildNodeKey(props.selectedNode) : '');

const domainOverview = computed(() => {
  const groups = new Map<string, { domain: string; label: string; lane: string; count: number; failureCount: number }>();
  for (const node of props.nodes) {
    const domain = node.domain || node.entityKind || 'context';
    const current = groups.get(domain) || { domain, label: node.domainLabel || domain, lane: node.laneLabel || node.stage, count: 0, failureCount: 0 };
    current.count += 1;
    if (isDangerNode(node)) current.failureCount += 1;
    groups.set(domain, current);
  }
  return [...groups.values()].sort((a, b) => b.failureCount - a.failureCount || b.count - a.count || a.label.localeCompare(b.label));
});

const failureInsights = computed(() => props.nodes.filter(isDangerNode).slice(0, 5));

interface FlowGraphNode {
  key: string;
  node: SkillAnalysisFlatNodeProjection;
  x: number;
  y: number;
  danger: boolean;
}

interface FlowGraphEdge {
  id: string;
  path: string;
  danger: boolean;
}

interface FlowGraphLane {
  stage: string;
  y: number;
}

interface TimelineGraphLane {
  name: string;
  y: number;
}

interface TimelineGraphTick {
  frame: number;
  x: number;
}

interface TimelineGraphEvent {
  id: string;
  event: SkillAnalysisTimelineEventProjection;
  nodeId: string | null;
  x: number;
  y: number;
  danger: boolean;
}

const flowGraph = computed(() => {
  const nodeWidth = 210;
  const nodeHeight = 82;
  const columnGap = 86;
  const laneGap = 112;
  const marginX = 44;
  const marginY = 48;
  const laneOrder = Array.from(new Set(props.nodes.map(node => node.laneLabel || node.stage || 'unknown')));
  const laneIndex = new Map(laneOrder.map((stage, index) => [stage, index]));
  const depthSlots = new Map<number, number>();
  const positioned = props.nodes.map(node => {
    const depth = Math.max(0, node.depth || 0);
    const slot = depthSlots.get(depth) || 0;
    depthSlots.set(depth, slot + 1);
    const stageIndex = laneIndex.get(node.laneLabel || node.stage || 'unknown') || 0;
    const yOffset = slot * 10;
    return {
      key: buildNodeKey(node),
      node,
      x: marginX + depth * (nodeWidth + columnGap),
      y: marginY + stageIndex * laneGap + yOffset,
      danger: isDangerNode(node),
    } satisfies FlowGraphNode;
  });
  const byNodeId = new Map(positioned.map(item => [item.node.nodeId, item]));
  const edges = positioned.flatMap(item => {
    if (!item.node.parentId) return [];
    const parent = byNodeId.get(item.node.parentId);
    if (!parent) return [];
    const startX = parent.x + nodeWidth;
    const startY = parent.y + nodeHeight / 2;
    const endX = item.x;
    const endY = item.y + nodeHeight / 2;
    const midX = startX + Math.max(28, (endX - startX) / 2);
    return [{
      id: `${parent.key}->${item.key}`,
      path: `M ${startX} ${startY} C ${midX} ${startY}, ${midX} ${endY}, ${endX} ${endY}`,
      danger: parent.danger || item.danger,
    } satisfies FlowGraphEdge];
  });
  const width = Math.max(760, marginX * 2 + (Math.max(0, ...positioned.map(item => item.node.depth || 0)) + 1) * nodeWidth + Math.max(0, ...positioned.map(item => item.node.depth || 0)) * columnGap);
  const height = Math.max(260, marginY * 2 + Math.max(1, laneOrder.length) * laneGap + Math.max(0, ...Array.from(depthSlots.values())) * 10);
  const lanes = laneOrder.map((stage, index) => ({ stage, y: marginY + index * laneGap + nodeHeight / 2 } satisfies FlowGraphLane));
  return { nodes: positioned, edges, lanes, width, height, nodeWidth, nodeHeight };
});

const timelineGraph = computed(() => {
  const marginX = 90;
  const marginY = 44;
  const laneGap = 72;
  const timeline = props.timeline;
  const trackWidth = Math.max(680, Math.max(0, ...timeline.map(item => item.frame)) * 66);
  const laneOrder = Array.from(new Set(timeline.map(item => item.lane || 'runtime-event')));
  const laneIndex = new Map(laneOrder.map((lane, index) => [lane, index]));
  const minFrame = timeline.length > 0 ? Math.min(...timeline.map(item => item.frame)) : 0;
  const maxFrame = timeline.length > 0 ? Math.max(...timeline.map(item => item.frame)) : 0;
  const frameRange = Math.max(1, maxFrame - minFrame);
  const events = timeline.map((event, index) => {
    const lane = event.lane || 'runtime-event';
    const normalizedFrame = event.frame - minFrame;
    const x = marginX + (normalizedFrame / frameRange) * trackWidth;
    const y = marginY + (laneIndex.get(lane) || 0) * laneGap;
    return {
      id: event.id,
      event,
      nodeId: event.nodeId ?? null,
      x,
      y,
      danger: event.severity === 'error' || event.severity === 'failed'
    } satisfies TimelineGraphEvent;
  });
  const ticks = uniqueFrameTicks(timeline).map(frame => ({ frame, x: marginX + ((frame - minFrame) / frameRange) * trackWidth } satisfies TimelineGraphTick));
  const width = Math.max(820, marginX * 2 + trackWidth);
  const height = Math.max(220, marginY * 2 + Math.max(1, laneOrder.length) * laneGap + 46);
  const lanes = laneOrder.map((name, index) => ({ name, y: marginY + index * laneGap } satisfies TimelineGraphLane));
  return {
    events,
    ticks,
    lanes,
    width,
    height,
    marginX,
    marginY,
    axisY: marginY - 4,
    eventWidth: 170
  };
});

function buildNodeKey(node: Pick<SkillAnalysisFlatNodeProjection, 'source' | 'nodeId'>): string {
  return `${node.source}:${node.nodeId}`;
}

function isDangerNode(node: SkillAnalysisFlatNodeProjection): boolean {
  return node.severity === 'error' || node.severity === 'failed' || Boolean(node.failure);
}

function selectNode(node: SkillAnalysisFlatNodeProjection): void {
  emit('select-node', node);
}

function selectDomain(domain: string): void {
  const firstFailure = props.nodes.find(node => node.domain === domain && isDangerNode(node));
  const firstNode = firstFailure || props.nodes.find(node => node.domain === domain);
  if (firstNode) selectNode(firstNode);
}

function domainOptionLabel(value: string): string {
  const first = props.nodes.find(node => node.domain === value || node.entityKind === value);
  return first?.domainLabel || value;
}

function selectRelation(relation: SkillAnalysisEntityRelationProjection): void {
  const firstFailure = relation.nodes.find(node => node.severity === 'error' || node.severity === 'failed' || Boolean(node.failure));
  const firstNode = firstFailure || relation.nodes[0];
  if (firstNode) selectNode(firstNode);
}

function selectTimelineEvent(event: SkillAnalysisTimelineEventProjection): void {
  if (!event.nodeId) return;
  const node = props.nodes.find(item => item.nodeId === event.nodeId && item.source === event.source) || props.nodes.find(item => item.nodeId === event.nodeId);
  if (node) selectNode(node);
}

function uniqueFrameTicks(events: SkillAnalysisTimelineEventProjection[]): number[] {
  return [...new Set(events.map(item => item.frame))].sort((a, b) => a - b).slice(0, 12);
}

function truncateLabel(value: string, maxLength: number): string {
  if (!value) return '';
  return value.length > maxLength ? `${value.slice(0, Math.max(0, maxLength - 1))}…` : value;
}
</script>
