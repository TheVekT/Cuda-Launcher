#define MyAppName "Cuda Launcher"
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#define MyAppPublisher "TheVekT"
#define MyAppExeName "Cuda Launcher.exe"
#ifndef MySourceDir
  #define MySourceDir "..\Build\Release"
#endif
#ifndef MyOutputDir
  #define MyOutputDir "..\Artifacts"
#endif
#ifndef MyIconFile
  #define MyIconFile "..\Launcher.UI.WPF\icon.ico"
#endif

[Setup]
AppId={{D37E6F40-8C84-4899-B0F2-72124E5B09E7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Cuda-Launcher
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
OutputDir={#MyOutputDir}
OutputBaseFilename=Cuda-Launcher-{#MyAppVersion}-Setup
SetupIconFile={#MyIconFile}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent