@echo Off
set version=%1
if "%version%" == "" (
   set version=1
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\Build.proj /t:Go;BuildPackages /p:Configuration=Release;BUILD_NUMBER=%version% /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false