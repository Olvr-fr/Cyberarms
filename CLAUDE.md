# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

Cyberarms IDDS (Intrusion Detection and Defense System) v3.0.0 — a Windows-only intrusion detection and prevention system. It monitors Windows Event Logs via agents, detects brute-force and credential attacks, and automatically blocks attacking IP addresses using the Windows Firewall COM API (`HNetCfg.FwPolicy2`).

Forked from the original .NET Framework codebase; fully migrated to **.NET 8 / net8.0-windows**.

---

## Build and publish

All projects target `net8.0-windows`. There is no top-level build script — build and publish each executable separately.

**Build (development check):**
```powershell
dotnet build Cyberarms.IntrusionDetection.Service/Cyberarms.IntrusionDetection.Service.csproj -c Release
dotnet build Cyberarms.IntrusionDetection.Admin/Cyberarms.IntrusionDetection.Admin.csproj -c Release
```

**Publish (self-contained, production — Service first, then Admin):**
```powershell
# Clean first
Remove-Item -Recurse -Force C:\Temp\CyberarmsPublish -ErrorAction SilentlyContinue

dotnet publish Cyberarms.IntrusionDetection.Service/Cyberarms.IntrusionDetection.Service.csproj -c Release -r win-x64 --self-contained true -o C:\Temp\CyberarmsPublish
dotnet publish Cyberarms.IntrusionDetection.Admin/Cyberarms.IntrusionDetection.Admin.csproj  -c Release -r win-x64 --self-contained true -o C:\Temp\CyberarmsPublish
```

Use `;` (not `&&`) to separate the two publish commands in scripts — piping through `grep` makes `&&` abort the chain when no errors are found (grep returns exit 1 on no matches).

**Result structure:**

```
C:\Temp\CyberarmsPublish\
  CyberarmsIdsService.exe         ← Windows service
  IntrusionDetectionAdmin.exe     ← Admin UI
  <.NET 8 runtime DLLs>
  Plugins\
    Cyberarms.IntrusionDetection.Base.Plugins.dll   ← AD Credential, Windows Base, RRAS, Kerberos
    Cyberarms.Agents.TerminalServer.dll             ← TLS/SSL
    Cyberarms.Agents.FileMaker.dll
    Cyberarms.Agents.FtpServer.dll
    Cyberarms.Agents.Smtp.dll
    Cyberarms.Agents.SqlServer.dll
    Cyberarms.Agents.Bind9.dll                      ← optional (Linux BIND9 on Windows)
    Cyberarms.Agents.MySql.dll                      ← optional (MySQL for Windows)
    Cyberarms.Agents.WebSecurity.dll                ← optional (IIS + Cyberarms module)
```

`Cyberarms.Agents.MailServer.dll` is intentionally **excluded** — it duplicates SMTP functionality already covered by `Cyberarms.Agents.Smtp.dll` and adds POP3 which is not in the original product.

**How Plugins\ gets populated during publish:**
Agent DLLs are declared as `None` items with `TargetPath` + `CopyToPublishDirectory=PreserveNewest` in `Admin.csproj`. This hooks into the standard SDK publish pipeline. The `CopyAgentsToPlugins` MSBuild target (also in `Admin.csproj`) handles the same copy for local `bin\` builds.

**Run tests:**
```powershell
dotnet test Cyberarms.IntrusionDetection.Shared.Test/
dotnet test Cyberarms.IntrusionDetection.Service.Test/
```

**Register and start the service (after copying to final install path):**
```powershell
sc.exe create "CyberarmsIds" binPath= '"C:\Program Files\Cyberarms\Cyberarms Intrusion Detection\CyberarmsIdsService.exe"' DisplayName= "Cyberarms Intrusion Detection Service" start= auto
sc.exe start "CyberarmsIds"
```

---

## Architecture

### Project dependency graph

```
Cyberarms.IntrusionDetection.Api              ← plugin contract only (interfaces, base classes)
        ↑
Cyberarms.IntrusionDetection.Shared           ← database, config, agent loading, locks, crypto
        ↑                         ↑
Cyberarms.IntrusionDetection.Service    Cyberarms.IntrusionDetection.Admin
(Windows service — blocks IPs)          (WinForms admin UI — reads same SQLite DB)

