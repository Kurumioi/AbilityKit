import { AdminApiClient } from './adminApiClient';
import type { AddRoomRobotsResponse, AdminClusterDiagnostics, AdminDashboardResponse, AdminServerOperationResponse, AdminServerStatus, AdminSkillAcceptanceArtifactDirectoryList, AdminSkillAcceptanceBatch, AdminSkillAcceptanceCase, AdminSkillAcceptanceDeleteResponse, AdminSkillAcceptanceRunPlan, AdminSkillAcceptanceRunResponse, AdminSkillAcceptanceTemplateList, AdminSkillAnalysisArtifact, AdminSkillAnalysisArtifactDirectoryList, AdminSkillAnalysisArtifactList, AdminSkillAnalysisModel, AdminSkillDiagnosticsEvents, AdminSkillDiagnosticsSummary, AdminStartRoomBattleResponse, CreateRoomResponse, RestoreRoomResponse, RoomRuntimeState, RoomSnapshot, SessionResponse, ShooterSandboxState, ShooterWorldDiagnostics } from '../types';

export class AdminDashboardApi {
  public constructor(private readonly client: AdminApiClient) {}

  public dashboard(request: unknown) {
    return this.client.request<AdminDashboardResponse>('/api/admin/dashboard', request);
  }
}

export class AdminOpsApi {
  public constructor(private readonly client: AdminApiClient) {}

  public status() {
    return this.client.request<AdminServerStatus>('/api/admin/server/status', undefined, 'GET');
  }

  public maintenance(request: unknown) {
    return this.client.request<AdminServerOperationResponse>('/api/admin/server/maintenance', request);
  }

  public drain(request: unknown) {
    return this.client.request<AdminServerOperationResponse>('/api/admin/server/drain', request);
  }

  public restartRequest(request: unknown) {
    return this.client.request<AdminServerOperationResponse>('/api/admin/server/restart-request', request);
  }
}

export class AdminClusterApi {
  public constructor(private readonly client: AdminApiClient) {}

  public diagnostics() {
    return this.client.request<AdminClusterDiagnostics>('/api/admin/cluster/diagnostics', undefined, 'GET');
  }
}

export class AdminSkillApi {
  public constructor(private readonly client: AdminApiClient) {}

  public summary(query: string) {
    return this.client.request<AdminSkillDiagnosticsSummary>(`/api/admin/skills/summary${query}`, undefined, 'GET');
  }

  public events(query: string) {
    return this.client.request<AdminSkillDiagnosticsEvents>(`/api/admin/skills/events${query}`, undefined, 'GET');
  }

  public analysisModel() {
    return this.client.request<AdminSkillAnalysisModel>('/api/admin/skills/analysis-model', undefined, 'GET');
  }

  public analysisArtifactDirectories() {
    return this.client.request<AdminSkillAnalysisArtifactDirectoryList>('/api/admin/skills/analysis-artifacts/directories', undefined, 'GET');
  }

  public analysisArtifacts(query: string) {
    return this.client.request<AdminSkillAnalysisArtifactList>(`/api/admin/skills/analysis-artifacts${query}`, undefined, 'GET');
  }

  public analysisArtifact(fileName: string, query: string) {
    return this.client.request<AdminSkillAnalysisArtifact>(`/api/admin/skills/analysis-artifacts/${encodeURIComponent(fileName)}${query}`, undefined, 'GET');
  }

  public acceptanceArtifactDirectories() {
    return this.client.request<AdminSkillAcceptanceArtifactDirectoryList>('/api/admin/skills/acceptance/artifact-directories', undefined, 'GET');
  }

  public acceptanceTemplates() {
    return this.client.request<AdminSkillAcceptanceTemplateList>('/api/admin/skills/acceptance/templates', undefined, 'GET');
  }

  public acceptanceBatch(query: string) {
    return this.client.request<AdminSkillAcceptanceBatch>(`/api/admin/skills/acceptance/batch${query}`, undefined, 'GET');
  }

  public acceptanceDelete(request: unknown) {
    return this.client.request<AdminSkillAcceptanceDeleteResponse>('/api/admin/skills/acceptance/delete', request);
  }

