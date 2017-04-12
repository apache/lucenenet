@echo off
GOTO endcommentblock
:: -----------------------------------------------------------------------------------
:: This file will build Lucene.Net and create the NuGet packages.
::
:: Syntax:
::   build[.bat] [<options>]
::
:: Available Options:
::
::   -Version:<Version>
::   -v:<Version> - Assembly version number. If not supplied, the version will be the same 
::                  as PackageVersion (excluding any pre-release tag).
::
::   -PackageVersion:<PackageVersion>
::   -pv:<PackageVersion> - Nuget package version. Default is 1.0.0.
::
::   -Configuration:<Configuration>
::   -config:<Configuration> - MSBuild configuration for the build.
::
::   -Test
::   -t - Run the tests.
::
::   All options are case insensitive.
::
::   To escape any of the options, put double quotes around the entire value, like this:
::   "-config:Release"
::
:: -----------------------------------------------------------------------------------
:endcommentblock
setlocal enabledelayedexpansion enableextensions

REM Default values
IF "%version%" == "" (
	REM  If version is not supplied, our build script should parse it
	REM  from the %PackageVersion% variable. We determine this by checking
	REM  whether it is 0.0.0 (uninitialized).
	set version=0.0.0
)
IF "%PackageVersion%" == "" (
    set PackageVersion=1.0.0
)
set configuration=Release
IF NOT "%config%" == "" (
	set configuration=%config%
)
set runtests=false

FOR %%a IN (%*) DO (
	FOR /f "useback tokens=*" %%a in ('%%a') do (
		set value=%%~a

		set test=!value:~0,3!
		IF /I !test! EQU -v: (
			set version=!value:~3!
		)

		set test=!value:~0,9!
		IF /I !test! EQU -version: (
			set version=!value:~9!
		)
		
		set test=!value:~0,4!
		IF /I !test!==-pv: (
			set packageversion=!value:~4!
		)

		set test=!value:~0,16!
		IF /I !test!==-packageversion: (
			set packageversion=!value:~16!
		)

		set test=!value:~0,8!
		IF /I !test!==-config: (
			set configuration=!value:~8!
		)

		set test=!value:~0,15!
		IF /I !test!==-configuration: (
			set configuration=!value:~15!
		)
		
		set test=!value:~0,2!
		IF /I !test!==-t (
			set runtests=true
		)

		set test=!value:~0,5!
		IF /I !test!==-test: (
			set runtests=true
		)
	)
)

if "!runtests!"=="true" (
	powershell -ExecutionPolicy Bypass -Command "& { Import-Module .\build\psake.psm1; Invoke-Psake .\build\build.ps1 -task Test -properties @{\"version\"=\"%version%\";\"configuration\"=\"%configuration%"\";\"packageVersion\"=\"%PackageVersion%"\"} }"
) else (
	powershell -ExecutionPolicy Bypass -Command "& { Import-Module .\build\psake.psm1; Invoke-Psake .\build\build.ps1 -properties @{\"version\"=\"%version%\";\"configuration\"=\"%configuration%"\";\"packageVersion\"=\"%PackageVersion%"\"} }"
)

endlocal