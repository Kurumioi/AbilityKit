import type { AdminSkillAcceptanceBatch } from '../types';

export interface AcceptanceTraceTreeNode {
  nodeId: number;
  rootId: number;
  parentId: number;
  kind: string;
  configId: number;
  frame: number;
  timeMs: number;
  isRoot: boolean;
  isEnded: boolean;
  endedFrame: number;
  childCount: number;
  children: AcceptanceTraceTreeNode[];
}

export interface AcceptanceAssertionGroup {
  key: string;
  title: string;
  items: string[];
}

export type AcceptanceTraceTreeFlatNode = AcceptanceTraceTreeNode & { depth: number };

export interface AcceptanceCaseFilters {
  statusFilter: string;
  searchText: string;
  sortKey: string;
}

export function toNumber(value: unknown): number {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) return parsed;
  }
  return 0;
}

export function toText(value: unknown): string {
  if (value === null || value === undefined) return '';
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

export function filterAcceptanceCases(cases: AdminSkillAcceptanceBatch['cases'], filters: AcceptanceCaseFilters): AdminSkillAcceptanceBatch['cases'] {
  const keyword = filters.searchText.trim().toLowerCase();
  const status = filters.statusFilter;
  const filtered = cases.filter(item => {
    const statusMatched = status === 'all'
      || (status === 'passed' && item.passed === true)
      || (status === 'failed' && item.passed === false)
      || (status === 'unknown' && item.passed !== true && item.passed !== false);
    if (!statusMatched) return false;
    if (!keyword) return true;
    return [item.caseId, item.description, item.worldId, item.summaryPath, item.tracePath]
      .filter(Boolean)
      .some(value => String(value).toLowerCase().includes(keyword));
  });

  const direction = filters.sortKey === 'failedFirst' ? -1 : 1;
  return filtered.sort((a, b) => {
    if (filters.sortKey === 'status') return String(a.passed).localeCompare(String(b.passed));
    if (filters.sortKey === 'duration') return (b.finalTimeMs - a.finalTimeMs) * direction;
    if (filters.sortKey === 'trace') return (b.traceNodeCount - a.traceNodeCount) * direction;
    if (filters.sortKey === 'failedFirst') return Number(a.passed === true) - Number(b.passed === true) || a.caseId.localeCompare(b.caseId);
    return a.caseId.localeCompare(b.caseId);
  });
}

export function buildAcceptanceTraceTree(records: Record<string, unknown>[]): AcceptanceTraceTreeNode[] {
  const nodes = new Map<number, AcceptanceTraceTreeNode>();
  const roots: AcceptanceTraceTreeNode[] = [];

  const sortedRecords = [...records].sort((a, b) => {
    const rootCompare = toNumber(a.rootId) - toNumber(b.rootId);
    if (rootCompare !== 0) return rootCompare;
    const frameCompare = toNumber(a.frame) - toNumber(b.frame);
    if (frameCompare !== 0) return frameCompare;
    return toNumber(a.nodeId) - toNumber(b.nodeId);
  });

  for (const record of sortedRecords) {
    const nodeId = toNumber(record.nodeId);
    if (nodeId <= 0) continue;
    nodes.set(nodeId, {
      nodeId,
      rootId: toNumber(record.rootId),
      parentId: toNumber(record.parentId),
      kind: toText(record.kind) || 'Unknown',
      configId: toNumber(record.configId),
      frame: toNumber(record.frame),
      timeMs: toNumber(record.timeMs),
      isRoot: record.isRoot === true,
      isEnded: record.isEnded === true,
      endedFrame: toNumber(record.endedFrame),
      childCount: toNumber(record.childCount),
      children: []
    });
  }

  for (const node of nodes.values()) {
    const parent = node.parentId > 0 ? nodes.get(node.parentId) : null;
    if (parent) {
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }

  const sortNodes = (list: AcceptanceTraceTreeNode[]) => {
    list.sort((a, b) => {
      const rootCompare = a.rootId - b.rootId;
      if (rootCompare !== 0) return rootCompare;
      const frameCompare = a.frame - b.frame;
      if (frameCompare !== 0) return frameCompare;
      return a.nodeId - b.nodeId;
    });
    list.forEach(node => sortNodes(node.children));
  };

  sortNodes(roots);
  return roots;
}

export function flattenAcceptanceTraceTree(nodes: AcceptanceTraceTreeNode[], depth = 0): AcceptanceTraceTreeFlatNode[] {
  const result: AcceptanceTraceTreeFlatNode[] = [];
  for (const node of nodes) {
    result.push({ ...node, depth });
    if (node.children.length > 0) result.push(...flattenAcceptanceTraceTree(node.children, depth + 1));
  }
  return result;
}

export function buildAcceptanceAssertionGroups(summary: Record<string, unknown> | null, caseId: string): AcceptanceAssertionGroup[] {
  if (!summary) {
    return [{ key: 'empty', title: '断言分组', items: ['尚未选择 case。'] }];
  }

  const groups: AcceptanceAssertionGroup[] = [];
  const addGroup = (key: string, title: string, items: string[]) => {
    if (items.length > 0) groups.push({ key, title, items });
  };
  const readArray = (value: unknown): unknown[] => Array.isArray(value) ? value : [];

  addGroup('meta', '场景元数据', [
    `caseId: ${caseId || toText(summary.caseId) || 'unknown'}`,
    `worldId: ${toText(summary.worldId) || 'unknown'}`,
    `tickRate: ${toNumber(summary.tickRate)}`,
    `accelerated: ${summary.accelerated === true}`
  ]);

  addGroup('actors', 'Actor 断言', readArray(summary.actors).map((item, index) => {
    const actor = item as Record<string, unknown>;
    const alias = toText(actor.alias) || `Actor${index + 1}`;
    const heroId = toNumber(actor.heroId);
    const actorId = toText(actor.actorId) || 'unknown';
    const level = toNumber(actor.level);
    const skills = readArray(actor.skillIds).map(x => toText(x)).filter(Boolean).join(', ');
    return `${alias} | actorId=${actorId} | hero=${heroId} | level=${level}${skills ? ` | skills=${skills}` : ''}`;
  }));

  addGroup('setup', 'Setup Actions', readArray(summary.setupActions).map((item, index) => {
    const action = item as Record<string, unknown>;
    const actionName = toText(action.action) || `Setup${index + 1}`;
    const actorAlias = toText(action.actorAlias);
    const targetAlias = toText(action.targetAlias);
    const skillId = toNumber(action.skillId);
    const payload = toText(action.payload);
    return [actionName, actorAlias ? `actor=${actorAlias}` : '', targetAlias ? `target=${targetAlias}` : '', skillId > 0 ? `skillId=${skillId}` : '', payload ? `payload=${payload}` : '']
      .filter(Boolean)
      .join(' | ');
  }));

  addGroup('timeline', 'Timeline 断言', readArray(summary.timeline).map((item, index) => {
    const step = item as Record<string, unknown>;
    const stepId = toText(step.stepId) || `Step${index + 1}`;
    const atMs = toNumber(step.atMs);
    const action = toText(step.action);
    const actorAlias = toText(step.actorAlias);
    const targetAlias = toText(step.targetAlias);
    const skillId = toNumber(step.skillId);
    return `${stepId} @ ${atMs}ms${action ? ` | ${action}` : ''}${actorAlias ? ` | actor=${actorAlias}` : ''}${targetAlias ? ` | target=${targetAlias}` : ''}${skillId > 0 ? ` | skillId=${skillId}` : ''}`;
  }));

  addGroup('state', 'State 断言', readArray(summary.stateExpectations).map((item, index) => {
    const state = item as Record<string, unknown>;
    const alias = toText(state.alias) || `State${index + 1}`;
    const property = toText(state.property);
    const comparator = toText(state.comparator) || '==';
    const expected = toText(state.expectedValue) || (state.expectedFloat ?? state.expectedInt ?? state.expectedBool);
    return `${alias}${property ? `.${property}` : ''} ${comparator} ${expected}`;
  }));

  addGroup('context', 'Context 断言', readArray(summary.contextExpectations).map((item, index) => {
    const context = item as Record<string, unknown>;
    const alias = toText(context.alias) || `Context${index + 1}`;
    const kind = toText(context.kind);
    const property = toText(context.property);
    const comparator = toText(context.comparator) || '==';
    const expected = toText(context.expectedValue) || (context.expectedFloat ?? context.expectedInt ?? context.expectedBool);
    return `${alias}${kind ? `(${kind})` : ''}${property ? `.${property}` : ''} ${comparator} ${expected}`;
  }));

  const result = summary.result as Record<string, unknown> | undefined;
  addGroup('trace', 'Trace 结果', [
    `passed: ${toText(result?.passed) || 'unknown'}`,
    `skillCastTraceFound: ${toText(result?.skillCastTraceFound) || 'unknown'}`,
    `effectExecutionTraceFound: ${toText(result?.effectExecutionTraceFound) || 'unknown'}`,
    `allExpectedActionsExecuted: ${toText(result?.allExpectedActionsExecuted) || 'unknown'}`,
    `projectileLaunched: ${toText(result?.projectileLaunched) || 'unknown'}`,
    `finalFrame: ${toNumber(result?.finalFrame)}`,
    `traceNodeCount: ${toNumber(result?.traceNodeCount)}`
  ]);

  addGroup('counts', 'Trace Counts', readArray(summary.traceCounts).map((item) => {
    const count = item as Record<string, unknown>;
    return `${toText(count.kind) || 'Unknown'}: ${toNumber(count.count)}`;
  }));

  return groups;
}
