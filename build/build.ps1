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
	[string]$publish_directory = "$release_directory\Publish"
	[string]$solutionFile = "$base_directory\Lucene.Net.sln"
	[string]$versionFile = "$base_directory\Version.proj"
	[string]$sdkPath = "$env:programfiles/dotnet/sdk"

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
	[string]$frameworks_to_test = "netcoreapp2.0,netcoreapp1.0,net451"
	[string]$where = ""
}

$backedUpFiles = New-Object System.Collections.ArrayList

task default -depends Publish

task Clean -description "This task cleans up the build directory" {
	Write-Host "##teamcity[progressMessage 'Cleaning']"
	Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
	Get-ChildItem $base_directory -Include *.bak -Recurse | foreach ($_) {Remove-Item $_.FullName}
}

task InstallSDK -description "This task makes sure the correct SDK version is installed to build" {
	Write-Host "##teamcity[progressMessage 'Installing SDK']"
	& where.exe dotnet.exe
	$sdkVersion = "0.0.0.0"

	if ($LASTEXITCODE -eq 0) {
		$sdkVersion = [string]((& dotnet.exe --version) | Out-String).Trim()
	}

	Write-Host "Current SDK version: $sdkVersion" -ForegroundColor Yellow

	# Make sure framework for .NET Core 2.0.0 is available
	if (($sdkVersion.Contains("-")) -or ([version]$sdkVersion -lt ([version]"2.0.0")) -or ([version]$sdkVersion -ge ([version]"3.0.0"))) {
		Write-Host "Requires SDK version 2.0.0 or greater, installing..." -ForegroundColor Red
		Invoke-Expression "$base_directory\build\dotnet-install.ps1 -Version 2.0.0"
	}

	# Safety check - this should never happen
	& where.exe dotnet.exe

	if ($LASTEXITCODE -ne 0) {
		throw "Could not find dotnet CLI in PATH. Please install the .NET Core 2.0 SDK."
	}
}

task InstallSDK2IfRequired -description "This task installs the .NET Core 2.x SDK (required for testing under .NET Core 2.0 or .NET Framework)" {
	Write-Host "##teamcity[progressMessage 'Installing SDK']"
	# netcoreapp1.0 requires the .NET Core SDK 2.0 or there is an 'illegal characters in path' error
	#if ($frameworks_to_test.Contains("netcoreapp2.") -or $frameworks_to_test.Contains("net45")) {
		Invoke-Task InstallSDK
	#}
}

