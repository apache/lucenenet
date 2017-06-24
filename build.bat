@echo off
GOTO endcommentblock
:: -----------------------------------------------------------------------------------
::
::  Licensed to the Apache Software Foundation (ASF) under one or more
::  contributor license agreements.  See the NOTICE file distributed with
::  this work for additional information regarding copyright ownership.
::  The ASF licenses this file to You under the Apache License, Version 2.0
::  (the "License"); you may not use this file except in compliance with
::  the License.  You may obtain a copy of the License at
::
::      http://www.apache.org/licenses/LICENSE-2.0
::
::  Unless required by applicable law or agreed to in writing, software
::  distributed under the License is distributed on an "AS IS" BASIS,
::  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
::  See the License for the specific language governing permissions and
::  limitations under the License.
::
:: -----------------------------------------------------------------------------------
::
:: This file will build Lucene.Net and create the NuGet packages.
::
:: Syntax:
::   build[.bat] [<options>]
::
:: Available Options:
::
::   --Version:<Version>
::   -v:<Version> - Assembly version number. If not supplied, the version will be the same 
::                  as PackageVersion (excluding any pre-release tag).
::
::   --PackageVersion:<PackageVersion>
::   -pv:<PackageVersion> - Nuget package version. Default is 0.0.0, which instructs the script to use the value in Version.proj.
::
::   --Configuration:<Configuration>
::   -config:<Configuration> - MSBuild configuration for the build.
::
::   --Test
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
    set PackageVersion=0.0.0
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

		set test=!value:~0,10!
		IF /I !test! EQU --version: (
			set version=!value:~10!
		)
		
		set test=!value:~0,4!
		IF /I !test!==-pv: (
			set packageversion=!value:~4!
		)

		set test=!value:~0,17!
		IF /I !test!==--packageversion: (
			set packageversion=!value:~17!
		)

		set test=!value:~0,8!
		IF /I !test!==-config: (
			set configuration=!value:~8!
		)

		set test=!value:~0,16!
		IF /I !test!==--configuration: (
			set configuration=!value:~16!
		)
		
		set test=!value:~0,2!
		IF /I !test!==-t (
			set runtests=true
		)

		set test=!value:~0,6!
		IF /I !test!==--test (
			set runtests=true
		)
	)
)

set tasks="Default"
if "!runtests!"=="true" (
	set tasks="Default,Test"
)

powershell -ExecutionPolicy Bypass -Command "& { Import-Module .\build\psake.psm1; Invoke-Psake .\build\build.ps1 %tasks% -properties @{configuration='%configuration%'} -parameters @{ packageVersion='%PackageVersion%';version='%version%' } }"

endlocal
