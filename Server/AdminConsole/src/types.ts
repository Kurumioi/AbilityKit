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
