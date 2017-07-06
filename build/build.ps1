# -----------------------------------------------------------------------------------
#
# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the ""License""); you may not use this file except in compliance with
# the License.  You may obtain a copy of the License at
# 
# http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an ""AS IS"" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
# -----------------------------------------------------------------------------------

properties {
	[string]$base_directory   = Resolve-Path "..\."
	[string]$release_directory  = "$base_directory\release"
	[string]$source_directory = "$base_directory"
	[string]$tools_directory  = "$base_directory\lib"
	[string]$nuget_package_directory = "$release_directory\NuGetPackages"
	[string]$test_results_directory = "$release_directory\TestResults"

	[string]$buildCounter     = $(if ($buildCounter) { $buildCounter } else { $env:BuildCounter }) #NOTE: Pass in as a parameter (not a property) or environment variable to override
	[string]$preReleaseCounterPattern = $(if ($preReleaseCounterPattern) { $preReleaseCounterPattern } else { if ($env:PreReleaseCounterPattern) { $env:PreReleaseCounterPattern } else { "00000" } })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
	[string]$versionSuffix    = $(if ($versionSuffix) { $versionSuffix } else { $env:VersionSuffix })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
	[string]$packageVersion   = Get-Package-Version #NOTE: Pass in as a parameter (not a property) or environment variable to override
	[string]$version          = Get-Version
	[string]$configuration    = "Release"
	[bool]$backup_files       = $true
	[bool]$prepareForBuild    = $true
	[bool]$generateBuildBat   = $false

	[string]$common_assembly_info = "$base_directory\src\CommonAssemblyInfo.cs"
	[string]$build_bat = "$base_directory\build.bat"
	[string]$copyright_year = [DateTime]::Today.Year.ToString() #Get the current year from the system
	[string]$copyright = "Copyright " + $([char]0x00A9) + " 2006 - $copyright_year The Apache Software Foundation"
	[string]$company_name = "The Apache Software Foundation"
	[string]$product_name = "Lucene.Net"
	
	#test paramters
	[string]$frameworks_to_test = "netcoreapp1.0,net451"
	[string]$where = ""
}

$backedUpFiles = New-Object System.Collections.ArrayList

task default -depends Pack

task Clean -description "This task cleans up the build directory" {
	Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
	Get-ChildItem $base_directory -Include *.bak -Recurse | foreach ($_) {Remove-Item $_.FullName}
}

task InstallSDK -description "This task makes sure the correct SDK version is installed" {
	& where.exe dotnet.exe
	$sdkVersion = ""

	if ($LASTEXITCODE -eq 0) {
		$sdkVersion = ((& dotnet.exe --version) | Out-String).Trim()
	}
	
	Write-Host "Current SDK version: $sdkVersion" -ForegroundColor Yellow
	if (!$sdkVersion.Equals("1.0.0-preview2-1-003177")) {
		Write-Host "Require SDK version 1.0.0-preview2-1-003177, installing..." -ForegroundColor Red
		#Install the correct version of the .NET SDK for this build
	    Invoke-Expression "$base_directory\build\dotnet-install.ps1 -Version 1.0.0-preview2-1-003177"
	}

	# Safety check - this should never happen
	& where.exe dotnet.exe

	if ($LASTEXITCODE -ne 0) {
		throw "Could not find dotnet CLI in PATH. Please install the .NET Core 1.1 SDK version 1.0.0-preview2-1-003177."
	}
}

task Init -depends InstallSDK -description "This task makes sure the build environment is correctly setup" {
	#Update TeamCity or MyGet with packageVersion
	Write-Output "##teamcity[buildNumber '$packageVersion']"
	Write-Output "##myget[buildNumber '$packageVersion']"

	& dotnet.exe --version
	Write-Host "Base Directory: $base_directory"
	Write-Host "Release Directory: $release_directory"
	Write-Host "Source Directory: $source_directory"
	Write-Host "Tools Directory: $tools_directory"
	Write-Host "NuGet Package Directory: $nuget_package_directory"
	Write-Host "BuildCounter: $buildCounter"
	Write-Host "PreReleaseCounterPattern: $preReleaseCounterPattern"
	Write-Host "VersionSuffix: $versionSuffix"
	Write-Host "Package Version: $packageVersion"
	Write-Host "Version: $version"
	Write-Host "Configuration: $configuration"

	Ensure-Directory-Exists "$release_directory"
}

