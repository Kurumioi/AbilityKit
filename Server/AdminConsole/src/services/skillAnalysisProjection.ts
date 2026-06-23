import type { AdminSkillDiagnosticsEvents, SkillAnalysisEntityGroupProjection, SkillAnalysisEntityRelationProjection, SkillAnalysisFilterOptions, SkillAnalysisFilterState, SkillAnalysisFlatNodeProjection, SkillAnalysisNodeProjection, SkillAnalysisTimelineEventProjection } from '../types';
import { toNumber, toText } from './skillAcceptanceAnalysis';

export function createDefaultSkillAnalysisFilter(): SkillAnalysisFilterState {
  return {
    searchText: '',
    severity: 'all',
    stage: 'all',
    kind: 'all',
    entityKind: 'all',
    actorId: '',
    skillId: '',
    configId: '',
    rootId: '',
    contextId: '',
    onlyFailures: false
  };
}

export function inferSkillAnalysisStage(kind: string): string {
  const normalized = kind.toLowerCase();
  if (normalized.includes('effect') || normalized.includes('damage') || normalized.includes('buff') || normalized.includes('projectile') || normalized.includes('area')) return 'effect-execution';
  if (normalized.includes('assert') || normalized.includes('expect') || normalized.includes('result')) return 'assertion-result';
  if (normalized.includes('cast') || normalized.includes('skill')) return 'cast-pipeline';
  return 'trigger-lineage';
}

export function inferSkillAnalysisSeverity(record: Record<string, unknown>, status: string): string {
  const severity = toText(record.severity).toLowerCase();
  if (severity) return severity;
  const failed = record.passed === false || status === 'failed' || Boolean(record.failReason) || Boolean(record.failure);
  return failed ? 'error' : 'info';
}

export function inferSkillAnalysisEntityKind(kind: string, stage: string, record: Record<string, unknown>): string {
  const text = `${kind} ${stage} ${toText(record.runtimeKind)} ${toText(record.entityKind)} ${toText(record.eventType)}`.toLowerCase();
  if (text.includes('projectile')) return 'projectile';
  if (text.includes('area') || text.includes('aoe')) return 'area';
  if (text.includes('buff')) return 'buff';
  if (text.includes('damage')) return 'damage';
  if (text.includes('presentation')) return 'presentation';
  if (text.includes('action')) return 'effect-action';
  if (text.includes('effect')) return 'effect';
  if (text.includes('skill') || text.includes('cast')) return 'actor-skill';
  return 'context';
}

export function buildSkillAnalysisNodeProjection(record: Record<string, unknown>, source: string, caseId = ''): SkillAnalysisNodeProjection | null {
  const numericNodeId = toNumber(record.nodeId);
  if (numericNodeId <= 0) return null;

  const kind = toText(record.kind) || toText(record.eventType) || 'Unknown';
  const status = toText(record.status) || (record.isEnded === true ? 'ended' : 'running');
  const failure = toText(record.failReason) || toText(record.failure) || toText(record.reason) || null;
  const configId = toNumber(record.configId || record.runtimeConfigId || record.effectId || record.actionId);
  const sourceActorId = toNumber(record.sourceActorId || record.casterActorId);
  const targetActorId = toNumber(record.targetActorId);
  const actorId = toNumber(record.actorId || sourceActorId || targetActorId);
  const skillId = toNumber(record.skillId);
  const frame = toNumber(record.frame);
  const timeMs = toNumber(record.timeMs);
  const stage = toText(record.stage) || inferSkillAnalysisStage(kind);
  const entityKind = inferSkillAnalysisEntityKind(kind, stage, record);
  const sourceContextId = String(toNumber(record.sourceContextId || record.contextId || record.immediateContextId) || numericNodeId);
  const rootContextId = String(toNumber(record.rootContextId || record.rootId) || numericNodeId);
  const ownerContextId = String(toNumber(record.ownerContextId || record.parentContextId || record.rootId) || 0);
  const entityKey = buildEntityKey(entityKind, record, numericNodeId, configId, actorId, sourceContextId);
  const labelParts = [kind, configId > 0 ? `#${configId}` : '', skillId > 0 ? `skill ${skillId}` : ''].filter(Boolean);

  return {
    nodeId: String(numericNodeId),
    numericNodeId,
    rootId: String(toNumber(record.rootId) || numericNodeId),
    numericRootId: toNumber(record.rootId) || numericNodeId,
    parentId: String(toNumber(record.parentId) || 0),
    numericParentId: toNumber(record.parentId),
    kind,
    stage,
    label: labelParts.join(' '),
    configId: configId > 0 ? configId : null,
    frame,
    timeMs,
    actorId: actorId > 0 ? actorId : null,
    skillId: skillId > 0 ? skillId : null,
    sourceActorId: sourceActorId > 0 ? sourceActorId : null,
    targetActorId: targetActorId > 0 ? targetActorId : null,
    rootContextId,
    ownerContextId,
    sourceContextId,
    entityKind,
    entityKey,
    status,
    severity: inferSkillAnalysisSeverity(record, status),
    summary: buildNodeSummary(record, caseId),
    source,
    failure,
    raw: record,
    children: []
  };
}

