@echo Off

rem Enforce package restore to avoid build issues. See http://go.microsoft.com/fwlink/?LinkID=317568 for more details
msbuild .nuget\NuGet.targets /t:RestorePackages

set target=%1
if "%target%" == "" (
   set target=BuildCmd
)
set config=%2
if "%config%" == "" (
   set config=Debug
)

msbuild Build\Build.proj /t:"%target%" /p:Configuration="%config%" /m /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false