task Restore -description "This task restores the dependencies" {
	Exec {
		& dotnet.exe restore $base_directory
	}
}

task Compile -depends Clean, Init -description "This task compiles the solution" {
	try {
		pushd $base_directory
		$projects = Get-ChildItem -Path "project.json" -Recurse
		popd

		Backup-Files $projects
		if ($prepareForBuild -eq $true) {
			Prepare-For-Build $projects
		}

		Invoke-Task Restore

		Build-Assemblies $projects

		$success = $true
	} finally {
		if ($success -ne $true) {
			Restore-Files $backedUpFiles
		}
	}
}

task Pack -depends Compile -description "This task creates the NuGet packages" {
	try {
		pushd $base_directory
		$packages = Get-ChildItem -Path "project.json" -Recurse | ? { !$_.Directory.Name.Contains(".Test") -and !$_.Directory.Name.Contains(".Demo") }
		popd

		Pack-Assemblies $packages

		$success = $true
	} finally {
		#if ($success -ne $true) {
			Restore-Files $backedUpFiles
		#}
	}
}

task Test -depends InstallSDK, Restore -description "This task runs the tests" {
	Write-Host "Running tests..." -ForegroundColor DarkCyan

	pushd $base_directory
	$testProjects = Get-ChildItem -Path "project.json" -Recurse | ? { $_.Directory.Name.Contains(".Tests") }
	popd

	Write-Host "frameworks_to_test: $frameworks_to_test" -ForegroundColor Yellow

	$frameworksToTest = $frameworks_to_test -split "\s*?,\s*?"

	foreach ($framework in $frameworksToTest) {
		Write-Host "Framework: $framework" -ForegroundColor Blue

		foreach ($testProject in $testProjects) {

			$testName = $testProject.Directory.Name
			$projectDirectory = $testProject.DirectoryName
			Write-Host "Directory: $projectDirectory" -ForegroundColor Green

			if ($framework.StartsWith("netcore")) {
				$testExpression = "dotnet.exe test '$projectDirectory\project.json' --configuration $configuration --framework $framework --no-build"
			} else {
				$binaryRoot = "$projectDirectory\bin\$configuration\$framework"

				$testBinary = "$binaryRoot\win7-x64\$testName.dll"
				if (-not (Test-Path $testBinary)) {
					$testBinary = "$binaryRoot\win7-x32\$testName.dll"
				}
				if (-not (Test-Path $testBinary)) {
					$testBinary = "$binaryRoot\$testName.dll"
				} 

				$testExpression = "$tools_directory\NUnit\NUnit.ConsoleRunner.3.5.0\tools\nunit3-console.exe $testBinary"
			}

			$testResultDirectory = "$test_results_directory\$framework\$testName"
			Ensure-Directory-Exists $testResultDirectory

			$testExpression = "$testExpression --result:$testResultDirectory\TestResult.xml --teamcity"

			if ($where -ne $null -and (-Not [System.String]::IsNullOrEmpty($where))) {
				$testExpression = "$testExpression --where $where"
			}

			Write-Host $testExpression -ForegroundColor Magenta

			Invoke-Expression $testExpression
			# fail the build on negative exit codes (NUnit errors - if positive it is a test count or, if 1, it could be a dotnet error)
			if ($LASTEXITCODE -lt 0) {
				throw "Test execution failed"
			}
		}
	}
}

