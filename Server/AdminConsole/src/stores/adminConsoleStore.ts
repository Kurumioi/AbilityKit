import { computed, reactive, ref, watch } from 'vue';
import { adminStorage } from '../services/storage';
import { AdminDomainApis } from '../services/domainApi';
import { buildAcceptanceAssertionGroups, buildAcceptanceTraceTree, filterAcceptanceCases, flattenAcceptanceTraceTree } from '../services/skillAcceptanceAnalysis';
import { buildSkillAnalysisEntityRelations, buildSkillAnalysisFilterOptions, buildSkillAnalysisTree, buildTimelineFromAnalysisNodes, buildTimelineFromRuntimeEvents, createDefaultSkillAnalysisFilter, filterSkillAnalysisNodes, flattenSkillAnalysisTree } from '../services/skillAnalysisProjection';
import type { AdminClusterDiagnostics, AdminDashboardResponse, AdminServerOperationResponse, AdminServerStatus, AdminSkillAcceptanceArtifactDirectoryList, AdminSkillAcceptanceBatch, AdminSkillAcceptanceCase, AdminSkillAcceptanceRunPlan, AdminSkillAcceptanceRunRequest, AdminSkillAcceptanceRunResponse, AdminSkillAcceptanceTemplateList, AdminSkillAnalysisModel, AdminSkillDiagnosticsEvents, AdminSkillDiagnosticsSummary, CreateRoomResponse, GameplayDescriptor, RestoreRoomResponse, RoomRuntimeState, RoomSnapshot, RoomSummary, SessionResponse, ShooterSandboxState, SkillAnalysisFlatNodeProjection } from '../types';

const apis = new AdminDomainApis();

const busy = ref(false);
const sessionToken = ref(adminStorage.get('sessionToken', ''));
const region = ref(adminStorage.get('region', 'cn'));
const serverId = ref(adminStorage.get('serverId', 'dev'));
const selectedRoomType = ref('shooter');
const roomId = ref(adminStorage.get('roomId', ''));
const gameplays = ref<GameplayDescriptor[]>([]);
const rooms = ref<RoomSummary[]>([]);
const snapshot = ref<RoomSnapshot | null>(null);
const runtimeState = ref<RoomRuntimeState | null>(null);
const dashboard = ref<AdminDashboardResponse | null>(null);
const serverStatus = ref<AdminServerStatus | null>(null);
const clusterDiagnostics = ref<AdminClusterDiagnostics | null>(null);
const skillSummary = ref<AdminSkillDiagnosticsSummary | null>(null);
const skillEvents = ref<AdminSkillDiagnosticsEvents | null>(null);
const skillAnalysisModel = ref<AdminSkillAnalysisModel | null>(null);
const acceptanceBatch = ref<AdminSkillAcceptanceBatch | null>(null);
const acceptanceCase = ref<AdminSkillAcceptanceCase | null>(null);
const acceptanceRunPlan = ref<AdminSkillAcceptanceRunPlan | null>(null);
const acceptanceArtifactDirectories = ref<AdminSkillAcceptanceArtifactDirectoryList | null>(null);
const acceptanceTemplates = ref<AdminSkillAcceptanceTemplateList | null>(null);
const acceptanceLastRun = ref<AdminSkillAcceptanceRunResponse | null>(null);
const lastResponse = ref('');

const account = reactive({ accountId: adminStorage.get('accountId', ''), expireSeconds: 86400, kickExisting: true });
const create = reactive({ roomType: 'shooter', title: 'Shooter 后台房间', isPublic: true, maxPlayers: 4, tagsJson: '{\n  "source": "admin-console"\n}' });
const battle = reactive({ gameplayId: 2, ruleSetId: 0, configVersion: 1, protocolVersion: 1, worldType: 'shooter_battle', syncTemplateId: 'pure-state-authority' });
const skillLoadout = reactive({ heroId: 1, teamId: 1, spawnPointId: 1, level: 1, attributeTemplateId: 1, basicAttackSkillId: 1001, skillIdsText: '1002,1003,1004' });
const skillEventFilter = reactive<{ battleId: string; actorId: number | null; skillId: number | null; limit: number }>({ battleId: '', actorId: null, skillId: null, limit: 100 });
const sandbox = reactive<{ sandboxId: string; botCount: number; maxPlayers: number; tickRate: number; state: ShooterSandboxState | null }>({ sandboxId: 'default', botCount: 3, maxPlayers: 4, tickRate: 30, state: null });
const serverOperation = reactive({ reason: '后台控制台操作' });
const acceptance = reactive({ artifactDirectory: 'artifacts/moba-acceptance', selectedCaseId: '', traceLimit: 500, statusFilter: 'all', searchText: '', categoryFilter: '', tagFilter: '', sortKey: 'caseId', selectedTemplateId: 'single-skill-damage', runCaseId: '', runDescription: '后台参数化战斗分析导出', runActorId: 1, runTargetActorId: 2, runSkillId: 1002, runEffectId: 2001, runProjectileId: 0, runAreaId: 0, runBuffId: 0, runShieldId: 5001, runBaseDamage: 120, runMitigatedDamage: 96, runShieldAbsorb: 60, runHpDamage: 36, runTickRate: 30, runDurationFrames: 12, runOperatorReason: '后台战斗分析导出' });
const acceptanceAnalysisFilter = reactive(createDefaultSkillAnalysisFilter());
const runtimeAnalysisFilter = reactive(createDefaultSkillAnalysisFilter());
const selectedAcceptanceAnalysisNodeKey = ref('');
const selectedRuntimeAnalysisNodeKey = ref('');

const selectedGameplay = computed(() => gameplays.value.find(x => x.roomType === selectedRoomType.value) || null);
const selectedRoomSummary = computed(() => roomId.value ? rooms.value.find(room => room.roomId === roomId.value) || null : null);
const effectiveRoomId = computed(() => roomId.value || snapshot.value?.summary?.roomId || selectedRoomSummary.value?.roomId || '');
const hasSession = computed(() => Boolean(sessionToken.value));
const hasCurrentRoom = computed(() => Boolean(effectiveRoomId.value));
const canOperateCurrentRoom = computed(() => !busy.value && hasSession.value && hasCurrentRoom.value);
const accountLabel = computed(() => dashboard.value?.accountId || account.accountId || (sessionToken.value ? '已登录会话' : '未登录'));
const sandboxLabel = computed(() => sandbox.state?.running ? '运行中' : '已停止');
const serverModeLabel = computed(() => {
  if (!serverStatus.value) return '未知';
  const flags = [];
  if (serverStatus.value.restartRequested) flags.push('已请求重启');
  if (serverStatus.value.maintenanceMode) flags.push('维护中');
  if (serverStatus.value.drainMode) flags.push('排空中');
  return flags.length > 0 ? flags.join(' / ') : '正常';
});
const clusterLabel = computed(() => clusterDiagnostics.value ? `${clusterDiagnostics.value.clusterId} / ${clusterDiagnostics.value.serviceId}` : '未知');
const skillBattleId = computed(() => skillEventFilter.battleId || skillSummary.value?.battleId || runtimeState.value?.battleId || '');
const battleId = computed(() => skillBattleId.value || runtimeState.value?.battleId || '');
const acceptancePassedCount = computed(() => acceptanceBatch.value?.cases?.filter(x => x.passed === true).length || 0);
const acceptanceFailedCount = computed(() => acceptanceBatch.value?.cases?.filter(x => x.passed === false).length || 0);
const acceptanceUnknownCount = computed(() => acceptanceBatch.value?.cases?.filter(x => x.passed !== true && x.passed !== false).length || 0);
const acceptanceFilteredCases = computed(() => filterAcceptanceCases(acceptanceBatch.value?.cases || [], acceptance));
const acceptanceTracePreview = computed(() => acceptanceCase.value?.traceRecords?.slice(0, 80) || []);
const acceptanceTraceTree = computed(() => buildAcceptanceTraceTree(acceptanceCase.value?.traceRecords || []));
const acceptanceTraceFlat = computed(() => flattenAcceptanceTraceTree(acceptanceTraceTree.value));
const acceptanceAssertionGroups = computed(() => buildAcceptanceAssertionGroups(acceptanceCase.value?.summary || null, acceptanceCase.value?.caseId || acceptanceBatch.value?.cases?.find(x => x.caseId)?.caseId || ''));
const acceptanceAnalysisTree = computed(() => buildSkillAnalysisTree(acceptanceCase.value?.traceRecords || [], 'scenario-acceptance-artifacts', acceptanceCase.value?.caseId || acceptance.selectedCaseId || ''));
const acceptanceAnalysisFlat = computed(() => flattenSkillAnalysisTree(acceptanceAnalysisTree.value));
const acceptanceAnalysisFilterOptions = computed(() => buildSkillAnalysisFilterOptions(acceptanceAnalysisFlat.value));
const acceptanceAnalysisFilteredFlat = computed(() => filterSkillAnalysisNodes(acceptanceAnalysisFlat.value, acceptanceAnalysisFilter));
const acceptanceAnalysisTimeline = computed(() => buildTimelineFromAnalysisNodes(acceptanceAnalysisFilteredFlat.value));
const acceptanceAnalysisEntityRelations = computed(() => buildSkillAnalysisEntityRelations(acceptanceAnalysisFilteredFlat.value));
const runtimeAnalysisFlat = computed<SkillAnalysisFlatNodeProjection[]>(() => []);
const runtimeAnalysisFilterOptions = computed(() => buildSkillAnalysisFilterOptions(runtimeAnalysisFlat.value));
const runtimeAnalysisFilteredFlat = computed(() => filterSkillAnalysisNodes(runtimeAnalysisFlat.value, runtimeAnalysisFilter));
const runtimeAnalysisTimeline = computed(() => runtimeAnalysisFilteredFlat.value.length > 0 ? buildTimelineFromAnalysisNodes(runtimeAnalysisFilteredFlat.value) : buildTimelineFromRuntimeEvents(skillEvents.value));
const runtimeAnalysisEntityRelations = computed(() => buildSkillAnalysisEntityRelations(runtimeAnalysisFilteredFlat.value));
const selectedAcceptanceAnalysisNode = computed(() => findSelectedAnalysisNode(acceptanceAnalysisFilteredFlat.value, selectedAcceptanceAnalysisNodeKey.value));
const selectedRuntimeAnalysisNode = computed(() => findSelectedAnalysisNode(runtimeAnalysisFilteredFlat.value, selectedRuntimeAnalysisNodeKey.value));
const skillDiagnosticsWarnings = computed(() => [...(skillSummary.value?.warnings || []), ...(skillEvents.value?.warnings || [])]);
const skillAnalysisModelNotes = computed(() => skillAnalysisModel.value?.notes || []);

