; =============================================================================
; Cyberarms Intrusion Detection System v3.0.0
; Inno Setup 6 Script
; =============================================================================

#define MyAppName        "Cyberarms Intrusion Detection System"
#define MyAppVersion     "3.0.0"
#define MyAppPublisher   "Cyberarms"
#define MyAppExeAdmin    "IntrusionDetectionAdmin.exe"
#define MyAppExeService  "CyberarmsIdsService.exe"
#define MyServiceName    "CyberarmsIds"
#define MyServiceDisplay "Cyberarms Intrusion Detection Service"
#define MyServiceDesc    "Monitors Windows event logs and blocks attacking IP addresses in real time using Windows Firewall."
#define MyEventLog       "Cyberarms"
#define MyEventSource    "Cyberarms Intrusion Detection"
#define SourceDir        "C:\Temp\CyberarmsPublish"
#define MyFwRuleName     "Cyberarms IDS Service"

[Setup]
AppId={{F3A8B2C1-4D7E-4F9A-8C3B-1E6D2A0F5B79}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup

DefaultDirName={autopf}\Cyberarms\Cyberarms Intrusion Detection
DefaultGroupName=Cyberarms
AllowNoIcons=yes

OutputDir=C:\Temp\CyberarmsInstaller
OutputBaseFilename=CyberarmsIDDS_Setup_v{#MyAppVersion}

Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Requires administrator — needed for service install and auditpol
PrivilegesRequired=admin
MinVersion=10.0
ArchitecturesInstallIn64BitMode=x64compatible

; Show the installed app's icon in Add/Remove Programs
UninstallDisplayIcon={app}\{#MyAppExeAdmin}
UninstallDisplayName={#MyAppName} {#MyAppVersion}

; Close running admin UI before upgrading
CloseApplications=yes
RestartApplications=no


; =============================================================================
[Languages]
; =============================================================================
Name: "english"; MessagesFile: "compiler:Default.isl"


; =============================================================================
[Tasks]
; =============================================================================
Name: "desktopicon"; \
  Description: "{cm:CreateDesktopIcon}"; \
  GroupDescription: "{cm:AdditionalIcons}"; \
  Flags: unchecked


; =============================================================================
[Files]
; =============================================================================
; All publish output — recurses into Plugins\ automatically.
; PDB files are excluded (debug symbols, not needed in production).
Source: "{#SourceDir}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "*.pdb"


; =============================================================================
[Registry]
; =============================================================================
; Create the custom Windows Event Log "Cyberarms" and register the source.
; TypesSupported=7 = Information|Warning|Error
Root: HKLM; \
  Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\{#MyEventLog}\{#MyEventSource}"; \
  ValueType: string; ValueName: "EventMessageFile"; \
  ValueData: "{sys}\EventCreate.exe"; \
  Flags: uninsdeletekey createvalueifdoesntexist
Root: HKLM; \
  Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\{#MyEventLog}\{#MyEventSource}"; \
  ValueType: dword; ValueName: "TypesSupported"; \
  ValueData: "7"; \
  Flags: createvalueifdoesntexist

; Create ProgramData directory for the database key file
Root: HKLM; \
  Subkey: "SOFTWARE\Cyberarms\IntrusionDetection"; \
  ValueType: string; ValueName: "InstallPath"; \
  ValueData: "{app}"; \
  Flags: uninsdeletekey


; =============================================================================
[Dirs]
; =============================================================================
; Ensure ProgramData folder exists with write access for the service (SYSTEM)
Name: "{commonappdata}\Cyberarms"; Permissions: everyone-modify


; =============================================================================
[Icons]
; =============================================================================
Name: "{group}\Cyberarms IDS Admin"; \
  Filename: "{app}\{#MyAppExeAdmin}"; \
  WorkingDir: "{app}"

Name: "{group}\Uninstall {#MyAppName}"; \
  Filename: "{uninstallexe}"

Name: "{commondesktop}\Cyberarms IDS Admin"; \
  Filename: "{app}\{#MyAppExeAdmin}"; \
  WorkingDir: "{app}"; \
  Tasks: desktopicon

; Startup shortcut — Admin auto-starts with Windows session
Name: "{commonstartup}\Cyberarms IDS Admin"; \
  Filename: "{app}\{#MyAppExeAdmin}"; \
  WorkingDir: "{app}"


; =============================================================================
[Run]
; =============================================================================

; --- 1. Windows Service ---
; Register the service (sc.exe requires the exact spacing around "=")
Filename: "{sys}\sc.exe"; \
  Parameters: "create ""{#MyServiceName}"" binPath= """"""{app}\{#MyAppExeService}"""""" DisplayName= ""{#MyServiceDisplay}"" start= auto obj= LocalSystem"; \
  Flags: runhidden; \
  StatusMsg: "Registering Windows service..."

; Set the service description
Filename: "{sys}\sc.exe"; \
  Parameters: "description ""{#MyServiceName}"" ""{#MyServiceDesc}"""; \
  Flags: runhidden

; Configure service failure actions: restart after 1 min on any of the first 3 failures
Filename: "{sys}\sc.exe"; \
  Parameters: "failure ""{#MyServiceName}"" reset= 86400 actions= restart/60000/restart/60000/restart/60000"; \
  Flags: runhidden

; Start the service
Filename: "{sys}\sc.exe"; \
  Parameters: "start ""{#MyServiceName}"""; \
  Flags: runhidden; \
  StatusMsg: "Starting Cyberarms service..."

; --- 2. Windows Audit Policies ---
; GUIDs are used instead of locale-dependent names so this works on any Windows language.
;
; Logon/Logoff category:
;   Logon          {0CCE9215-69AE-11D9-BED3-505054503030}
;   Logoff         {0CCE9216-69AE-11D9-BED3-505054503030}
;   Account Lockout{0CCE9217-69AE-11D9-BED3-505054503030}
; Account Logon category:
;   Credential Validation {0CCE923F-69AE-11D9-BED3-505054503030}
;   Kerberos Auth         {0CCE9242-69AE-11D9-BED3-505054503030}
;   Kerberos Ticket Ops   {0CCE9240-69AE-11D9-BED3-505054503030}
; Account Management category:
;   User Account Mgmt     {0CCE9235-69AE-11D9-BED3-505054503030}
;   Security Group Mgmt   {0CCE9237-69AE-11D9-BED3-505054503030}

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9215-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden; StatusMsg: "Configuring audit policies..."

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9216-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9217-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE923F-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9242-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9240-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9235-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9237-69AE-11D9-BED3-505054503030}"" /success:enable /failure:enable"; \
  Flags: runhidden

; --- 3. Windows Firewall ---
; Allow the service executable through the firewall (inbound + outbound).
; The service also calls the Windows Firewall COM API to block attacker IPs —
; running as SYSTEM it can do so without additional rules.
Filename: "{sys}\netsh.exe"; \
  Parameters: "advfirewall firewall add rule name=""{#MyFwRuleName}"" dir=in action=allow program=""{app}\{#MyAppExeService}"" enable=yes profile=any description=""Cyberarms IDS — allows the detection service to operate"""; \
  Flags: runhidden; StatusMsg: "Configuring Windows Firewall..."

Filename: "{sys}\netsh.exe"; \
  Parameters: "advfirewall firewall add rule name=""{#MyFwRuleName}"" dir=out action=allow program=""{app}\{#MyAppExeService}"" enable=yes profile=any"; \
  Flags: runhidden

; --- 4. Launch Admin (post-install, optional, not elevated) ---
Filename: "{app}\{#MyAppExeAdmin}"; \
  Description: "Launch Cyberarms IDS Admin"; \
  Flags: nowait postinstall skipifsilent


; =============================================================================
[UninstallRun]
; =============================================================================

; Stop service gracefully, wait 5 s, then force-delete
Filename: "{sys}\sc.exe"; \
  Parameters: "stop ""{#MyServiceName}"""; \
  Flags: runhidden; \
  RunOnceId: "StopService"

Filename: "{sys}\cmd.exe"; \
  Parameters: "/c timeout /t 5 /nobreak > nul"; \
  Flags: runhidden; \
  RunOnceId: "WaitForStop"

Filename: "{sys}\sc.exe"; \
  Parameters: "delete ""{#MyServiceName}"""; \
  Flags: runhidden; \
  RunOnceId: "DeleteService"

; Remove firewall rules
Filename: "{sys}\netsh.exe"; \
  Parameters: "advfirewall firewall delete rule name=""{#MyFwRuleName}"""; \
  Flags: runhidden; \
  RunOnceId: "RemoveFwRules"

; Revert audit policies to "No Auditing"
Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9215-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertLogon"

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9216-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertLogoff"

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9217-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertLockout"

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE923F-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertCredVal"

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9242-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertKerbAuth"

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9240-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertKerbTicket"

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9235-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertUserAcct"

Filename: "{sys}\auditpol.exe"; \
  Parameters: "/set /subcategory:""{0CCE9237-69AE-11D9-BED3-505054503030}"" /success:disable /failure:disable"; \
  Flags: runhidden; RunOnceId: "RevertSecGroup"


; =============================================================================
[Code]
; =============================================================================
// Runs before the installer copies files.
// If a previous version's service is running, stop and delete it so files
// can be overwritten (the EXE is locked while the service is running).

function ServiceExists(ServiceName: string): Boolean;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'query "' + ServiceName + '"',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

procedure StopAndDeleteService(ServiceName: string);
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop "' + ServiceName + '"',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  // Give it a moment to stop
  Sleep(3000);
  Exec(ExpandConstant('{sys}\sc.exe'), 'delete "' + ServiceName + '"',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(1000);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    // Stop existing service before copying files (upgrade scenario)
    if ServiceExists('{#MyServiceName}') then
    begin
      WizardForm.StatusLabel.Caption := 'Stopping existing service...';
      StopAndDeleteService('{#MyServiceName}');
    end;
  end;
end;

// Warn user before uninstall if the service is still running
function InitializeUninstall(): Boolean;
begin
  Result := True;
  if ServiceExists('{#MyServiceName}') then
  begin
    MsgBox('The Cyberarms service will be stopped and removed during uninstall.', mbInformation, MB_OK);
  end;
end;
