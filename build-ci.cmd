@echo Off

set config=%1
if "%config%" == "" (
   set config=Release
)
set version=%2
if "%version%" == "" (
   set version=1
)
set perfRun=%3
if /i not "%perfRun%" == "true" (
   set perfRun=false
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\Build.proj /t:CI /p:Configuration="%config%";BUILD_NUMBER=%version%;PRERELEASE=true /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false /p:PerfRun=%perfRun%