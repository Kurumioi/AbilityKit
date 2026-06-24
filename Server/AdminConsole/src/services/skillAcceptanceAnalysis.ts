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
      kind: toText(record.kind) || '未知',
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
    return [{ key: 'empty', title: '断言分组', items: ['尚未选择用例。'] }];
  }

  const groups: AcceptanceAssertionGroup[] = [];
  const addGroup = (key: string, title: string, items: string[]) => {
    if (items.length > 0) groups.push({ key, title, items });
  };
  const readArray = (value: unknown): unknown[] => Array.isArray(value) ? value : [];

  addGroup('meta', '场景元数据', [
    `用例 ID: ${caseId || toText(summary.caseId) || '未知'}`,
    `世界 ID: ${toText(summary.worldId) || '未知'}`,
    `帧率: ${toNumber(summary.tickRate)}`,
    `加速: ${summary.accelerated === true}`
  ]);

  addGroup('actors', '角色断言', readArray(summary.actors).map((item, index) => {
    const actor = item as Record<string, unknown>;
    const alias = toText(actor.alias) || `角色${index + 1}`;
    const heroId = toNumber(actor.heroId);
    const actorId = toText(actor.actorId) || '未知';
    const level = toNumber(actor.level);
    const skills = readArray(actor.skillIds).map(x => toText(x)).filter(Boolean).join(', ');
    return `${alias} | 角色ID=${actorId} | 英雄=${heroId} | 等级=${level}${skills ? ` | 技能=${skills}` : ''}`;
  }));

  addGroup('setup', '初始化动作', readArray(summary.setupActions).map((item, index) => {
    const action = item as Record<string, unknown>;
    const actionName = toText(action.action) || `初始化${index + 1}`;
    const actorAlias = toText(action.actorAlias);
    const targetAlias = toText(action.targetAlias);
    const skillId = toNumber(action.skillId);
    const payload = toText(action.payload);
    return [actionName, actorAlias ? `角色=${actorAlias}` : '', targetAlias ? `目标=${targetAlias}` : '', skillId > 0 ? `技能ID=${skillId}` : '', payload ? `载荷=${payload}` : '']
      .filter(Boolean)
      .join(' | ');
  }));

  addGroup('timeline', '时间轴断言', readArray(summary.timeline).map((item, index) => {
    const step = item as Record<string, unknown>;
    const stepId = toText(step.stepId) || `Step${index + 1}`;
    const atMs = toNumber(step.atMs);
    const action = toText(step.action);
    const actorAlias = toText(step.actorAlias);
    const targetAlias = toText(step.targetAlias);
    const skillId = toNumber(step.skillId);
    return `${stepId} @ ${atMs}ms${action ? ` | ${action}` : ''}${actorAlias ? ` | 角色=${actorAlias}` : ''}${targetAlias ? ` | 目标=${targetAlias}` : ''}${skillId > 0 ? ` | 技能ID=${skillId}` : ''}`;
  }));

  addGroup('state', '状态断言', readArray(summary.stateExpectations).map((item, index) => {
    const state = item as Record<string, unknown>;
    const alias = toText(state.alias) || `状态${index + 1}`;
    const property = toText(state.property);
    const comparator = toText(state.comparator) || '==';
    const expected = toText(state.expectedValue) || (state.expectedFloat ?? state.expectedInt ?? state.expectedBool);
    return `${alias}${property ? `.${property}` : ''} ${comparator} ${expected}`;
  }));

  addGroup('context', '上下文断言', readArray(summary.contextExpectations).map((item, index) => {
    const context = item as Record<string, unknown>;
    const alias = toText(context.alias) || `上下文${index + 1}`;
    const kind = toText(context.kind);
    const property = toText(context.property);
    const comparator = toText(context.comparator) || '==';
    const expected = toText(context.expectedValue) || (context.expectedFloat ?? context.expectedInt ?? context.expectedBool);
    return `${alias}${kind ? `(${kind})` : ''}${property ? `.${property}` : ''} ${comparator} ${expected}`;
  }));

  const result = summary.result as Record<string, unknown> | undefined;
  addGroup('trace', '追踪结果', [
    `通过: ${toText(result?.passed) || '未知'}`,
    `找到技能释放追踪: ${toText(result?.skillCastTraceFound) || '未知'}`,
    `找到效果执行追踪: ${toText(result?.effectExecutionTraceFound) || '未知'}`,
    `全部预期动作已执行: ${toText(result?.allExpectedActionsExecuted) || '未知'}`,
    `已发射投射物: ${toText(result?.projectileLaunched) || '未知'}`,
    `最终帧: ${toNumber(result?.finalFrame)}`,
    `追踪节点数: ${toNumber(result?.traceNodeCount)}`
  ]);

  addGroup('counts', '追踪计数', readArray(summary.traceCounts).map((item) => {
    const count = item as Record<string, unknown>;
    return `${toText(count.kind) || '未知'}: ${toNumber(count.count)}`;
  }));

  return groups;
}
