@echo Off
set version=%1
if "%version%" == "" (
   set version=1
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\Build.proj /t:Go;BuildPackages;BuildDocs /p:Configuration=Release;BUILD_NUMBER=%version%;PRERELEASE=true /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false