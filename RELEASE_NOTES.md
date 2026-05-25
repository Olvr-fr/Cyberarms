# Release Notes

## v3.0.0 — .NET 8 Migration & Service Stability

### Breaking changes
- Minimum runtime: **.NET 8 / Windows** (self-contained publish, no runtime install required).
- Database encrypted with **AES-256 via SQLCipher**. Existing v2.x unencrypted databases are not compatible.
- Agent DLL discovery path changed from the application root to `Plugins\` subfolder.

---

### Service — crash fixes (PlatformNotSupportedException on startup)

The root cause of the service failing to start on .NET 8 was a chain of four APIs removed or broken since .NET 5:

| File | Problem | Fix |
| --- | --- | --- |
| `PaladinService.cs` | `Delegate.BeginInvoke` not supported in .NET 5+ | Replaced with `Task.Run(() => StartService())` |
| `SecurityAgents.cs` | `AppDomain.Unload()` throws `PlatformNotSupportedException` | Removed — agents share the current AppDomain in .NET 8 |
| `CryptoLib.cs` | `MD5CryptoServiceProvider` / `TripleDESCryptoServiceProvider` obsolete CSP classes | Replaced with `MD5.Create()` / `TripleDES.Create()` |
| `FirewallManager.cs` | `Activator.CreateInstance(Type.GetTypeFromProgID(...))` with no null-check | Added null-check — `GetTypeFromProgID` returns null if COM not registered |
| `FirewallPolicyManager.cs` | Same as above for `HNetCfg.FwPolicy2` | Same fix |

All `EventLog.WriteEntry` calls in `WindowsLogManager.cs`, `Program.cs`, `PaladinService.cs` and `FirewallPolicyManager.cs` are wrapped in `try { } catch { }` — Windows Event Log API throws in restricted environments.

### Service — crash diagnostic log

`Program.cs` now registers a global `AppDomain.CurrentDomain.UnhandledException` handler and a `StartService()` catch that write full stack traces to:

```
C:\ProgramData\Cyberarms\crash.log
```

This is intentionally left active to aid diagnosis in production.

---

### Admin — Plugins\ publish pipeline

`Admin.csproj` previously had no reliable mechanism to copy agent DLLs to `Plugins\` during `dotnet publish -r win-x64`. Fixed by declaring each agent DLL as a `None` item with `TargetPath=Plugins\<name>` and `CopyToPublishDirectory=PreserveNewest`, which hooks into the standard SDK publish pipeline regardless of RID.

`Cyberarms.Agents.MailServer.dll` is **excluded** from `Plugins\`. It contains `SmtpAgent` ("SMTP Mail Security Agent") and `Pop3Agent` ("POP3 Mail Security Agent") which are not part of the original product and duplicate SMTP functionality already covered by `Cyberarms.Agents.Smtp.dll`.

### Admin — Base.Plugins agents included

`Cyberarms.IntrusionDetection.Base.Plugins` added as a build dependency and publish item. This brings the four Windows-native agents that were missing from `Plugins\`:

- AD Credential Validation Security Agent
- Windows Base Security Agent
- RRAS Security Agent - Routing and Remote Access
- Kerberos pre-authentication Security Agent

### Admin — Agent display order

The AGENTS tab now shows agents in canonical product order regardless of DLL load order (which is filesystem-alphabetical):

1. AD Credential Validation Security Agent
2. Windows Base Security Agent
3. RRAS Security Agent - Routing and Remote Access
4. Kerberos pre-authentication Security Agent
5. TLS/SSL Security Agent
6. FileMaker Security Agent
7. FTP Security Agent
8. SMTP Security Agent
9. SQL Server Security Agent

Additional agents (Bind9, MySql, WebSecurity) appear after the nine canonical agents, sorted alphabetically.

---

### Agents — IExtendedInformation

`Bind9DDoSKiller`, `Pop3Agent` (MailServer), and `SmtpAgent` (MailServer) now implement `IExtendedInformation`. Without this interface `AgentLoaderProxy` falls back to `type.FullName` as the display name.

---

### Documentation

- `CLAUDE.md` added — developer reference covering build commands, publish steps, architecture, plugin contract, database key management, and .NET 8 migration inventory.

---

## v2.2.0 and earlier

See git log. Original .NET Framework codebase; no formal release notes maintained.