export function buildSkillAnalysisTree(records: Record<string, unknown>[], source: string, caseId = ''): SkillAnalysisNodeProjection[] {
  const nodes = new Map<number, SkillAnalysisNodeProjection>();
  const roots: SkillAnalysisNodeProjection[] = [];

  const sortedRecords = [...records].sort((a, b) => {
    const rootCompare = toNumber(a.rootId) - toNumber(b.rootId);
    if (rootCompare !== 0) return rootCompare;
    const frameCompare = toNumber(a.frame) - toNumber(b.frame);
    if (frameCompare !== 0) return frameCompare;
    return toNumber(a.nodeId) - toNumber(b.nodeId);
  });

  for (const record of sortedRecords) {
    const node = buildSkillAnalysisNodeProjection(record, source, caseId);
    if (node) nodes.set(node.numericNodeId, node);
  }

  for (const node of nodes.values()) {
    const parent = node.numericParentId > 0 ? nodes.get(node.numericParentId) : null;
    if (parent) parent.children.push(node);
    else roots.push(node);
  }

  sortSkillAnalysisNodes(roots);
  return roots;
}

export function flattenSkillAnalysisTree(nodes: SkillAnalysisNodeProjection[], depth = 0): SkillAnalysisFlatNodeProjection[] {
  const result: SkillAnalysisFlatNodeProjection[] = [];
  for (const node of nodes) {
    result.push({ ...node, depth });
    if (node.children.length > 0) result.push(...flattenSkillAnalysisTree(node.children, depth + 1));
  }
  return result;
}

export function buildSkillAnalysisFilterOptions(nodes: SkillAnalysisFlatNodeProjection[]): SkillAnalysisFilterOptions {
  return {
    severities: uniqueSorted(nodes.map(x => x.severity)),
    stages: uniqueSorted(nodes.map(x => x.stage)),
    kinds: uniqueSorted(nodes.map(x => x.kind)),
    entityKinds: uniqueSorted(nodes.map(x => x.entityKind)),
    actorIds: uniqueNumbers(nodes.flatMap(x => [x.actorId || 0, x.sourceActorId || 0, x.targetActorId || 0])),
    skillIds: uniqueNumbers(nodes.map(x => x.skillId || 0)),
    configIds: uniqueNumbers(nodes.map(x => x.configId || 0)),
    rootIds: uniqueSorted(nodes.map(x => x.rootId))
  };
}

export function filterSkillAnalysisNodes(nodes: SkillAnalysisFlatNodeProjection[], filter: SkillAnalysisFilterState): SkillAnalysisFlatNodeProjection[] {
  return nodes.filter(node => nodeMatchesSkillAnalysisFilter(node, filter));
}

export function nodeMatchesSkillAnalysisFilter(node: SkillAnalysisFlatNodeProjection, filter: SkillAnalysisFilterState): boolean {
  if (filter.onlyFailures && !isFailureNode(node)) return false;
  if (!matchesOption(node.severity, filter.severity)) return false;
  if (!matchesOption(node.stage, filter.stage)) return false;
  if (!matchesOption(node.kind, filter.kind)) return false;
  if (!matchesOption(node.entityKind, filter.entityKind)) return false;
  if (!matchesNumberFilter([node.actorId, node.sourceActorId, node.targetActorId], filter.actorId)) return false;
  if (!matchesNumberFilter([node.skillId], filter.skillId)) return false;
  if (!matchesNumberFilter([node.configId], filter.configId)) return false;
  if (filter.rootId && node.rootId !== filter.rootId) return false;
  if (filter.contextId && ![node.nodeId, node.parentId, node.rootId, node.sourceContextId, node.rootContextId, node.ownerContextId].includes(filter.contextId)) return false;

  const text = filter.searchText.trim().toLowerCase();
  if (!text) return true;
  const haystack = [node.label, node.kind, node.stage, node.status, node.severity, node.summary, node.failure || '', node.entityKind, node.entityKey, node.nodeId, node.parentId, node.rootId, toText(node.raw)].join(' ').toLowerCase();
  return haystack.includes(text);
}

export function buildSkillAnalysisEntityRelations(nodes: SkillAnalysisFlatNodeProjection[]): SkillAnalysisEntityGroupProjection[] {
  const groups = new Map<string, SkillAnalysisEntityGroupProjection>();
  const relationBuckets = new Map<string, SkillAnalysisFlatNodeProjection[]>();

  for (const node of nodes) {
    const ownerKey = buildOwnerKey(node);
    const group = ensureEntityGroup(groups, ownerKey, node);
    group.totalNodes += 1;
    if (isFailureNode(node)) group.failureCount += 1;

    const relationKey = `${ownerKey}:${node.entityKind}:${node.entityKey}`;
    if (!relationBuckets.has(relationKey)) relationBuckets.set(relationKey, []);
    relationBuckets.get(relationKey)?.push(node);
  }

  for (const [bucketKey, bucketNodes] of relationBuckets.entries()) {
    const first = bucketNodes[0];
    const ownerKey = buildOwnerKey(first);
    const relation: SkillAnalysisEntityRelationProjection = {
      id: bucketKey,
      ownerKey,
      entityKind: first.entityKind,
      entityKey: first.entityKey,
      label: `${first.entityKind} / ${first.entityKey}`,
      nodeCount: bucketNodes.length,
      failureCount: bucketNodes.filter(isFailureNode).length,
      firstFrame: Math.min(...bucketNodes.map(x => x.frame)),
      lastFrame: Math.max(...bucketNodes.map(x => x.frame)),
      nodes: bucketNodes
    };
    groups.get(ownerKey)?.relations.push(relation);
  }

  return [...groups.values()]
    .map(group => ({ ...group, relations: group.relations.sort((a, b) => b.failureCount - a.failureCount || a.firstFrame - b.firstFrame || a.label.localeCompare(b.label)) }))
    .sort((a, b) => b.failureCount - a.failureCount || b.totalNodes - a.totalNodes || a.label.localeCompare(b.label));
}

export function buildTimelineFromAnalysisNodes(nodes: SkillAnalysisFlatNodeProjection[]): SkillAnalysisTimelineEventProjection[] {
  return nodes
    .map(node => ({
      id: `${node.source}:${node.nodeId}`,
      frame: node.frame,
      timeMs: node.timeMs,
      lane: `${node.stage}/${node.entityKind}`,
      label: node.label,
      nodeId: node.nodeId,
      severity: node.severity,
      source: node.source
    }))
    .sort((a, b) => a.frame - b.frame || a.timeMs - b.timeMs || a.id.localeCompare(b.id));
}

export function buildTimelineFromRuntimeEvents(events: AdminSkillDiagnosticsEvents | null): SkillAnalysisTimelineEventProjection[] {
  return (events?.events || [])
    .map((event, index) => ({
      id: `runtime:${event.frame}:${event.skillInstanceId}:${event.stage}:${index}`,
      frame: event.frame,
      timeMs: 0,
      lane: event.stage || 'runtime-event',
      label: `${event.eventType} Actor ${event.actorId} Skill ${event.skillId}`,
      nodeId: null,
      severity: event.severity || 'info',
      source: 'runtime-diagnostics'
    }))
    .sort((a, b) => a.frame - b.frame || a.id.localeCompare(b.id));
}