function Get-Package-Version() {
	Write-Host $parameters.packageVersion -ForegroundColor Red

	#If $packageVersion is not passed in (as a parameter or environment variable), get it from Version.proj
	if (![string]::IsNullOrWhiteSpace($parameters.packageVersion) -and $parameters.packageVersion -ne "0.0.0") {
		return $parameters.packageVersion
	} elseif (![string]::IsNullOrWhiteSpace($env:PackageVersion) -and $env:PackageVersion -ne "0.0.0") {
		return $env:PackageVersion
	} else {
		#Get the version info
		$versionFile = "$base_directory\Version.proj"
		$xml = [xml](Get-Content $versionFile)

		$versionPrefix = $xml.Project.PropertyGroup.VersionPrefix

		if ([string]::IsNullOrWhiteSpace($versionSuffix)) {
			# this is a production release - use 4 segment version number 0.0.0.0
			if ([string]::IsNullOrWhiteSpace($buildCounter)) {
				$buildCounter = "0"
			}
			$packageVersion = "$versionPrefix.$buildCounter"
		} else {
			if (![string]::IsNullOrWhiteSpace($buildCounter)) {
				$buildCounter = ([Int32]$buildCounter).ToString($preReleaseCounterPattern)
			}
			# this is a pre-release - use 3 segment version number with (optional) zero-padded pre-release tag
			$packageVersion = "$versionPrefix-$versionSuffix$buildCounter"
		}

		return $packageVersion
	}
}

function Get-Version() {
	#If $version is not passed in, parse it from $packageVersion
	if ([string]::IsNullOrWhiteSpace($version) -or $version -eq "0.0.0") {
		$version = Get-Package-Version
		if ($version.Contains("-") -eq $true) {
			$version = $version.SubString(0, $version.IndexOf("-"))
		}
	}
	return $version
}

function Prepare-For-Build([string[]]$projects) {
	Backup-File $common_assembly_info 
	
	$pv = $packageVersion
	#check for presense of Git
	& where.exe git.exe
	if ($LASTEXITCODE -eq 0) {
		$gitCommit = ((git rev-parse --verify --short=10 head) | Out-String).Trim()
		$pv = "$packageVersion commit:[$gitCommit]"
	}

	Generate-Assembly-Info `
		-product $product_name `
		-company $company_name `
		-copyright $copyright `
		-version $version `
		-packageVersion $pv `
		-file $common_assembly_info

	Update-Constants-Version $packageVersion

	foreach ($project in $projects) {
		Write-Host "Updating project.json for build: $project" -ForegroundColor Cyan

		#Update version (for NuGet package) and dependency version of this project's inter-dependencies
		(Get-Content $project) | % {
			$_-replace "(?<=""Lucene.Net[\w\.]*?""\s*?:\s*?"")([^""]+)", $packageVersion
		} | Set-Content $project -Force

		$json = (Get-Content $project -Raw) | ConvertFrom-Json
		$json.version = $PackageVersion
		if (!$project.Contains("Test")) {
			if ($json.buildOptions.xmlDoc -eq $null) {
				$json.buildOptions | Add-Member -Name "xmlDoc" -Value true -MemberType NoteProperty
			} else {
				$json.buildOptions | % {$_.xmlDoc = true}
			}
		}
		$json | ConvertTo-Json -depth 100 | Out-File $project -encoding UTF8 -Force
	}

	if ($generateBuildBat -eq $true) {
		Backup-File $build_bat
		Generate-Build-Bat $build_bat
	}
}

function Update-Constants-Version([string]$version) {
	$constantsFile = "$base_directory\src\Lucene.Net\Util\Constants.cs"

	Backup-File $constantsFile
	(Get-Content $constantsFile) | % {
		$_-replace "(?<=LUCENE_VERSION\s*?=\s*?"")([^""]*)", $version
	} | Set-Content $constantsFile -Force
}

