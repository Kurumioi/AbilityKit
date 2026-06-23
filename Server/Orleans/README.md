# AbilityKit Orleans Server

This folder contains the Orleans server, TCP gateway, room/battle grains, and Shooter smoke automation used by local development and integration validation.

## Projects

- `AbilityKit.Orleans.Contracts`: Grain interfaces, DTOs, and shared result status codes.
- `AbilityKit.Orleans.Grains`: Room, battle, and gameplay grain implementations.
- `AbilityKit.Orleans.Gateway`: HTTP/TCP gateway and room operation error mapping.
- `AbilityKit.Orleans.Host`: Standalone silo host.
- `AbilityKit.Orleans.ShooterSmoke`: Self-hosted Shooter TCP Gateway E2E smoke runner.
- `AbilityKit.Orleans.Grains.Tests`: Grain/runtime adapter tests.
- `AbilityKit.Orleans.Gateway.Tests`: Gateway error mapping tests.

## Run

Use the one-click launcher to build and start the Orleans Host plus HTTP/TCP Gateway for local development:

```powershell
.\tools\start_abilitykit.ps1
```

The default `dev` profile serves the admin console at `http://localhost:5001/admin` and the health endpoint at `http://localhost:5001/health`.

List configured launch profiles:

```powershell
.\tools\start_abilitykit.ps1 -ListProfiles
```

Run a named environment profile:

```powershell
.\tools\start_abilitykit.ps1 -Profile ops-a
```

Run multiple local environments on one machine by starting profiles with isolated ports and cluster IDs:

```powershell
.\tools\start_abilitykit.ps1 -Profile ops-a,ops-b -NoBuild
```

Override ports directly when a profile needs temporary port changes:

```powershell
.\tools\start_abilitykit.ps1 -Profile dev -GatewayPort 5051 -SiloPort 11511 -SiloGatewayPort 30500 -TcpPort 4050
```

Profiles are defined in `tools\abilitykit_launch_profiles.json`. Each profile owns its `instanceName`, `clusterId`, `serviceId`, HTTP gateway port, Orleans silo port, Orleans gateway port, and TCP gateway port. Logs are written under `logs\<instanceName>\`.

`tools\restart_all.ps1` remains as a compatibility wrapper and now delegates to the profile launcher.

## Shooter Smoke

The Shooter smoke runner self-hosts an Orleans silo and TCP gateway, then validates guest login, room creation/readiness, battle start, snapshot push, local input submission, stale snapshot rejection, late join projection, reconnect projection, and battle cleanup.

```powershell
.\tools\run_shooter_smoke.ps1 -Configuration Debug -TcpPort 41001
```

```cmd
tools\run_shooter_smoke.bat -Configuration Debug -TcpPort 41001
```

Use `-NoBuild` when the smoke project has already been built. Use `-TcpPort` to avoid local port conflicts in parallel runs.

## Validation

```cmd
dotnet test src\AbilityKit.Orleans.Grains.Tests\AbilityKit.Orleans.Grains.Tests.csproj --filter ShooterBattleRuntimeAdapterTests
```

```cmd
dotnet test src\AbilityKit.Orleans.Gateway.Tests\AbilityKit.Orleans.Gateway.Tests.csproj
```