function sortSkillAnalysisNodes(nodes: SkillAnalysisNodeProjection[]): void {
  nodes.sort((a, b) => {
    const rootCompare = a.numericRootId - b.numericRootId;
    if (rootCompare !== 0) return rootCompare;
    const frameCompare = a.frame - b.frame;
    if (frameCompare !== 0) return frameCompare;
    return a.numericNodeId - b.numericNodeId;
  });
  nodes.forEach(node => sortSkillAnalysisNodes(node.children));
}

function buildNodeSummary(record: Record<string, unknown>, caseId: string): string {
  const parts = [
    caseId ? `case=${caseId}` : '',
    toNumber(record.childCount) > 0 ? `children=${toNumber(record.childCount)}` : '',
    record.isEnded === true ? `ended@${toNumber(record.endedFrame)}` : '',
    toText(record.message),
    toText(record.result)
  ].filter(Boolean);
  return parts.join(' | ') || 'trace node projection';
}

function buildEntityKey(entityKind: string, record: Record<string, unknown>, nodeId: number, configId: number, actorId: number, sourceContextId: string): string {
  const runtimeKind = toText(record.runtimeKind);
  const runtimeConfigId = toNumber(record.runtimeConfigId);
  const entityId = toNumber(record.entityId || record.projectileId || record.buffId || record.areaId || record.contextId || record.sourceContextId);
  if (entityId > 0) return `${entityKind}:${entityId}`;
  if (runtimeKind && runtimeConfigId > 0) return `${runtimeKind}:${runtimeConfigId}`;
  if (configId > 0) return `${entityKind}:${configId}`;
  if (actorId > 0 && entityKind === 'actor-skill') return `actor:${actorId}`;
  if (sourceContextId && sourceContextId !== '0') return `context:${sourceContextId}`;
  return `${entityKind}:node-${nodeId}`;
}

function buildOwnerKey(node: SkillAnalysisFlatNodeProjection): string {
  if (node.sourceActorId) return `actor:${node.sourceActorId}`;
  if (node.actorId) return `actor:${node.actorId}`;
  if (node.ownerContextId && node.ownerContextId !== '0') return `owner-context:${node.ownerContextId}`;
  if (node.rootContextId && node.rootContextId !== '0') return `root-context:${node.rootContextId}`;
  return `root:${node.rootId}`;
}

function ensureEntityGroup(groups: Map<string, SkillAnalysisEntityGroupProjection>, ownerKey: string, node: SkillAnalysisFlatNodeProjection): SkillAnalysisEntityGroupProjection {
  const existing = groups.get(ownerKey);
  if (existing) return existing;
  const group: SkillAnalysisEntityGroupProjection = {
    id: ownerKey,
    ownerKey,
    label: `主实体 ${ownerKey}`,
    actorId: node.sourceActorId || node.actorId || null,
    rootId: node.rootId || null,
    totalNodes: 0,
    failureCount: 0,
    relations: []
  };
  groups.set(ownerKey, group);
  return group;
}

function isFailureNode(node: SkillAnalysisFlatNodeProjection): boolean {
  const severity = node.severity.toLowerCase();
  return severity === 'error' || severity === 'failed' || Boolean(node.failure);
}

function matchesOption(value: string, filterValue: string): boolean {
  return !filterValue || filterValue === 'all' || value === filterValue;
}

function matchesNumberFilter(values: Array<number | null | undefined>, filterText: string): boolean {
  if (!filterText) return true;
  const expected = Number(filterText);
  if (!Number.isFinite(expected)) return true;
  return values.some(value => Number(value || 0) === expected);
}

function uniqueSorted(values: string[]): string[] {
  return [...new Set(values.filter(Boolean))].sort((a, b) => a.localeCompare(b));
}

function uniqueNumbers(values: number[]): number[] {
  return [...new Set(values.filter(value => Number.isFinite(value) && value > 0))].sort((a, b) => a - b);
}