function Generate-Assembly-Info {
param(
	[string]$product,
	[string]$company,
	[string]$copyright,
	[string]$version,
	[string]$packageVersion,
	[string]$file = $(throw "file is a required parameter.")
)
	#Use only the major version as the assembly version.
	#This ensures binary compatibility unless the major version changes.
	$version-match "(^\d+)"
	$assemblyVersion = $Matches[0]
	$assemblyVersion = "$assemblyVersion.0.0"

  $asmInfo = "/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the ""License""); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an ""AS IS"" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Reflection;

[assembly: AssemblyProduct(""$product"")]
[assembly: AssemblyCompany(""$company"")]
[assembly: AssemblyTrademark(""$copyright"")]
[assembly: AssemblyCopyright(""$copyright"")]
[assembly: AssemblyVersion(""$assemblyVersion"")] 
[assembly: AssemblyFileVersion(""$version"")]
[assembly: AssemblyInformationalVersion(""$packageVersion"")]
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	Ensure-Directory-Exists $dir

	Write-Host "Generating assembly info file: $file"
	Out-File -filePath $file -encoding UTF8 -inputObject $asmInfo
}

function Generate-Build-Bat {
param(
	[string]$file = $(throw "file is a required parameter.")
)
  $buildBat = "
@echo off
GOTO endcommentblock
:: -----------------------------------------------------------------------------------
::
:: Licensed to the Apache Software Foundation (ASF) under one or more
:: contributor license agreements.  See the NOTICE file distributed with
:: this work for additional information regarding copyright ownership.
:: The ASF licenses this file to You under the Apache License, Version 2.0
:: (the ""License""); you may not use this file except in compliance with
:: the License.  You may obtain a copy of the License at
:: 
:: http://www.apache.org/licenses/LICENSE-2.0
:: 
:: Unless required by applicable law or agreed to in writing, software
:: distributed under the License is distributed on an ""AS IS"" BASIS,
:: WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
:: See the License for the specific language governing permissions and
:: limitations under the License.
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
::   --Test
::   -t - Run the tests.
::
:: -----------------------------------------------------------------------------------
:endcommentblock
setlocal enabledelayedexpansion enableextensions

set runtests=false

FOR %%a IN (%*) DO (
	FOR /f ""useback tokens=*"" %%a in ('%%a') do (
		set value=%%~a
		
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

set tasks=""Default""
if ""!runtests!""==""true"" (
	set tasks=""Default,Test""
)

powershell -ExecutionPolicy Bypass -Command ""& { Import-Module .\build\psake.psm1; Invoke-Psake .\build\build.ps1 -Task %tasks% -properties @{prepareForBuild='false';backup_files='false'} }""

endlocal
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	Ensure-Directory-Exists $dir

	Write-Host "Generating build.bat file: $file"
	#Out-File -filePath $file -encoding UTF8 -inputObject $buildBat -Force
	$Utf8EncodingNoBom = New-Object System.Text.UTF8Encoding $false
	[System.IO.File]::WriteAllLines($file, $buildBat, $Utf8EncodingNoBom)
}

function Build-Assemblies([string[]]$projects) {
	foreach ($project in $projects) {
		Exec {
			& dotnet.exe build $project --configuration $configuration
		}
	}
}

function Pack-Assemblies([string[]]$projects) {
	Ensure-Directory-Exists $nuget_package_directory
	foreach ($project in $projects) {
		Write-Host "Creating NuGet package for $project..." -ForegroundColor Magenta
		Exec {
			& dotnet.exe pack $project --configuration $Configuration --output $nuget_package_directory --no-build
		}
	}
}

function Backup-Files([string[]]$paths) {
	foreach ($path in $paths) {
		Backup-File $path
	}
}

function Backup-File([string]$path) {
	if ($backup_files -eq $true) {
		Copy-Item $path "$path.bak" -Force
		$backedUpFiles.Insert(0, $path)
	} else {
		Write-Host "Ignoring backup of file $path" -ForegroundColor DarkRed
	}
}

function Restore-Files([string[]]$paths) {
	foreach ($path in $paths) {
		Restore-File $path
	}
}

function Restore-File([string]$path) {
	if ($backup_files -eq $true) {
		if (Test-Path "$path.bak") {
			Move-Item "$path.bak" $path -Force
		}
		$backedUpFiles.Remove($path)
	}
}

function Ensure-Directory-Exists([string] $path)
{
	if (!(Test-Path $path)) {
		New-Item $path -ItemType Directory
	}
}