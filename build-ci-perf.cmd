@echo OFF

set build=%1
if "%build%" == "" (
   set build=build-0
)
set branch=%2
if "%branch%" == "" (
   set branch=dev
)

set privateRun=true
if /i "%branch%" == "dev" (
  set privateRun=false
)
if /i "%branch%" == "release" (
  set privateRun=false
)

call \PerfRuns\SignalR-Env.cmd

cmd /c build-ci Release 1 true

if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

cmd /c %PerfToolsBin%\PerfRun /TeamName:SignalR /LabConfigPath:%PerfLabConfig% /RemotePasswordFile:%PerfPasswordFile% /UpdateMetadata:true /Import:true /Branch:%branch% /Build:%build% /Run:artifacts\Release\Microsoft.AspNet.SignalR.Stress /Archive:true /PrivateRun:%privateRun%