Cyberarms.IntrusionDetection.Base.Plugins  ← 4 built-in Windows agents
Cyberarms.Agents.*                         ← optional agents, loaded at runtime from Plugins\
```

Agents are **not** referenced at compile time by Service or Admin. `Admin.csproj` has `ProjectReference` entries with `ReferenceOutputAssembly="false"` so they are built but not linked.

### Attack detection flow

1. An agent (`AgentPlugin` subclass) subscribes to a Windows EventLog query or raw socket.
2. On detection it fires `AttackDetected(sender, NotificationEventArgs{ IpAddress, EventId, CreateDate })`.
3. `PaladinService` receives the event, evaluates soft-lock / hard-lock thresholds from the DB.
4. `FirewallPolicyManager.Block(ip)` appends the IP to the single firewall rule `Blocked by Cyberarms Intrusion Detection_BlockAttacker_AllPorts`.
5. A `Lock` record is persisted to SQLite.

### Agent plugin contract

Every agent DLL must contain at least one `public`, non-abstract class implementing `IAgentPlugin`. `AgentLoaderProxy` discovers these via `Assembly.LoadFile` + reflection on every DLL in `Plugins\`.

Agents **must also** implement `IExtendedInformation` — without it `AgentLoaderProxy` falls back to `type.FullName` as `DisplayName` and uses generic icons.

```csharp
public interface IExtendedInformation {
    string DisplayName { get; set; }   // shown in Admin AGENTS tab
    Image  Icon           { get; set; }
    Image  SelectedIcon   { get; set; }
    Image  UnselectedIcon { get; set; }
    Guid   Id             { get; }     // must be globally unique per agent class
}
```

Override `OnStartAgent` / `OnStopAgent` / `OnPauseAgent` / `OnContinueAgent` from `AgentPlugin`. Call `OnAttackDetected(this, args)` when an attack is detected.

### Agent display order in Admin UI

`IddsAdmin.InitAgentSettings()` sorts agents before rendering using a hardcoded `AgentDisplayOrder` string array. The 9 canonical agents appear first (in product order); unknown agents are sorted alphabetically after them. When adding a new canonical agent, add its `DisplayName` to that array.

### Database

SQLite via `Microsoft.Data.Sqlite`, encrypted with `SQLitePCLRaw.bundle_e_sqlcipher`.

- File: `%PROGRAMDATA%\Cyberarms\cyberarms.idds.dbf`
- Key resolution order: env var `CYBERARMS_DB_KEY` → DPAPI file `%PROGRAMDATA%\Cyberarms\cyberarms.key` → auto-generated 32-byte random key (stored DPAPI-protected, `LocalMachine` scope)
- `Database.Instance.Configure(directory)` must be called before any query. Called in `PaladinService.Init()` and Admin startup.
- Schema migrations: `DbUpgrader` in `Shared/Db/`.

### Windows service

- SCM name: `CyberarmsIds`
- Class: `Service : ServiceBase` in `PaladinService.cs`
- `OnStart()` launches `StartService()` via `Task.Run(() => StartService())`.
- `StartService()` calls `Init()` → `InitAgentConfiguration()` → `LoadAgents()` → `StartAgents()`.
- Crash log: `C:\ProgramData\Cyberarms\crash.log` — written by `Program.WriteCrashLog(source, ex)`, hooked on `AppDomain.CurrentDomain.UnhandledException` and inside the `StartService()` catch block.
- Event Log: name `Cyberarms`, source `Cyberarms Intrusion Detection`.

### Admin UI

WinForms. Communicates with the service exclusively through the shared SQLite database — no IPC, no named pipe. The Admin only needs the service to be running for live data; it can open the DB directly when the service is stopped.

### Firewall integration

`FirewallPolicyManager` (blocking rules via `HNetCfg.FwPolicy2`) and `FirewallManager` (port/app rules via `HNetCfg.FwMgr`) use Windows Firewall COM API. Both constructors null-check the result of `Type.GetTypeFromProgID(...)` before passing it to `Activator.CreateInstance` — the COM ProgID may not be registered in some container or non-standard environments.

---

## Key constants (`Globals.cs` in Shared)

| Constant | Value |
| --- | --- |
| Service SCM name | `CyberarmsIds` |
| Windows Event Log name | `Cyberarms` |
| Windows Event Log source | `Cyberarms Intrusion Detection` |
| Firewall block rule name | `Blocked by Cyberarms Intrusion Detection_BlockAttacker_AllPorts` |
| Firewall group name | `Cyberarms Intrusion Detection` |
| Plugins subfolder | `Plugins` |
| DB filename | `cyberarms.idds.dbf` |

---

## .NET 8 migration — fixes applied (do not revert)

| Problem | Fix applied |
| --- | --- |
| `Delegate.BeginInvoke` in `PaladinService.OnStart` | → `Task.Run(() => StartService())` |
| `AppDomain.Unload(domain)` in `SecurityAgents` | → removed (no-op comment); agents share current domain |
| `MD5CryptoServiceProvider` in `CryptoLib` | → `MD5.Create()` |
| `TripleDESCryptoServiceProvider` in `CryptoLib` | → `TripleDES.Create()` |
| `Activator.CreateInstance(Type.GetTypeFromProgID(...))` without null check | → null-check added in both `FirewallManager` and `FirewallPolicyManager` |
| `EventLog.WriteEntry` calls throwing on startup | → all wrapped in `try { } catch { }` |
