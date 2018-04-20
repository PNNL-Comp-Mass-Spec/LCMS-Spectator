; This is an Inno Setup configuration file
; http://www.jrsoftware.org/isinfo.php

#define ApplicationVersion GetFileVersion('..\LcmsSpectator\bin\Release\LcmsSpectator.exe')

[CustomMessages]
AppName=LcmsSpectator
[Messages]
; WelcomeLabel2 is set using the code section
;WelcomeLabel2=This will install [name/ver] on your computer.%n%nLcmsSpectator is a standalone Windows graphical user interface tool for viewing LC-MS data and identifications.

[Files]
; Application files
Source: LcmsSpectator\bin\Release\LcmsSpectator.exe;                         DestDir: {app}
Source: LcmsSpectator\bin\Release\LcmsSpectator.exe.config;                  DestDir: {app}

; Nuget-Installed libraries
Source: LcmsSpectator\bin\Release\GraphX.Controls.dll;                       DestDir: {app}
Source: LcmsSpectator\bin\Release\GraphX.PCL.Common.dll;                     DestDir: {app}
Source: LcmsSpectator\bin\Release\GraphX.PCL.Logic.dll;                      DestDir: {app}
Source: LcmsSpectator\bin\Release\OxyPlot.dll;                               DestDir: {app}
Source: LcmsSpectator\bin\Release\OxyPlot.Wpf.dll;                           DestDir: {app}
Source: LcmsSpectator\bin\Release\QuickGraph.Data.dll;                       DestDir: {app}
Source: LcmsSpectator\bin\Release\QuickGraph.dll;                            DestDir: {app}
Source: LcmsSpectator\bin\Release\QuickGraph.Graphviz.dll;                   DestDir: {app}
Source: LcmsSpectator\bin\Release\QuickGraph.Serialization.dll;              DestDir: {app}
Source: LcmsSpectator\bin\Release\ReactiveUI.dll;                            DestDir: {app}
Source: LcmsSpectator\bin\Release\Splat.dll;                                 DestDir: {app}
Source: LcmsSpectator\bin\Release\System.Reactive.Core.dll;                  DestDir: {app}
Source: LcmsSpectator\bin\Release\System.Reactive.Interfaces.dll;            DestDir: {app}
Source: LcmsSpectator\bin\Release\System.Reactive.Linq.dll;                  DestDir: {app}
Source: LcmsSpectator\bin\Release\System.Reactive.PlatformServices.dll;      DestDir: {app}
Source: LcmsSpectator\bin\Release\System.Reactive.Windows.Threading.dll;     DestDir: {app}
Source: LcmsSpectator\bin\Release\Xceed.Wpf.AvalonDock.dll;                  DestDir: {app}
Source: LcmsSpectator\bin\Release\Xceed.Wpf.AvalonDock.Themes.Aero.dll;      DestDir: {app}
Source: LcmsSpectator\bin\Release\Xceed.Wpf.AvalonDock.Themes.Metro.dll;     DestDir: {app}
Source: LcmsSpectator\bin\Release\Xceed.Wpf.AvalonDock.Themes.VS2010.dll;    DestDir: {app}
Source: LcmsSpectator\bin\Release\Xceed.Wpf.DataGrid.dll;                    DestDir: {app}
Source: LcmsSpectator\bin\Release\Xceed.Wpf.Toolkit.dll;                     DestDir: {app}

Source: LcmsSpectator\bin\Release\System.Data.SQLite.dll;                    DestDir: {app}
Source: LcmsSpectator\bin\Release\x64\SQLite.Interop.dll;                    DestDir: {app}\x64
Source: LcmsSpectator\bin\Release\x86\SQLite.Interop.dll;                    DestDir: {app}\x86

; PSI_Interface
Source: LcmsSpectator\bin\Release\PSI_Interface.dll;                         DestDir: {app}
Source: LcmsSpectator\bin\Release\DotNetZip.dll;                             DestDir: {app}

; Separately-managed libraries
Source: Library\QuadTreeLib\QuadTreeLib.dll;                                 DestDir: {app}
Source: Library\MwtWinDll.dll;                                               DestDir: {app}

; PNNLOmics
Source: Library\InformedProteomics\alglibnet2.dll;                           DestDir: {app}
Source: Library\InformedProteomics\PNNLOmics.dll;                            DestDir: {app}

; MTDBFramework
Source: Library\MTDBFramework\FluentNHibernate.dll;                          DestDir: {app}
Source: Library\MTDBFramework\Iesi.Collections.dll;                          DestDir: {app}
Source: Library\MTDBFramework\MTDBFramework.dll;                             DestDir: {app}
Source: Library\MTDBFramework\NHibernate.dll;                                DestDir: {app}
Source: Library\MTDBFramework\NETPrediction.dll;                             DestDir: {app}
Source: Library\MTDBFramework\PHRPReader.dll;                                DestDir: {app}

