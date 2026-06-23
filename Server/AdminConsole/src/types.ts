export interface ApiResult<T> {
  ok: boolean;
  status: number;
  body: T | string | null;
}

export interface GameplayDescriptor {
  roomType: string;
  gameplayId: number;
  displayName: string;
  defaultMaxPlayers: number;
  allowJoinInProgress: boolean;
  defaultWorldType: string;
  defaultTickRate: number;
  defaultSyncTemplateId: string;
  supportedSyncTemplateIds: string[];
  supportsStateSyncPush: boolean;
  supportsFrameSync: boolean;
}

export interface RoomSummary {
  roomId: string;
  roomType: string;
  title?: string | null;
  playerCount: number;
  maxPlayers: number;
  isPublic: boolean;
  tags?: Record<string, string> | null;
}

export interface RoomSnapshot {
  summary?: RoomSummary;
  members?: unknown[];
  canStart?: boolean;
}

export interface RestoreRoomResponse {
  hasActiveRoom: boolean;
  snapshot?: RoomSnapshot | null;
}

export interface RoomRuntimeState {
  roomId: string;
  roomType: string;
  battleId?: string | null;
  worldId: number;
  isClosed: boolean;
  isInBattle: boolean;
  members: string[];
  memberStates?: Record<string, unknown> | null;
  serverNowTicks: number;
  tags?: Record<string, string> | null;
}

export interface ShooterSandboxState {
  running: boolean;
  region: string;
  serverId: string;
  roomId: string;
  battleId: string;
  worldId: number;
  botCount: number;
  currentFrame: number;
  serverNowTicks: number;
  snapshot?: RoomSnapshot | null;
}

export interface AdminServerStatus {
  environmentName: string;
  applicationName: string;
  machineName: string;
  processId: number;
  processName: string;
  startTimeUtcTicks: number;
  uptimeSeconds: number;
  workingSetBytes: number;
  gcTotalMemoryBytes: number;
  threadCount: number;
  maintenanceMode: boolean;
  drainMode: boolean;
  restartRequested: boolean;
  lastOperationId?: string | null;
  lastOperationReason?: string | null;
  lastOperationRequestedBy?: string | null;
  lastOperationRequestedAtTicks?: number | null;
  serverNowTicks: number;
}

export interface AdminClusterNodeProbe {
  nodeId: string;
  role: string;
  endpoint: string;
  status: string;
  diagnostics: string;
}

export interface AdminClusterDiagnostics {
  clusterId: string;
  serviceId: string;
  siloPort?: number | null;
  orleansGatewayPort?: number | null;
  clientConnected: boolean;
  clientStatus: string;
  nodes: AdminClusterNodeProbe[];
  runtimeMetrics: string[];
  warnings: string[];
  gatewayProcess: AdminServerStatus;
  serverNowTicks: number;
}

export interface AdminSkillActorSummary {
  accountId: string;
  actorId: number;
  basicAttackSkillId: number;
  skillIds: number[];
  diagnostics: string;
}

export interface AdminSkillMetric {
  name: string;
  value: number;
  unit: string;
  source: string;
}

export interface AdminSkillDiagnosticsSummary {
  roomId?: string | null;
  roomType?: string | null;
  battleId?: string | null;
  worldId: number;
  isInBattle: boolean;
  currentFrame: number;
  members: string[];
  diagnosticsStatus: string;
  actors: AdminSkillActorSummary[];
  metrics: AdminSkillMetric[];
  warnings: string[];
  serverNowTicks: number;
}

export interface AdminSkillEventFilter {
  battleId?: string | null;
  actorId?: number | null;
  skillId?: number | null;
  limit: number;
}

export interface AdminSkillEvent {
  frame: number;
  actorId: number;
  skillId: number;
  skillInstanceId: number;
  stage: string;
  eventType: string;
  targetActorId?: number | null;
  value?: number | null;
  message?: string | null;
  severity: string;
}

export interface AdminSkillDiagnosticsEvents {
  diagnosticsStatus: string;
  filters: AdminSkillEventFilter;
  events: AdminSkillEvent[];
  warnings: string[];
  serverNowTicks: number;
}

export interface AdminSkillAnalysisStage {
  id: string;
  displayName: string;
  runtimeSource: string;
  acceptanceSource: string;
  fields: string[];
}

export interface AdminSkillAnalysisField {
  name: string;
  description: string;
  requiredForCorrelation: boolean;
}

export interface AdminSkillAnalysisProjectionSchema {
  id: string;
  displayName: string;
  description: string;
  fields: string[];
}

export interface AdminSkillAnalysisModel {
  modelVersion: string;
  sources: string[];
  stages: AdminSkillAnalysisStage[];
  correlationFields: AdminSkillAnalysisField[];
  projectionSchemas: AdminSkillAnalysisProjectionSchema[];
  notes: string[];
  serverNowTicks: number;
}

