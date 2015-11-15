; This is an Inno Setup configuration file
; http://www.jrsoftware.org/isinfo.php

#define ApplicationVersion GetFileVersion('..\LcmsSpectator\bin\x64\Release\LcmsSpectator.exe')

[CustomMessages]
AppName=LcmsSpectator
[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nLcmsSpectator is a standalone Windows graphical user interface tool for viewing LC-MS data and identifications.

[Files]
; Application files
Source: LcmsSpectator\bin\x64\Release\layout.xml;                                DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\layoutdoc.xml;                             DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\LcmsSpectator.exe;                         DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\LcmsSpectator.exe.config;                  DestDir: {app}

; Nuget-Installed libraries
Source: LcmsSpectator\bin\x64\Release\GraphX.Controls.dll;                       DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\GraphX.PCL.Common.dll;                     DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\GraphX.PCL.Logic.dll;                      DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\OxyPlot.dll;                               DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\OxyPlot.Wpf.dll;                           DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\OxyPlot.Xps.dll;                           DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\QuickGraph.Data.dll;                       DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\QuickGraph.dll;                            DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\QuickGraph.Graphviz.dll;                   DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\QuickGraph.Serialization.dll;              DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\ReactiveUI.dll;                            DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\ReactiveUI.Events.dll;                     DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\Splat.dll;                                 DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\System.Reactive.Core.dll;                  DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\System.Reactive.Interfaces.dll;            DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\System.Reactive.Linq.dll;                  DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\System.Reactive.PlatformServices.dll;      DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\System.Reactive.Windows.Threading.dll;     DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\Xceed.Wpf.AvalonDock.dll;                  DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\Xceed.Wpf.AvalonDock.Themes.Aero.dll;      DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\Xceed.Wpf.AvalonDock.Themes.Metro.dll;     DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\Xceed.Wpf.AvalonDock.Themes.VS2010.dll;    DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\Xceed.Wpf.DataGrid.dll;                    DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\Xceed.Wpf.Toolkit.dll;                     DestDir: {app}

Source: LcmsSpectator\bin\x64\Release\System.Data.SQLite.dll;                    DestDir: {app}
Source: LcmsSpectator\bin\x64\Release\x64\SQLite.Interop.dll;                    DestDir: {app}\x64
Source: LcmsSpectator\bin\x64\Release\x86\SQLite.Interop.dll;                    DestDir: {app}\x86

; Separately-managed libraries
Source: Library\QuadTreeLib.dll;                                                 DestDir: {app}

; PNNLOmics
Source: Library\alglibnet2.dll;                                                  DestDir: {app}
Source: Library\PNNLOmics.dll;                                                   DestDir: {app}

; MTDBFramework
Source: Library\MTDBFramework\FluentNHibernate.dll;                                            DestDir: {app}
Source: Library\MTDBFramework\Iesi.Collections.dll;                                            DestDir: {app}
Source: Library\MTDBFramework\MTDBFramework.dll;                                               DestDir: {app}
Source: Library\MTDBFramework\NHibernate.dll;                                                  DestDir: {app}
Source: Library\MTDBFramework\NETPrediction.dll;                                               DestDir: {app}
Source: Library\MTDBFramework\PHRPReader.dll;                                                  DestDir: {app}

; InformedProteomics
Source: Library\InformedProteomics.Backend.dll;                                  DestDir: {app}
Source: Library\InformedProteomics.Scoring.dll;                                  DestDir: {app}
Source: Library\InformedProteomics.TopDown.dll;                                  DestDir: {app}
Source: Library\MathNet.Numerics.dll;                                            DestDir: {app}
Source: Library\ProteinFileReader.dll;                                           DestDir: {app}
Source: Library\SAIS.dll;                                                        DestDir: {app}
Source: Library\ThermoRawFileReaderDLL.dll;                                      DestDir: {app}

; PSI_Interface
Source: Library\PSI_Interface\PSI_Interface.dll;                                               DestDir: {app}
Source: Library\PSI_Interface\Ionic.Zip.dll;                                                   DestDir: {app}
Source: Library\PSI_Interface\zlib.net.dll;                                                    DestDir: {app}

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
OutputBaseFilename=LcmsSpectatorSetup
;VersionInfoVersion=1.1.16
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=LcmsSpectator
VersionInfoCopyright=PNNL
DisableFinishedPage=true
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