  public acceptanceRun(request: unknown) {
    return this.client.request<AdminSkillAcceptanceRunResponse>('/api/admin/skills/acceptance/run', request);
  }

  public acceptanceCase(caseId: string, query: string) {
    return this.client.request<AdminSkillAcceptanceCase>(`/api/admin/skills/acceptance/cases/${encodeURIComponent(caseId)}${query}`, undefined, 'GET');
  }

  public acceptanceRunPlan(query: string) {
    return this.client.request<AdminSkillAcceptanceRunPlan>(`/api/admin/skills/acceptance/run-plan${query}`, undefined, 'GET');
  }
}

export class RoomApi {
  public constructor(private readonly client: AdminApiClient) {}

  public create(request: unknown) {
    return this.client.request<CreateRoomResponse>('/api/admin/rooms/create', request);
  }

  public join(request: unknown) {
    return this.client.request<CreateRoomResponse>('/api/admin/rooms/join', request);
  }

  public restoreCurrent(request: unknown) {
    return this.client.request<RestoreRoomResponse>('/api/admin/rooms/restore-current', request);
  }

  public leave(request: unknown) {
    return this.client.request('/api/admin/rooms/leave', request);
  }

  public close(request: unknown) {
    return this.client.request('/api/admin/rooms/close', request);
  }

  public markOffline(request: unknown) {
    return this.client.request<RoomRuntimeState>('/api/admin/rooms/mark-offline', request);
  }

  public ready(request: unknown) {
    return this.client.request('/api/admin/rooms/ready', request);
  }

  public startBattle(request: unknown) {
    return this.client.request<AdminStartRoomBattleResponse>('/api/admin/rooms/start-battle', request);
  }

  public addRobots(request: unknown) {
    return this.client.request<AddRoomRobotsResponse>('/api/admin/rooms/add-robots', request);
  }

  public pickHero(request: unknown) {
    return this.client.request<RoomSnapshot>('/api/admin/rooms/pick-hero', request);
  }

  public shooterWorld(query: string) {
    return this.client.request<ShooterWorldDiagnostics>(`/api/admin/shooter/world${query}`, undefined, 'GET');
  }
}

export class SandboxApi {
  public constructor(private readonly client: AdminApiClient) {}

  public start(request: unknown) {
    return this.client.request<ShooterSandboxState>('/api/shooter-sandbox/start', request);
  }

  public state(sandboxId: string) {
    const suffix = sandboxId ? `/${encodeURIComponent(sandboxId)}` : '';
    return this.client.request<ShooterSandboxState>(`/api/shooter-sandbox${suffix}`, undefined, 'GET');
  }

  public stop(request: unknown) {
    return this.client.request('/api/shooter-sandbox/stop', request);
  }
}

export class SessionApi {
  public constructor(private readonly client: AdminApiClient) {}

  public guestLogin() {
    return this.client.request<SessionResponse>('/api/guest/login', {}, 'POST');
  }

  public accountLogin(request: unknown) {
    return this.client.request<SessionResponse>('/api/accounts/login', request);
  }

  public validate(request: unknown) {
    return this.client.request<SessionResponse>('/api/session/validate', request);
  }

  public logout(request: unknown) {
    return this.client.request('/api/session/logout', request);
  }
}

export class AdminDomainApis {
  public readonly dashboard: AdminDashboardApi;
  public readonly ops: AdminOpsApi;
  public readonly cluster: AdminClusterApi;
  public readonly skills: AdminSkillApi;
  public readonly rooms: RoomApi;
  public readonly sandbox: SandboxApi;
  public readonly session: SessionApi;

  public constructor(client = new AdminApiClient()) {
    this.dashboard = new AdminDashboardApi(client);
    this.ops = new AdminOpsApi(client);
    this.cluster = new AdminClusterApi(client);
    this.skills = new AdminSkillApi(client);
    this.rooms = new RoomApi(client);
    this.sandbox = new SandboxApi(client);
    this.session = new SessionApi(client);
  }
}