; InformedProteomics
Source: Library\InformedProteomics\InformedProteomics.Backend.dll;           DestDir: {app}
Source: Library\InformedProteomics\InformedProteomics.FeatureFinding.dll;    DestDir: {app}
Source: Library\InformedProteomics\InformedProteomics.Scoring.dll;           DestDir: {app}
Source: Library\InformedProteomics\InformedProteomics.TopDown.dll;           DestDir: {app}
Source: Library\InformedProteomics\MathNet.Numerics.dll;                     DestDir: {app}
Source: Library\InformedProteomics\ProteinFileReader.dll;                    DestDir: {app}
Source: Library\InformedProteomics\SAIS.dll;                                 DestDir: {app}
Source: Library\InformedProteomics\ThermoRawFileReader.dll;                  DestDir: {app}

; Xceed Themes
Source: Library\Theme\Xceed.Wpf.Themes.dll;                                  DestDir: {app}
Source: Library\Theme\Xceed.Wpf.Themes.Office2007.dll;                       DestDir: {app}

[Dirs]
Name: {commonappdata}\LcmsSpectator; Flags: uninsalwaysuninstall

[Icons]
Name: {commondesktop}\{cm:AppName}; Filename: {app}\LcmsSpectator.exe; Tasks: desktopicon; IconFilename: {app}..\Resources\iconSmall.ico; Comment: LcmsSpectator; IconIndex: 0
Name: {userappdata}\Microsoft\Internet Explorer\Quick Launch\{cm:AppName}; Filename: {app}\LcmsSpectator.exe; Tasks: quicklaunchicon; IconFilename: {app}..\Resources\iconSmall.ico; Comment: LcmsSpectator; IconIndex: 0
Name: {group}\LCMS Spectator; Filename: {app}\LcmsSpectator.exe; Comment: LCMS Spectator

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
[Setup]
; As AnyCPU, we can install as 32-bit or 64-bit, so allow installing on 32-bit Windows, but make sure it installs as 64-bit on 64-bit Windows
ArchitecturesAllowed=x64 x86
ArchitecturesInstallIn64BitMode=x64
AppName=LcmsSpectator
AppVersion={#ApplicationVersion}
;AppVerName=LcmsSpectator
AppID=LcmsSpectator
AppPublisher=Pacific Northwest National Laboratory
AppPublisherURL=http://omics.pnl.gov/software
AppSupportURL=http://omics.pnl.gov/software
AppUpdatesURL=http://omics.pnl.gov/software
DefaultDirName={pf}\LcmsSpectator
DefaultGroupName=PAST Toolkit
AppCopyright=© PNNL
PrivilegesRequired=poweruser
SetupIconFile=LcmsSpectator\Resources\iconSmall.ico
OutputBaseFilename=LcmsSpectatorSetup
;VersionInfoVersion=1.1.16
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=LcmsSpectator
VersionInfoCopyright=PNNL
ShowLanguageDialog=no
ChangesAssociations=true
EnableDirDoesntExistWarning=true
AlwaysShowDirOnReadyPage=true
ShowTasksTreeLines=true
OutputDir=Installer\Output
SourceDir=..\
Compression=lzma
SolidCompression=yes

[UninstallDelete]
Name: {app}; Type: filesandordirs
Name: {app}\Tools; Type: filesandordirs

[Code]
function GetInstallArch(): String;
begin
  { Return a user value }
  if Is64BitInstallMode then
    Result := '64'
  else
    Result := '32';
end;

procedure InitializeWizard;
var
  message2_a: string;
  message2_b: string;
  message2_c: string;
  message2: string;
  appname: string;
  appversion: string;
begin
  appname := '{#SetupSetting("AppName")}';
  appversion := '{#SetupSetting("AppVersion")}';
  (* #13 is carriage return, #10 is new line *)
  message2_a := 'This will install ' + appname + ' version ' + appversion + ' on your computer.' + #10#10 + 
                'LcmsSpectator is a standalone Windows graphical user interface tool for viewing LC-MS data and identifications.'
                + #10#10#10#10 + 'NOTICE:' + #10 + 'Reading of some data files requires access to a ';
  message2_b := '-bit ProteoWizard installation. Please install ';
  message2_c := '-bit ProteoWizard before using the program to avoid errors.' + #10#10;
  message2 := message2_a + GetInstallArch + message2_b + GetInstallArch + message2_c;
  WizardForm.WelcomeLabel2.Caption := message2;
end;