export interface SkillAnalysisNodeProjection {
  nodeId: string;
  numericNodeId: number;
  rootId: string;
  numericRootId: number;
  parentId: string;
  numericParentId: number;
  kind: string;
  stage: string;
  label: string;
  configId?: number | null;
  frame: number;
  timeMs: number;
  actorId?: number | null;
  skillId?: number | null;
  sourceActorId?: number | null;
  targetActorId?: number | null;
  rootContextId?: string | null;
  ownerContextId?: string | null;
  sourceContextId?: string | null;
  entityKind: string;
  entityKey: string;
  status: string;
  severity: string;
  summary: string;
  source: string;
  failure?: string | null;
  raw: Record<string, unknown>;
  children: SkillAnalysisNodeProjection[];
}

export type SkillAnalysisFlatNodeProjection = SkillAnalysisNodeProjection & { depth: number };

export interface SkillAnalysisTimelineEventProjection {
  id: string;
  frame: number;
  timeMs: number;
  lane: string;
  label: string;
  nodeId?: string | null;
  severity: string;
  source: string;
}

export interface SkillAnalysisFilterState {
  searchText: string;
  severity: string;
  stage: string;
  kind: string;
  entityKind: string;
  actorId: string;
  skillId: string;
  configId: string;
  rootId: string;
  contextId: string;
  onlyFailures: boolean;
}

export interface SkillAnalysisFilterOptions {
  severities: string[];
  stages: string[];
  kinds: string[];
  entityKinds: string[];
  actorIds: number[];
  skillIds: number[];
  configIds: number[];
  rootIds: string[];
}

export interface SkillAnalysisEntityRelationProjection {
  id: string;
  ownerKey: string;
  entityKind: string;
  entityKey: string;
  label: string;
  nodeCount: number;
  failureCount: number;
  firstFrame: number;
  lastFrame: number;
  nodes: SkillAnalysisFlatNodeProjection[];
}

export interface SkillAnalysisEntityGroupProjection {
  id: string;
  ownerKey: string;
  label: string;
  actorId?: number | null;
  rootId?: string | null;
  totalNodes: number;
  failureCount: number;
  relations: SkillAnalysisEntityRelationProjection[];
}

export interface AdminSkillAcceptanceCaseListItem {
  caseId: string;
  description?: string | null;
  worldId?: string | null;
  tickRate: number;
  accelerated: boolean;
  passed?: boolean | null;
  finalFrame: number;
  finalTimeMs: number;
  traceNodeCount: number;
  summaryPath: string;
  tracePath: string;
}

export interface AdminSkillAcceptanceBatch {
  artifactDirectory: string;
  hasBatchSummary: boolean;
  batchSummary?: Record<string, unknown> | null;
  cases: AdminSkillAcceptanceCaseListItem[];
  warnings: string[];
  serverNowTicks: number;
}

export interface AdminSkillAcceptanceCase {
  caseId: string;
  artifactDirectory: string;
  summary?: Record<string, unknown> | null;
  traceRecords: Record<string, unknown>[];
  summaryPath: string;
  tracePath: string;
  warnings: string[];
  serverNowTicks: number;
}

export interface AdminSkillAcceptanceExecutionStrategy {
  id: string;
  displayName: string;
  boundary: string;
  status: string;
  description: string;
}

export interface AdminSkillAcceptanceAllowedScript {
  id: string;
  displayName: string;
  relativePath: string;
  shell: string;
  exists: boolean;
  arguments: string[];
  produces: string[];
}

export interface AdminSkillAcceptanceRunPlan {
  allowed: boolean;
  message: string;
  artifactDirectory: string;
  executionMode: string;
  canRequestFromAdmin: boolean;
  strategies: AdminSkillAcceptanceExecutionStrategy[];
  allowedScripts: AdminSkillAcceptanceAllowedScript[];
  requiredApprovals: string[];
  auditFields: string[];
  notes: string[];
  serverNowTicks: number;
}

export interface AdminDashboardResponse {
  gameplays: GameplayDescriptor[];
  rooms: RoomSummary[];
  accountId?: string | null;
  currentRoomId?: string | null;
  currentRoom?: RestoreRoomResponse | null;
  runtimeState?: RoomRuntimeState | null;
  shooterSandbox?: ShooterSandboxState | null;
  serverNowTicks: number;
  serverStatus?: AdminServerStatus | null;
}

export interface AdminServerOperationResponse {
  success: boolean;
  operation: string;
  operationId: string;
  requestedBy: string;
  reason?: string | null;
  status: AdminServerStatus;
}

export interface SessionResponse {
  accountId?: string | null;
  sessionToken?: string;
  expireAtTicks?: number;
  isValid?: boolean;
}

export interface CreateRoomResponse {
  roomId?: string;
  snapshot?: RoomSnapshot;
}