task InstallSDK1IfRequired -description "This task installs the .NET Core 1.x SDK (required for testing under .NET Core 1.0)" {
	Write-Host "##teamcity[progressMessage 'Installing SDK']"
	if ($frameworks_to_test.Contains("netcoreapp1.")) {
		# Make sure framework for .NET Core 1.0.4 is available
		if (((Test-Path "$sdkPath/1.0.4") -eq $false) -and ((Test-Path "$sdkPath/1.1.0") -eq $false)) {
			Write-Host "Requires SDK version 1.0.4, installing..." -ForegroundColor Red
			Invoke-Expression "$base_directory\build\dotnet-install.ps1 -Version 1.0.4"
		}

		# Safety check - this should never happen
		& where.exe dotnet.exe

		if ($LASTEXITCODE -ne 0) {
			throw "Could not find dotnet CLI in PATH. Please install the .NET Core 1.0.4 SDK."
		}
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
	Write-Host "##teamcity[progressMessage 'Restoring']"
	Exec { 
		& dotnet.exe restore $solutionFile --no-dependencies /p:TestFrameworks=true
	}
}

task Compile -depends Clean, Init, Restore -description "This task compiles the solution" {
	Write-Host "##teamcity[progressMessage 'Compiling']"
	try {
		if ($prepareForBuild -eq $true) {
			Prepare-For-Build
		}

		#Use only the major version as the assembly version.
		#This ensures binary compatibility unless the major version changes.
		$version-match "(^\d+)"
		$assemblyVersion = $Matches[0]
		$assemblyVersion = "$assemblyVersion.0.0"

		Write-Host "Assembly version set to: $assemblyVersion" -ForegroundColor Green

		$pv = $packageVersion
		#check for presense of Git
		& where.exe git.exe
		if ($LASTEXITCODE -eq 0) {
			$gitCommit = ((git rev-parse --verify --short=10 head) | Out-String).Trim()
			$pv = "$packageVersion commit:[$gitCommit]"
		}

		Write-Host "Assembly informational version set to: $pv" -ForegroundColor Green

		$testFrameworks = $frameworks_to_test.Replace(',', ';')

		Write-Host "TestFrameworks set to: $testFrameworks" -ForegroundColor Green

		Exec {
			& dotnet.exe msbuild $solutionFile /t:Build `
				/p:Configuration=$configuration `
				/p:AssemblyVersion=$assemblyVersion `
				/p:InformationalVersion=$pv `
				/p:Product=$product_name `
				/p:Company=$company_name `
				/p:Copyright=$copyright `
				/p:TestFrameworks=true # workaround for parsing issue: https://github.com/Microsoft/msbuild/issues/471#issuecomment-181963350
		}

		$success = $true
	} finally {
		if ($success -ne $true) {
			Restore-Files $backedUpFiles
		}
	}
}

task Pack -depends Compile -description "This task creates the NuGet packages" {
	Write-Host "##teamcity[progressMessage 'Packing']"
	#create the nuget package output directory
	Ensure-Directory-Exists "$nuget_package_directory"

	try {
		pushd $base_directory
		$packages = Get-ChildItem -Path "$source_directory\**\*.csproj" -Recurse | ? { 
			!$_.Directory.Name.Contains(".Test") -and 
			!$_.Directory.Name.Contains(".Demo") -and 
			!$_.Directory.FullName.Contains("\tools\") -and 
			!$_.Directory.FullName.Contains("/tools/") -and 
			!$_.Directory.Name.Contains(".Replicator.AspNetCore")
		}
		popd

		Pack-Assemblies $packages

		$success = $true
	} finally {
		#if ($success -ne $true) {
			Restore-Files $backedUpFiles
		#}
	}
}

task Publish -depends Pack -description "This task publishes the command line tools" {
	Write-Host "##teamcity[progressMessage 'Publishing']"
	#create the publish output directory
	Ensure-Directory-Exists "$publish_directory"

	pushd $base_directory
	$tools = Get-ChildItem -Path "$source_directory\**\*.csproj" -Recurse | ? {
		$_.Directory.FullName.Contains("\tools\") -or $_.Directory.FullName.Contains("/tools/") -and 
		!$_.Directory.Name.Contains(".Test") -and
		!$_.Directory.Name.Contains("JavaDocToMarkdownConverter")
	}
	popd

	foreach ($tool in $tools) {
		Write-Host "Publishing $tool..." -ForegroundColor Magenta

		$toolName = [io.path]::GetFileNameWithoutExtension($tool)
		Exec {
			& dotnet.exe publish $tool --configuration $Configuration --output "$publish_directory\$toolName"
		}
	}
}

task Test -depends InstallSDK1IfRequired, InstallSDK2IfRequired, Restore -description "This task runs the tests" {
	Write-Host "##teamcity[progressMessage 'Testing']"
	Write-Host "Running tests..." -ForegroundColor DarkCyan

	pushd $base_directory
	$testProjects = Get-ChildItem -Path "$source_directory\**\*.csproj" -Recurse | ? { $_.Directory.Name.Contains(".Tests") }
	popd

	Write-Host "frameworks_to_test: $frameworks_to_test" -ForegroundColor Yellow

	$frameworksToTest = $frameworks_to_test -split "\s*?,\s*?"

	foreach ($framework in $frameworksToTest) {
		Write-Host "Framework: $framework" -ForegroundColor Blue

		foreach ($testProject in $testProjects) {
			$testName = $testProject.Directory.Name

			# Special case - our CLI tool only supports .NET Core 2.0
			if ($testName.Contains("Tests.Cli") -and ($framework -ne "netcoreapp2.0")) {
				continue
			}

			$testResultDirectory = "$test_results_directory\$framework\$testName"
			Ensure-Directory-Exists $testResultDirectory

			#if ($framework.StartsWith("netcore")) {
				$testExpression = "dotnet.exe test $testProject --configuration $configuration --framework $framework --no-build"
				#if ($framework -ne "netcoreapp1.0") {
					$testExpression = "$testExpression --no-restore"
					$testExpression = "$testExpression --results-directory $testResultDirectory\TestResult.xml"
				#}
				
				if ($where -ne $null -and (-Not [System.String]::IsNullOrEmpty($where))) {
					$testExpression = "$testExpression --filter $where"
				}
			#} else {
			#	$projectDirectory = $testProject.DirectoryName
			#	Write-Host "Directory: $projectDirectory" -ForegroundColor Green

			#	$binaryRoot = "$projectDirectory\bin\$configuration\$framework"

			#	$testBinary = "$binaryRoot\win7-x64\$testName.dll"
			#	if (-not (Test-Path $testBinary)) {
			#		$testBinary = "$binaryRoot\win7-x32\$testName.dll"
			#	}
			#	if (-not (Test-Path $testBinary)) {
			#		$testBinary = "$binaryRoot\$testName.dll"
			#	} 

			#	$testExpression = "$tools_directory\NUnit\NUnit.ConsoleRunner.3.5.0\tools\nunit3-console.exe $testBinary --teamcity"
			#	$testExpression = "$testExpression --result:$testResultDirectory\TestResult.xml"

			#	if ($where -ne $null -and (-Not [System.String]::IsNullOrEmpty($where))) {
			#		$testExpression = "$testExpression --where=$where"
			#	}
			#}

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

function Prepare-For-Build() {
	Backup-File $common_assembly_info 

	Generate-Assembly-Info `
		-version $version `
		-file $common_assembly_info

	Update-Constants-Version $packageVersion

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
	[string]$version,
	[string]$file = $(throw "file is a required parameter.")
)

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

[assembly: AssemblyFileVersion(""$version"")]
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

function Pack-Assemblies([string[]]$projects) {
	Ensure-Directory-Exists $nuget_package_directory
	foreach ($project in $projects) {
		Write-Host "Creating NuGet package for $project..." -ForegroundColor Magenta
		Exec {
			& dotnet.exe pack $project --configuration $Configuration --output $nuget_package_directory --no-build --include-symbols /p:PackageVersion=$packageVersion
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

function Ensure-Directory-Exists([string] $path) {
	if (!(Test-Path $path)) {
		New-Item $path -ItemType Directory
	}
}