watch(sessionToken, value => adminStorage.set('sessionToken', value));
watch(() => account.accountId, value => adminStorage.set('accountId', value));
watch(region, value => adminStorage.set('region', value));
watch(serverId, value => adminStorage.set('serverId', value));
watch(roomId, value => adminStorage.set('roomId', value));

async function call<T>(operation: Promise<{ ok: boolean; status: number; body: T | string | null }>): Promise<T | null> {
  busy.value = true;
  try {
    const result = await operation;
    lastResponse.value = JSON.stringify(result, null, 2);
    return result.ok ? result.body as T : null;
  } catch (error) {
    lastResponse.value = String(error);
    return null;
  } finally {
    busy.value = false;
  }
}

function buildQuery(params: Record<string, string | number | null | undefined>): string {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') query.set(key, String(value));
  });
  const text = query.toString();
  return text ? `?${text}` : '';
}

function formatBytes(value: number): string {
  if (!Number.isFinite(value) || value <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  let size = value;
  let unit = 0;
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024;
    unit += 1;
  }
  return `${size.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
}

function formatDuration(seconds: number): string {
  const value = Math.max(0, Math.floor(seconds || 0));
  const days = Math.floor(value / 86400);
  const hours = Math.floor((value % 86400) / 3600);
  const minutes = Math.floor((value % 3600) / 60);
  if (days > 0) return `${days}d ${hours}h`;
  if (hours > 0) return `${hours}h ${minutes}m`;
  return `${minutes}m`;
}

function applyGameplayDefaults(): void {
  const gameplay = selectedGameplay.value;
  if (!gameplay) return;
  create.roomType = gameplay.roomType;
  create.maxPlayers = gameplay.defaultMaxPlayers || create.maxPlayers;
  battle.gameplayId = Number(gameplay.gameplayId || battle.gameplayId || 0);
  battle.worldType = gameplay.defaultWorldType || gameplay.roomType;
  battle.syncTemplateId = gameplay.defaultSyncTemplateId || '';
}

async function refreshDashboard(): Promise<void> {
  const data = await call<AdminDashboardResponse>(apis.dashboard.dashboard({ sessionToken: sessionToken.value || null, region: region.value, serverId: serverId.value, roomType: selectedRoomType.value, limit: 50, sandboxId: sandbox.sandboxId || 'default' }));
  if (!data) return;
  dashboard.value = data;
  gameplays.value = data.gameplays || [];
  rooms.value = data.rooms || [];
  sandbox.state = data.shooterSandbox || null;
  serverStatus.value = data.serverStatus || serverStatus.value;
  runtimeState.value = data.runtimeState || runtimeState.value;
  if (data.currentRoomId) roomId.value = data.currentRoomId;
  if (data.currentRoom?.snapshot) snapshot.value = data.currentRoom.snapshot;
  if (!gameplays.value.some(x => x.roomType === selectedRoomType.value) && gameplays.value.length > 0) selectedRoomType.value = gameplays.value[0].roomType;
  applyGameplayDefaults();
  if (!skillEventFilter.battleId && data.runtimeState?.battleId) skillEventFilter.battleId = data.runtimeState.battleId;
}

async function refreshServerStatus(): Promise<void> {
  const data = await call<AdminServerStatus>(apis.ops.status());
  if (data) serverStatus.value = data;
}

async function refreshClusterDiagnostics(): Promise<void> {
  const data = await call<AdminClusterDiagnostics>(apis.cluster.diagnostics());
  if (!data) return;
  clusterDiagnostics.value = data;
  serverStatus.value = data.gatewayProcess || serverStatus.value;
}

async function refreshSkillSummary(): Promise<void> {
  const data = await call<AdminSkillDiagnosticsSummary>(apis.skills.summary(buildQuery({ roomId: roomId.value || runtimeState.value?.roomId, battleId: skillBattleId.value })));
  if (!data) return;
  skillSummary.value = data;
  if (!skillEventFilter.battleId && data.battleId) skillEventFilter.battleId = data.battleId;
}

async function refreshSkillEvents(): Promise<void> {
  const data = await call<AdminSkillDiagnosticsEvents>(apis.skills.events(buildQuery({ battleId: skillBattleId.value, actorId: skillEventFilter.actorId, skillId: skillEventFilter.skillId, limit: skillEventFilter.limit || 100 })));
  if (data) skillEvents.value = data;
}

async function refreshSkillAnalysisModel(): Promise<void> {
  const data = await call<AdminSkillAnalysisModel>(apis.skills.analysisModel());
  if (data) skillAnalysisModel.value = data;
}

async function refreshSkillDiagnostics(): Promise<void> {
  await refreshSkillSummary();
  await refreshSkillEvents();
  await refreshSkillAnalysisModel();
}

async function refreshAcceptanceArtifactDirectories(): Promise<void> {
  const data = await call<AdminSkillAcceptanceArtifactDirectoryList>(apis.skills.acceptanceArtifactDirectories());
  if (data) acceptanceArtifactDirectories.value = data;
}

async function refreshAcceptanceTemplates(): Promise<void> {
  const data = await call<AdminSkillAcceptanceTemplateList>(apis.skills.acceptanceTemplates());
  if (!data) return;
  acceptanceTemplates.value = data;
  if (!acceptance.selectedTemplateId && data.templates?.length) acceptance.selectedTemplateId = data.templates[0].id;
}

async function refreshAcceptanceBatch(): Promise<void> {
  const data = await call<AdminSkillAcceptanceBatch>(apis.skills.acceptanceBatch(buildQuery({ artifactDirectory: acceptance.artifactDirectory })));
  if (!data) return;
  acceptanceBatch.value = data;
  if (!acceptance.selectedCaseId && data.cases?.length) acceptance.selectedCaseId = data.cases[0].caseId;
}

async function refreshAcceptanceCase(caseId = acceptance.selectedCaseId): Promise<void> {
  if (!caseId) return;
  acceptance.selectedCaseId = caseId;
  const data = await call<AdminSkillAcceptanceCase>(apis.skills.acceptanceCase(caseId, buildQuery({ artifactDirectory: acceptance.artifactDirectory, traceLimit: acceptance.traceLimit || 500 })));
  if (data) acceptanceCase.value = data;
}

async function refreshAcceptanceRunPlan(): Promise<void> {
  const data = await call<AdminSkillAcceptanceRunPlan>(apis.skills.acceptanceRunPlan(buildQuery({ artifactDirectory: acceptance.artifactDirectory })));
  if (data) acceptanceRunPlan.value = data;
}

function toNumber(value: unknown): number {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) return parsed;
  }
  return 0;
}

function toText(value: unknown): string {
  if (value === null || value === undefined) return '';
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

function getAnalysisNodeKey(node: SkillAnalysisFlatNodeProjection): string {
  return `${node.source}:${node.nodeId}`;
}

function findSelectedAnalysisNode(nodes: SkillAnalysisFlatNodeProjection[], key: string): SkillAnalysisFlatNodeProjection | null {
  return nodes.find(node => getAnalysisNodeKey(node) === key) || nodes[0] || null;
}

function selectAcceptanceAnalysisNode(node: SkillAnalysisFlatNodeProjection): void {
  selectedAcceptanceAnalysisNodeKey.value = getAnalysisNodeKey(node);
}

function selectRuntimeAnalysisNode(node: SkillAnalysisFlatNodeProjection): void {
  selectedRuntimeAnalysisNodeKey.value = getAnalysisNodeKey(node);
}

async function refreshAcceptanceArtifacts(): Promise<void> {
  await refreshAcceptanceArtifactDirectories();
  await refreshAcceptanceTemplates();
  await refreshAcceptanceBatch();
  await refreshAcceptanceRunPlan();
  if (acceptance.selectedCaseId) await refreshAcceptanceCase(acceptance.selectedCaseId);
}

function selectAcceptanceArtifactDirectory(value: string): void {
  if (!value) return;
  acceptance.artifactDirectory = value;
  acceptance.selectedCaseId = '';
  acceptanceCase.value = null;
}

function applyAcceptanceTemplate(templateId = acceptance.selectedTemplateId): void {
  const template = acceptanceTemplates.value?.templates?.find(item => item.id === templateId);
  if (!template) return;
  acceptance.selectedTemplateId = template.id;
  acceptance.runDescription = template.description || acceptance.runDescription;
  acceptance.runSkillId = template.defaults.skillId;
  acceptance.runEffectId = template.defaults.effectId;
  acceptance.runProjectileId = template.defaults.projectileId;
  acceptance.runAreaId = template.defaults.areaId;
  acceptance.runBuffId = template.defaults.buffId;
  acceptance.runShieldId = template.defaults.shieldId;
  acceptance.runBaseDamage = template.defaults.baseDamage;
  acceptance.runMitigatedDamage = template.defaults.mitigatedDamage;
  acceptance.runShieldAbsorb = template.defaults.shieldAbsorb;
  acceptance.runHpDamage = template.defaults.hpDamage;
  acceptance.runTickRate = template.defaults.tickRate;
  acceptance.runDurationFrames = template.defaults.durationFrames;
}

function buildAcceptanceRunRequest(): AdminSkillAcceptanceRunRequest {
  return {
    sessionToken: sessionToken.value || null,
    artifactDirectory: acceptance.artifactDirectory,
    caseId: acceptance.runCaseId || null,
    description: acceptance.runDescription,
    actorId: Number(acceptance.runActorId || 1),
    targetActorId: Number(acceptance.runTargetActorId || 2),
    skillId: Number(acceptance.runSkillId || 1002),
    effectId: Number(acceptance.runEffectId || 2001),
    projectileId: Number(acceptance.runProjectileId || 0),
    areaId: Number(acceptance.runAreaId || 0),
    buffId: Number(acceptance.runBuffId || 0),
    shieldId: Number(acceptance.runShieldId || 0),
    baseDamage: Number(acceptance.runBaseDamage || 0),
    mitigatedDamage: Number(acceptance.runMitigatedDamage || 0),
    shieldAbsorb: Number(acceptance.runShieldAbsorb || 0),
    hpDamage: Number(acceptance.runHpDamage || 0),
    tickRate: Number(acceptance.runTickRate || 30),
    durationFrames: Number(acceptance.runDurationFrames || 12),
    templateId: acceptance.selectedTemplateId || null,
    operatorReason: acceptance.runOperatorReason || null
  };
}

async function runAcceptanceAnalysis(): Promise<void> {
  const data = await call<AdminSkillAcceptanceRunResponse>(apis.skills.acceptanceRun(buildAcceptanceRunRequest()));
  if (!data) return;
  acceptanceLastRun.value = data;
  acceptanceBatch.value = data.batch;
  acceptance.artifactDirectory = data.artifactDirectory;
  acceptance.selectedCaseId = data.caseId;
  await refreshAcceptanceArtifactDirectories();
  await refreshAcceptanceCase(data.caseId);
}

async function setMaintenanceMode(enabled: boolean): Promise<void> {
  const data = await call<AdminServerOperationResponse>(apis.ops.maintenance({ sessionToken: sessionToken.value, enabled, reason: serverOperation.reason }));
  if (data?.status) serverStatus.value = data.status;
}

async function setDrainMode(enabled: boolean): Promise<void> {
  const data = await call<AdminServerOperationResponse>(apis.ops.drain({ sessionToken: sessionToken.value, enabled, reason: serverOperation.reason }));
  if (data?.status) serverStatus.value = data.status;
}

async function requestRestart(): Promise<void> {
  const data = await call<AdminServerOperationResponse>(apis.ops.restartRequest({ sessionToken: sessionToken.value, enabled: true, reason: serverOperation.reason }));
  if (data?.status) serverStatus.value = data.status;
}

async function guestLogin(): Promise<void> {
  const data = await call<SessionResponse>(apis.session.guestLogin());
  if (data?.sessionToken) sessionToken.value = data.sessionToken;
  await refreshDashboard();
}

async function accountLogin(): Promise<void> {
  const data = await call<SessionResponse>(apis.session.accountLogin({ accountId: account.accountId, expireSeconds: Number(account.expireSeconds || 86400), kickExisting: account.kickExisting === true }));
  if (data?.sessionToken) sessionToken.value = data.sessionToken;
  await refreshDashboard();
}

async function validateSession(): Promise<void> {
  await call<SessionResponse>(apis.session.validate({ sessionToken: sessionToken.value }));
}

async function logout(): Promise<void> {
  await call(apis.session.logout({ sessionToken: sessionToken.value }));
  sessionToken.value = '';
  await refreshDashboard();
}

function parseTags(): Record<string, string> | null {
  const text = create.tagsJson.trim();
  return text ? JSON.parse(text) as Record<string, string> : null;
}

function parseSkillIds(): number[] {
  return skillLoadout.skillIdsText.split(',').map(x => Number(x.trim())).filter(x => Number.isFinite(x) && x > 0);
}

async function submitMobaLoadout(): Promise<void> {
  const data = await call<RoomSnapshot>(apis.rooms.pickHero({ sessionToken: sessionToken.value, roomId: roomId.value, heroId: Number(skillLoadout.heroId || 1), teamId: Number(skillLoadout.teamId || 1), spawnPointId: Number(skillLoadout.spawnPointId || 1), level: Number(skillLoadout.level || 1), attributeTemplateId: Number(skillLoadout.attributeTemplateId || 1), basicAttackSkillId: Number(skillLoadout.basicAttackSkillId || 1), skillIds: parseSkillIds() }));
  if (data) snapshot.value = data;
  await refreshDashboard();
  await refreshSkillDiagnostics();
}

async function createRoom(): Promise<void> {
  let tags: Record<string, string> | null = null;
  try { tags = parseTags(); } catch { lastResponse.value = '标签 JSON 格式无效'; return; }
  const data = await call<CreateRoomResponse>(apis.rooms.create({ sessionToken: sessionToken.value, region: region.value, serverId: serverId.value, roomType: create.roomType, title: create.title, isPublic: create.isPublic === true, maxPlayers: Number(create.maxPlayers || 0), tags }));
  if (data?.roomId) roomId.value = data.roomId;
  await refreshDashboard();
}

async function selectRoom(value: string): Promise<void> {
  roomId.value = value;
  const selected = rooms.value.find(room => room.roomId === value);
  if (selected) {
    selectedRoomType.value = selected.roomType || selectedRoomType.value;
    create.roomType = selected.roomType || create.roomType;
  }

  snapshot.value = selected ? { summary: selected, members: [], canStart: false } : null;
  runtimeState.value = null;
  if (sessionToken.value && value) await joinRoom(value);
}

async function joinRoom(targetRoomId = effectiveRoomId.value): Promise<boolean> {
  if (!targetRoomId) return false;
  roomId.value = targetRoomId;
  const data = await call<CreateRoomResponse>(apis.rooms.join({ sessionToken: sessionToken.value, roomId: targetRoomId }));
  if (!data?.snapshot) return false;
  roomId.value = data.snapshot.summary?.roomId || targetRoomId;
  snapshot.value = data.snapshot;
  selectedRoomType.value = data.snapshot.summary?.roomType || selectedRoomType.value;
  await refreshDashboard();
  return true;
}

async function restoreCurrentRoom(): Promise<void> {
  const data = await call<RestoreRoomResponse>(apis.rooms.restoreCurrent({ sessionToken: sessionToken.value }));
  if (data?.hasActiveRoom && data.snapshot) {
    snapshot.value = data.snapshot;
    roomId.value = data.snapshot.summary?.roomId || roomId.value;
  }
}

async function leaveRoom(targetRoomId = effectiveRoomId.value): Promise<void> {
  if (!targetRoomId) return;
  roomId.value = targetRoomId;
  const leavingRoomId = targetRoomId;
  const result = await call(apis.rooms.leave({ sessionToken: sessionToken.value, roomId: leavingRoomId }));
  if (!result) return;
  snapshot.value = null;
  runtimeState.value = null;
  roomId.value = '';
  await refreshDashboard();
  await selectNextAvailableRoom(leavingRoomId);
}

async function markOffline(targetRoomId = effectiveRoomId.value): Promise<void> {
  if (!targetRoomId) return;
  roomId.value = targetRoomId;
  const data = await call<RoomRuntimeState>(apis.rooms.markOffline({ sessionToken: sessionToken.value, roomId: targetRoomId }));
  if (data) runtimeState.value = data;
}

async function closeRoom(targetRoomId = effectiveRoomId.value): Promise<void> {
  if (!targetRoomId) return;
  const result = await call(apis.rooms.close({ sessionToken: sessionToken.value, roomId: targetRoomId }));
  if (!result) return;
  if (roomId.value === targetRoomId) {
    snapshot.value = null;
    runtimeState.value = null;
    roomId.value = '';
  }
  await refreshDashboard();
  if (!roomId.value || roomId.value === targetRoomId) await selectNextAvailableRoom(targetRoomId);
}

async function selectNextAvailableRoom(excludedRoomId: string): Promise<void> {
  const next = rooms.value.find(room => room.roomId !== excludedRoomId);
  if (!next) return;
  await selectRoom(next.roomId);
}

async function startShooterRoomQuick(): Promise<void> {
  battle.gameplayId = 2;
  battle.ruleSetId = 0;
  battle.configVersion = 1;
  battle.protocolVersion = 1;
  battle.worldType = 'shooter_battle';
  battle.syncTemplateId = battle.syncTemplateId || 'pure-state-authority';
  await call(apis.rooms.ready({ sessionToken: sessionToken.value, roomId: roomId.value, ready: true }));
  await startBattle();
}

async function startBattle(): Promise<void> {
  await call(apis.rooms.startBattle({ sessionToken: sessionToken.value, roomId: roomId.value, gameplayId: Number(battle.gameplayId || 0), ruleSetId: Number(battle.ruleSetId || 0), configVersion: Number(battle.configVersion || 1), protocolVersion: Number(battle.protocolVersion || 1), worldType: battle.worldType || null, clientId: 'admin-console', syncTemplateId: battle.syncTemplateId || null, syncModel: null, networkEnvironmentId: 'admin-console', carrierName: 'admin', enableAuthoritativeWorld: true, interpolationEnabled: true, inputDelayFrames: 0 }));
  await refreshDashboard();
  await refreshSkillDiagnostics();
}

async function startShooterSandbox(): Promise<void> {
  await call(apis.sandbox.start({ sandboxId: sandbox.sandboxId || 'default', region: region.value, serverId: serverId.value, botCount: Number(sandbox.botCount || 3), maxPlayers: Number(sandbox.maxPlayers || 4), tickRate: Number(sandbox.tickRate || 30), title: 'Shooter 后台沙盒', tags: { source: 'admin-console' } }));
  await refreshDashboard();
}

async function stopShooterSandbox(): Promise<void> {
  await call(apis.sandbox.stop({ sandboxId: sandbox.sandboxId || 'default' }));
  await refreshDashboard();
}

async function refreshAdminWorkspace(): Promise<void> {
  await refreshDashboard();
  await refreshClusterDiagnostics();
  await refreshSkillDiagnostics();
}

export function useAdminConsoleStore() {
  return {
    busy,
    sessionToken,
    region,
    serverId,
    selectedRoomType,
    roomId,
    gameplays,
    rooms,
    snapshot,
    runtimeState,
    dashboard,
    serverStatus,
    clusterDiagnostics,
    skillSummary,
    skillEvents,
    skillAnalysisModel,
    acceptanceBatch,
    acceptanceCase,
    acceptanceRunPlan,
    acceptanceArtifactDirectories,
    acceptanceTemplates,
    acceptanceLastRun,
    lastResponse,
    account,
    create,
    battle,
    skillLoadout,
    skillEventFilter,
    sandbox,
    serverOperation,
    acceptance,
    acceptanceAnalysisFilter,
    runtimeAnalysisFilter,
    selectedAcceptanceAnalysisNodeKey,
    selectedRuntimeAnalysisNodeKey,
    selectedGameplay,
    selectedRoomSummary,
    effectiveRoomId,
    hasSession,
    hasCurrentRoom,
    canOperateCurrentRoom,
    accountLabel,
    sandboxLabel,
    serverModeLabel,
    clusterLabel,
    skillBattleId,
    battleId,
    acceptancePassedCount,
    acceptanceFailedCount,
    acceptanceUnknownCount,
    acceptanceFilteredCases,
    acceptanceTracePreview,
    acceptanceTraceTree,
    acceptanceTraceFlat,
    acceptanceAssertionGroups,
    acceptanceAnalysisTree,
    acceptanceAnalysisFlat,
    acceptanceAnalysisFilterOptions,
    acceptanceAnalysisFilteredFlat,
    acceptanceAnalysisTimeline,
    acceptanceAnalysisEntityRelations,
    runtimeAnalysisFlat,
    runtimeAnalysisFilterOptions,
    runtimeAnalysisFilteredFlat,
    runtimeAnalysisTimeline,
    runtimeAnalysisEntityRelations,
    selectedAcceptanceAnalysisNode,
    selectedRuntimeAnalysisNode,
    skillDiagnosticsWarnings,
    skillAnalysisModelNotes,
    selectAcceptanceAnalysisNode,
    selectRuntimeAnalysisNode,
    formatBytes,
    formatDuration,
    applyGameplayDefaults,
    refreshDashboard,
    refreshServerStatus,
    refreshClusterDiagnostics,
    refreshSkillSummary,
    refreshSkillEvents,
    refreshSkillDiagnostics,
    refreshSkillAnalysisModel,
    refreshAcceptanceBatch,
    refreshAcceptanceCase,
    refreshAcceptanceRunPlan,
    refreshAcceptanceArtifactDirectories,
    refreshAcceptanceTemplates,
    refreshAcceptanceArtifacts,
    selectAcceptanceArtifactDirectory,
    applyAcceptanceTemplate,
    runAcceptanceAnalysis,
    setMaintenanceMode,
    setDrainMode,
    requestRestart,
    guestLogin,
    accountLogin,
    validateSession,
    logout,
    submitMobaLoadout,
    createRoom,
    selectRoom,
    joinRoom,
    restoreCurrentRoom,
    leaveRoom,
    markOffline,
    closeRoom,
    startShooterRoomQuick,
    startBattle,
    startShooterSandbox,
    stopShooterSandbox,
    refreshAdminWorkspace
  };
}
