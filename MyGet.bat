@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)

set version=
if not "%PackageVersion%" == "" (
   set version=-Version %PackageVersion%
)

REM Packages restore
%nuget% restore

REM Build
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Lucene.Net.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false
if not "%errorlevel%"=="0" goto failure

REM Unit tests
"%GallioEcho%" test\bin\%config%\Lucene.Net.Tests.dll
if not "%errorlevel%"=="0" goto failure

REM Package
mkdir Build
cmd /c %nuget% pack "src\core\Lucene.Net.csproj" -symbols -o Build -p Configuration=%config% %version%
if not "%errorlevel%"=="0" goto failure

:success
exit 0

:failure
exit -1
