import type { ShooterWorldComponentDiagnostics, ShooterWorldDiagnostics, ShooterWorldEntityDiagnostics } from '../types';

export interface ShooterWorldGroupProjection {
  key: string;
  label: string;
  entities: ShooterWorldEntityDiagnostics[];
}

export interface ShooterComponentKindCountProjection {
  kind: string;
  count: number;
  entities: number;
}

export function buildShooterWorldGroups(diagnostics: ShooterWorldDiagnostics | null | undefined): ShooterWorldGroupProjection[] {
  const groups = new Map<string, ShooterWorldGroupProjection>();
  for (const entity of diagnostics?.entities || []) {
    const key = `${entity.group}:${entity.entityKind}`;
    const label = `${entity.group.replace('ShooterSveltoGroups.', '')} / ${entity.entityKind}`;
    if (!groups.has(key)) groups.set(key, { key, label, entities: [] });
    groups.get(key)?.entities.push(entity);
  }

  return [...groups.values()].map(group => ({
    ...group,
    entities: group.entities.slice().sort((a, b) => a.entityId - b.entityId)
  }));
}

export function buildShooterComponentKindCounts(diagnostics: ShooterWorldDiagnostics | null | undefined): ShooterComponentKindCountProjection[] {
  const counts = new Map<string, { kind: string; count: number; entities: Set<string> }>();
  for (const entity of diagnostics?.entities || []) {
    for (const component of entity.components) {
      if (!counts.has(component.componentKind)) counts.set(component.componentKind, { kind: component.componentKind, count: 0, entities: new Set<string>() });
      const item = counts.get(component.componentKind);
      if (!item) continue;
      item.count += 1;
      item.entities.add(entity.key);
    }
  }

  return [...counts.values()].map(item => ({ kind: item.kind, count: item.count, entities: item.entities.size }));
}

export function fieldEntries(fields: ShooterWorldComponentDiagnostics['fields']): Array<{ key: string; value: string }> {
  return Object.entries(fields || {}).map(([key, value]) => ({ key, value }));
}

export function componentSummary(entity: ShooterWorldEntityDiagnostics): string {
  return entity.components.map(component => component.componentKind).join(' / ') || 'no components';
}
