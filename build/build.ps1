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
    [string]$base_directory   = Resolve-Path "../."
    [string]$release_directory  = "$base_directory/release"
    [string]$source_directory = "$base_directory"
    [string]$tools_directory  = "$base_directory/lib"
    [string]$nuget_package_directory = "$release_directory/NuGetPackages"
    [string]$test_results_directory = "$release_directory/TestResults"
    [string]$publish_directory = "$release_directory/Publish"
    [string]$solutionFile = "$base_directory/Lucene.Net.sln"
    [string]$sdkPath = "$env:programfiles/dotnet/sdk"
    [string]$sdkVersion = "6.0.100-rc.1.21463.6"
    [bool]$skipSdkInstallation = $false
    [string]$globalJsonFile = "$base_directory/global.json"
    [string]$versionPropsFile = "$base_directory/Version.props"
    [string]$build_bat = "$base_directory/build.bat"

    [string]$buildCounter     = $(if ($buildCounter) { $buildCounter } else { $env:BuildCounter }) #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$preReleaseCounterPattern = $(if ($preReleaseCounterPattern) { $preReleaseCounterPattern } else { if ($env:PreReleaseCounterPattern) { $env:PreReleaseCounterPattern } else { "0000000000" } })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$versionSuffix    = $(if ($versionSuffix -ne $null) { $versionSuffix } else { if ($env:VersionSuffix -ne $null) { $env:VersionSuffix } else { 'ci' }}) #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$packageVersion   = Get-Package-Version #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$version          = Get-Version
    [string]$configuration    = $(if ($configuration) { $configuration } else { if ($env:BuildConfiguration) { $env:BuildConfiguration } else { "Release" } })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$platform   = $(if ($platform) { $platform } else { if ($env:BuildPlatform) { $env:BuildPlatform } else { "Any CPU" } })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [bool]$backup_files       = $true
    [bool]$prepareForBuild    = $true
    [bool]$generateBuildBat   = $false
    [bool]$zipPublishedArtifacts = $false
    [string]$publishedArtifactZipFileName = "artifact.zip"

    [int]$maximumParalellJobs = 8
    
    #test paramters
    [string]$frameworks_to_test = "net6.0,net5.0,netcoreapp3.1,net48,net461"
    [string]$where = ""
}

$backedUpFiles = New-Object System.Collections.ArrayList
if ($IsWindows -eq $null) {
    $IsWindows = $env:OS.StartsWith('Windows')
}

task default -depends Pack

task Clean -description "This task cleans up the build directory" {
    Write-Host "##teamcity[progressMessage 'Cleaning']"
    Write-Host "##vso[task.setprogress]'Cleaning'"
    Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
    Get-ChildItem $base_directory -Include *.bak -Recurse | foreach ($_) {Remove-Item $_.FullName}
}

task UpdateLocalSDKVersion -description "Backs up the project.json file and pins the version to $sdkVersion" {
    Backup-File $globalJsonFile
    Generate-Global-Json `
        -sdkVersion $sdkVersion `
        -file $globalJsonFile
}

task InstallSDK -description "This task makes sure the correct SDK version is installed to build" -ContinueOnError {
    if (!$skipSdkInstallation) {
        Write-Host "##teamcity[progressMessage 'Installing SDK $sdkVersion']"
        Write-Host "##vso[task.setprogress]'Installing SDK $sdkVersion'"
        $installed = Is-Sdk-Version-Installed $sdkVersion
        if (!$installed) {
            Write-Host "Requires SDK version $sdkVersion, installing..." -ForegroundColor Red
            Invoke-Expression "$base_directory\build\dotnet-install.ps1 -Version $sdkVersion"
        }

        # Safety check - this should never happen
        & where.exe dotnet.exe

        if ($LASTEXITCODE -ne 0) {
            throw "Could not find dotnet CLI in PATH. Please install the .NET SDK, version $sdkVersion."
        }
    }
}

task Init -depends InstallSDK, UpdateLocalSDKVersion -description "This task makes sure the build environment is correctly setup" {
    #Update TeamCity, MyGet, or Azure Pipelines with packageVersion
    Write-Output "##teamcity[buildNumber '$packageVersion']"
    Write-Output "##myget[buildNumber '$packageVersion']"
    Write-Host "##vso[build.updatebuildnumber]$packageVersion"

    & dotnet.exe --version
    & dotnet.exe --info
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
    Write-Host "Platform: $platform"
    Write-Host "MaximumParallelJobs: $($maximumParalellJobs.ToString())"
    Write-Host "Powershell Version: $($PSVersionTable.PSVersion)"

    Ensure-Directory-Exists "$release_directory"
}

task Restore -description "This task restores the dependencies" {
    Write-Host "##teamcity[progressMessage 'Restoring']"
    Write-Host "##vso[task.setprogress]'Restoring'"
    Exec { 
        & dotnet.exe restore $solutionFile --no-dependencies /p:TestFrameworks=true
    }
}

task Compile -depends Clean, Init, Restore -description "This task compiles the solution" {
    Write-Host "##teamcity[progressMessage 'Compiling']"
    Write-Host "##vso[task.setprogress]'Compiling'"
    try {
        if ($prepareForBuild -eq $true) {
            Prepare-For-Build
        }

        $testFrameworks = [string]::Join(';', (Get-FrameworksToTest))

        Write-Host "TestFrameworks set to: $testFrameworks" -ForegroundColor Green

        Exec {
            # NOTE: Version information is not passed in at the command line,
            # instead it is output to the Version.props file. This file is then
            # used during a release to "freeze" the build at a specific version
            # so it is always a constant in release distributions.
            & dotnet build "$solutionFile" `
                --configuration "$configuration" `
                --no-restore `
                -p:Platform=$platform `
                -p:PortableDebugTypeOnly=true `
                -p:TestFrameworks=true # workaround for parsing issue: https://github.com/Microsoft/msbuild/issues/471#issuecomment-181963350
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
    Write-Host "##vso[task.setprogress]'Packing'"
    #create the nuget package output directory
    Ensure-Directory-Exists "$nuget_package_directory"

    try {
        Exec {
            # NOTE: Package version information is not passed in at the command line,
            # instead it is output to the Version.props file. This file is then
            # used during a release to "freeze" the build at a specific version
            # so it is always a constant in release distributions.
            & dotnet.exe pack $solutionFile --configuration $configuration --output $nuget_package_directory --no-build
        }

        $success = $true
    } finally {
        #if ($success -ne $true) {
            Restore-Files $backedUpFiles
            #Remove Version.props, as we don't want it to be committed to the repository
            if ($backup_files -eq $true -and (Test-Path -Path "$versionPropsFile") -eq $true) {
                Remove-Item -Path "$versionPropsFile" -Force
            }
        #}
    }
}

# Loops through each framework in the TestTargetFrameworks variable and
# publishes the solution in the artifact staging directory with the framework
# as part of the folder structure.
task Publish -depends Compile -description "This task uses dotnet publish to package the binaries with all of their dependencies so they can be run xplat" {
    Write-Host "##teamcity[progressMessage 'Publishing']"
    Write-Host "##vso[task.setprogress]'Publishing'"

    try {
        $frameworksToTest = Get-FrameworksToTest

        if ($zipPublishedArtifacts) {
            $outDirectory = New-TemporaryDirectory
        } else {
            $outDirectory = $publish_directory
        }
        
        foreach ($framework in $frameworksToTest) {

            # Pause if we have queued too many parallel jobs
            $running = @(Get-Job | Where-Object { $_.State -eq 'Running' })
            if ($running.Count -ge $maximumParalellJobs) {
                $running | Wait-Job -Any | Out-Null
            }

            $logPath = "$outDirectory/$framework"
            $outputPath = "$logPath"

            # Do this first so there is no conflict
            Ensure-Directory-Exists $outputPath
            
            Write-Host "Configuration: $configuration"

            $expression = "dotnet publish `"$solutionFile`" --configuration `"$configuration`" --framework `"$framework`" --output `"$outputPath`""
            $expression = "$expression --no-build --no-restore --verbosity Normal /p:TestFrameworks=true /p:Platform=`"$platform`""
            $expression = "$expression > `"$logPath/dotnet-publish.log`" 2> `"$logPath/dotnet-publish-error.log`""

            $scriptBlock = {
                param([string]$expression)
                Write-Host $expression
                # Note: Cannot use Psake Exec in background
                Invoke-Expression $expression
            }

            # Execute the jobs in parallel
            Start-Job -Name "$framework" -ScriptBlock $scriptBlock -ArgumentList @($expression)
        }

        # Wait for it all to complete
        do {
            $running = @(Get-Job | Where-Object { $_.State -eq 'Running' })
            if ($running.Count -gt 0) {
                Write-Host ""
                Write-Host "  Almost finished, only $($running.Count) projects left to publish..." -ForegroundColor Cyan
                $running | Wait-Job -Any | Out-Null
            }
        } until ($running.Count -eq 0)

        # Getting the information back from the jobs (time consuming)
        #Get-Job | Receive-Job

        if ($zipPublishedArtifacts) {
            Ensure-Directory-Exists $publish_directory
            Add-Type -assembly "System.IO.Compression.Filesystem"
            [System.IO.Compression.ZipFile]::CreateFromDirectory($outDirectory, "$publish_directory/$publishedArtifactZipFileName")
        }

        $success = $true
    } finally {
        #if ($success -ne $true) {
            Restore-Files $backedUpFiles
        #}
    }
}

task Test -depends InstallSDK, UpdateLocalSDKVersion, Restore -description "This task runs the tests" {
    Write-Host "##teamcity[progressMessage 'Testing']"
    Write-Host "##vso[task.setprogress]'Testing'"
    Write-Host "Running tests..." -ForegroundColor DarkCyan

    pushd $base_directory
    $testProjects = Get-ChildItem -Path "$source_directory/**/*.csproj" -Recurse | ? { $_.Directory.Name.Contains(".Tests") }
    popd

    $testProjects = $testProjects | Sort-Object -Property FullName

    $frameworksToTest = Get-FrameworksToTest

    Write-Host "frameworksToTest: $frameworksToTest" -ForegroundColor Yellow

    [int]$totalProjects = $testProjects.Length * $frameworksToTest.Length
    [int]$remainingProjects = $totalProjects

    Ensure-Directory-Exists $test_results_directory

    foreach ($testProject in $testProjects) {

        foreach ($framework in $frameworksToTest) {
            $testName = $testProject.Directory.Name

            # Special case - our CLI tool only supports .NET Core 3.1
            if ($testName.Contains("Tests.Cli") -and (!$framework.StartsWith("netcoreapp3.1"))) {
                $totalProjects--
                $remainingProjects--
                continue
            }
            
            # Special case - OpenNLP.NET only supports .NET Framework
            if ($testName.Contains("Tests.Analysis.OpenNLP") -and (!$framework.StartsWith("net4"))) {
                $totalProjects--
                $remainingProjects--
                continue
            }

            Write-Host "  Next Project in Queue: $testName, Framework: $framework" -ForegroundColor Yellow

            # Pause if we have queued too many parallel jobs
            $running = @(Get-Job | Where-Object { $_.State -eq 'Running' })
            if ($running.Count -ge $maximumParalellJobs) {
                Write-Host ""
                Write-Host "  Running tests in parallel on $($running.Count) projects out of approximately $totalProjects total." -ForegroundColor Cyan
                Write-Host "  $remainingProjects projects are waiting in the queue to run. This will take a bit, please wait..." -ForegroundColor Cyan
                $running | Wait-Job -Any | Out-Null
            }
            $remainingProjects -= 1

            $testResultDirectory = "$test_results_directory/$framework/$testName"
            Ensure-Directory-Exists $testResultDirectory

            $testProjectPath = $testProject.FullName
            $testExpression = "dotnet.exe test $testProjectPath --configuration $configuration --framework $framework --no-build"
            $testExpression = "$testExpression --no-restore --blame  --blame-hang --blame-hang-dump-type mini --blame-hang-timeout 15minutes --results-directory $testResultDirectory"

            # Breaking change: We need to explicitly set the logger for it to work with TeamCity.
            # See: https://github.com/microsoft/vstest/issues/1590#issuecomment-393460921

            # Log to the console normal verbosity. With the TeamCity.VSTest.TestAdapter
            # referenced by the test DLL, this will output teamcity service messages.
            # Also, it displays pretty user output on the console.
            $testExpression = "$testExpression --logger:""console;verbosity=normal"""

            # Also log to a file in TRX format, so we have a build artifact both when
            # doing release inspection and on the CI server.
            $testExpression = "$testExpression --logger:""trx;LogFileName=TestResults.trx"""
            
            if (![string]::IsNullOrEmpty($where)) {
                $testExpression = "$testExpression --TestCaseFilter:""$where"""
            }

            Write-Host $testExpression -ForegroundColor Magenta

            $scriptBlock = {
                param([string]$testExpression, [string]$testResultDirectory)
                $testExpression = "$testExpression > '$testResultDirectory/dotnet-test.log' 2> '$testResultDirectory/dotnet-test-error.log'"
                Invoke-Expression $testExpression
            }

            # Execute the jobs in parallel
            Start-Job -Name "$testName,$framework" -ScriptBlock $scriptBlock -ArgumentList $testExpression,$testResultDirectory

            #Invoke-Expression $testExpression
            ## fail the build on negative exit codes (NUnit errors - if positive it is a test count or, if 1, it could be a dotnet error)
            #if ($LASTEXITCODE -lt 0) {
            #	throw "Test execution failed"
            #}
        }
    }

    do {
        $running = @(Get-Job | Where-Object { $_.State -eq 'Running' })
        if ($running.Count -gt 0) {
            Write-Host ""
            Write-Host "  Almost finished, only $($running.Count) test projects left..." -ForegroundColor Cyan
            [int]$number = 0
            foreach ($runningJob in $running) {
                $number++
                $jobName = $runningJob | Select-Object -ExpandProperty Name
                Write-Host "$number. $jobName"
            }
            $running | Wait-Job -Any
        }
    } until ($running.Count -eq 0)

    Summarize-Test-Results -FrameworksToTest $frameworksToTest
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
        $versionFile = "$base_directory/Directory.Build.props"
        $xml = [xml](Get-Content $versionFile)

        $versionPrefix = ([string]$xml.Project.PropertyGroup.VersionPrefix).Trim()

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

function Get-FrameworksToTest() {
    $frameworksToTest = New-Object Collections.Generic.List[string]
    $frameworks = $frameworks_to_test -split "\s*?,\s*?"
    foreach ($framework in $frameworks) {
        if ($IsWindows) {
            $frameworksToTest.Add($framework)
        } elseif ($framework.StartsWith('netcore')) {
            $frameworksToTest.Add($framework)
        }
    }
    return [System.Linq.Enumerable]::ToArray($frameworksToTest)
}

function Is-Sdk-Version-Installed([string]$sdkVersion) {
    & where.exe dotnet.exe | Out-Null
    if ($LASTEXITCODE -eq 0) {
        pushd $PSScriptRoot
        $version = ((& dotnet --version 2>&1) | Out-String).Trim()
        popd

        # May happen if global.json contains a version that
        # isn't installed, but we have at least one
        if ($version.Contains('not found')) {
            return $false
        } elseif ([version]$version -eq [version]$sdkVersion) {
            return $true
        } elseif ([version]$version -gt [version]"2.1.0") {
            $availableSdks = ((& dotnet --list-sdks) | Out-String)
            if ($LASTEXITCODE -eq 0) {
                if ($availableSdks.Contains($sdkVersion)) {
                    return $true
                } else {
                    return $false
                }
            } else {
                return (Test-Path "$sdkPath/$sdkVersion")
            }
        }
    }
    return $false
}

function Prepare-For-Build() {
    #Use only the major version as the assembly version.
    #This ensures binary compatibility unless the major version changes.
    $version -match "(^\d+)"
    $assemblyVersion = $Matches[0]
    $assemblyVersion = "$assemblyVersion.0.0"

    Write-Host "Assembly version set to: $assemblyVersion" -ForegroundColor Green

    $informationalVersion = $packageVersion
    #check for presense of Git
    & where.exe git.exe
    if ($LASTEXITCODE -eq 0) {
        $gitCommit = ((git rev-parse --verify --short=10 head) | Out-String).Trim()
        $informationalVersion = "$packageVersion commit:[$gitCommit]"
    }

    Write-Host "##vso[task.setvariable variable=AssemblyVersion;]$assemblyVersion"
    Write-Host "##vso[task.setvariable variable=FileVersion;]$version"
    Write-Host "##vso[task.setvariable variable=InformationalVersion;]$informationalVersion"
    Write-Host "##vso[task.setvariable variable=PackageVersion;]$packageVersion"

    Write-Host "Assembly informational version set to: $informationalVersion" -ForegroundColor Green

    Generate-Version-Props `
        -AssemblyVersion $assemblyVersion `
        -FileVersion $version `
        -InformationalVersion $informationalVersion `
        -PackageVersion $packageVersion `
        -File $versionPropsFile
    Update-Constants-Version $packageVersion

    if ($generateBuildBat -eq $true) {
        Backup-File $build_bat
        Generate-Build-Bat $build_bat
    }
}

function Update-Constants-Version([string]$version) {
    $constantsFile = "$base_directory/src/Lucene.Net/Util/Constants.cs"

    Backup-File $constantsFile
    (Get-Content $constantsFile) | % {
        $_-replace "(?<=LUCENE_VERSION\s*?=\s*?"")([^""]*)", $version
    } | Set-Content $constantsFile -Force
}

function Generate-Global-Json {
param(
    [string]$sdkVersion,
    [string]$file = $(throw "file is a required parameter.")
)

$fileText = "{
  ""sources"": [ ""src"" ],
  ""sdk"": {
    ""version"": ""$sdkVersion""
  }
}"
    $dir = [System.IO.Path]::GetDirectoryName($file)
    Ensure-Directory-Exists $dir

    Write-Host "Generating global.json file: $file"
    Out-File -filePath $file -encoding UTF8 -inputObject $fileText
}

function Generate-Version-Props {
param(
    [string]$assemblyVersion,
    [string]$fileVersion,
    [string]$informationalVersion,
    [string]$packageVersion,
    [string]$file = $(throw "file is a required parameter.")
)

$fileText = "<!--
 Licensed to the Apache Software Foundation (ASF) under one
 or more contributor license agreements.  See the NOTICE file
 distributed with this work for additional information
 regarding copyright ownership.  The ASF licenses this file
 to you under the Apache License, Version 2.0 (the
 ""License""); you may not use this file except in compliance
 with the License.  You may obtain a copy of the License at
   http://www.apache.org/licenses/LICENSE-2.0
 Unless required by applicable law or agreed to in writing,
 software distributed under the License is distributed on an
 ""AS IS"" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 KIND, either express or implied.  See the License for the
 specific language governing permissions and limitations
 under the License.
-->
<Project>
  <PropertyGroup Label=""Version Override Properties"">
    <AssemblyVersion>$assemblyVersion</AssemblyVersion>
    <FileVersion>$fileVersion</FileVersion>
    <InformationalVersion>$informationalVersion</InformationalVersion>
    <PackageVersion>$packageVersion</PackageVersion>
  </PropertyGroup>
</Project>"
    $dir = [System.IO.Path]::GetDirectoryName($file)
    Ensure-Directory-Exists $dir

    Write-Host "Generating Version.props file: $file"
    Out-File -filePath $file -encoding UTF8 -inputObject $fileText
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
::   --MaximumParallelJobs
::   -mp - Set the maxumum number of parallel jobs to run during testing. If not supplied, the default is 8.
::
:: -----------------------------------------------------------------------------------
:endcommentblock
setlocal enabledelayedexpansion enableextensions
set runtests=false
set maximumParallelJobs=8
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
        set test=!value:~0,4!
        IF /I !test!==-mp: (
            set maximumParallelJobs=!value:~4!
        )
        set test=!value:~0,22!
        IF /I !test!==--maximumparalleljobs: (
            set maximumParallelJobs=!value:~22!
        )
    )
)
set tasks=""Default""
if ""!runtests!""==""true"" (
    set tasks=""Default,Test""
)
powershell -ExecutionPolicy Bypass -Command ""& { Import-Module .\build\psake.psm1; Invoke-Psake .\build\build.ps1 -Task %tasks% -properties @{prepareForBuild='false';backup_files='false';maximumParalellJobs=%maximumParallelJobs%} }""
endlocal
"
    $dir = [System.IO.Path]::GetDirectoryName($file)
    Ensure-Directory-Exists $dir

    Write-Host "Generating build.bat file: $file"
    #Out-File -filePath $file -encoding UTF8 -inputObject $buildBat -Force
    $Utf8EncodingNoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($file, $buildBat, $Utf8EncodingNoBom)
}

function New-CountersObject ([string]$project, [string]$outcome, [int]$total, [int]$executed, [int]$passed, [int]$failed, [int]$warning, [int]$inconclusive) {
    $counters = New-Object -TypeName PSObject
    $fields = [ordered]@{Project=$project;Outcome=$outcome;Total=$total;Executed=$executed;Passed=$passed;Failed=$failed;Warning=$warning;Inconclusive=$inconclusive}
    $counters | Add-Member -NotePropertyMembers $fields -TypeName Counters
    return $counters
}

function Summarize-Test-Results([string[]]$frameworksToTest) {

    foreach ($framework in $frameworksToTest) {
        pushd $base_directory
        $testReports = Get-ChildItem -Path "$test_results_directory/$framework" -Recurse -File -Filter "*.trx" | ForEach-Object {
            $_.FullName
        }
        popd
        
        [int]$totalCountForFramework = 0
        [int]$executedCountForFramework = 0
        [int]$passedCountForFramework = 0
        [int]$failedCountForFramework = 0
        [int]$warningCountForFramework = 0
        [int]$inconclusiveCountForFramework = 0
        [string]$outcomeForFramework = 'Completed'

        # HEADER FOR FRAMEWORK

        Write-Host ""
        Write-Host ""
        Write-Host "**********************************************************************" -ForegroundColor Yellow
        Write-Host "*                                                                    *" -ForegroundColor Yellow
        Write-Host "*                        Test Summary For $framework"  -ForegroundColor Yellow
        Write-Host "*                                                                    *" -ForegroundColor Yellow
        Write-Host "**********************************************************************" -ForegroundColor Yellow

        foreach ($testReport in $testReports) {
            $testName = [System.IO.Path]::GetFileName([System.IO.Path]::GetDirectoryName($testReport))
            $reader = [System.Xml.XmlReader]::Create($testReport)
            try {
                while ($reader.Read()) {
                    
                    if ($reader.NodeType -eq [System.Xml.XmlNodeType]::Element -and $reader.Name -eq 'ResultSummary') {
                        $outcome = $reader.GetAttribute('outcome')
                        if ($outcomeForFramework -eq 'Completed') {
                            $outcomeForFramework = $outcome
                        }
                    }
                    if ($reader.NodeType -eq [System.Xml.XmlNodeType]::Element -and $reader.Name -eq 'Counters') {
                        $counters = New-CountersObject `
                            -Project $testName `
                            -Outcome $outcome `
                            -Total $reader.GetAttribute('total') `
                            -Executed $reader.GetAttribute('executed') `
                            -Passed $reader.GetAttribute('passed') `
                            -Failed $reader.GetAttribute('failed') `
                            -Warning $reader.GetAttribute('warning') `
                            -Inconclusive $reader.GetAttribute('inconclusive')

                        $totalCountForFramework += $counters.Total
                        $executedCountForFramework += $counters.Executed
                        $passedCountForFramework += $counters.Passed
                        $failedCountForFramework += $counters.Failed
                        $warningCountForFramework += $counters.Warning
                        $inconclusiveCountForFramework += $counters.Inconclusive
                        $skippedCountForFramework += $counters.Skipped

                        $format = @{Expression={$_.Project};Label='Project';Width=35},
                            @{Expression={$_.Outcome};Label='Outcome';Width=7},
                            @{Expression={$_.Total};Label='Total';Width=6},
                            @{Expression={$_.Executed};Label='Executed';Width=8},
                            @{Expression={$_.Passed};Label='Passed';Width=6},
                            @{Expression={$_.Failed};Label='Failed';Width=6},
                            @{Expression={$_.Warning};Label='Warning';Width=7},
                            @{Expression={$_.Inconclusive};Label='Inconclusive';Width=14}

                        if ($counters.Failed -gt 0) {
                            $Counters | Format-Table $format
                        }
                    }
                }

            } finally {
                $reader.Dispose()
            }

        }

        # FOOTER FOR FRAMEWORK

        #Write-Host "**********************************************************************" -ForegroundColor Magenta
        #Write-Host "*                                                                    *" -ForegroundColor Magenta
        #Write-Host "*                           Totals For $framework"  -ForegroundColor Magenta
        #Write-Host "*                                                                    *" -ForegroundColor Magenta
        #Write-Host "**********************************************************************" -ForegroundColor Magenta
        #Write-Host ""
        $foreground = if ($outcomeForFramework -eq 'Failed') { 'Red' } else { 'Green' }
        Write-Host "Result: " -NoNewline; Write-Host "$outcomeForFramework" -ForegroundColor $foreground
        Write-Host ""
        Write-Host "Total: $totalCountForFramework"
        Write-Host "Executed: $executedCountForFramework"
        $foreground = if ($failedCountForFramework -gt 0) { 'Green' } else { (Get-Host).UI.RawUI.ForegroundColor }
        Write-Host "Passed: " -NoNewline; Write-Host "$passedCountForFramework" -ForegroundColor $foreground
        $foreground = if ($failedCountForFramework -gt 0) { 'Red' } else { (Get-Host).UI.RawUI.ForegroundColor }
        Write-Host "Failed: " -NoNewline; Write-Host "$failedCountForFramework" -ForegroundColor $foreground
        $foreground = if ($failedCountForFramework -gt 0) { 'Yellow' } else { (Get-Host).UI.RawUI.ForegroundColor }
        Write-Host "Warning: " -NoNewline; Write-Host "$warningCountForFramework" -ForegroundColor $foreground
        $foreground = if ($failedCountForFramework -gt 0) { 'Cyan' } else { (Get-Host).UI.RawUI.ForegroundColor }
        Write-Host "Inconclusive: " -NoNewline; Write-Host "$inconclusiveCountForFramework" -ForegroundColor $foreground
        Write-Host ""
        Write-Host "See the .trx logs in $test_results_directory/$framework for more details." -ForegroundColor DarkCyan
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

function New-TemporaryDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    [string] $name = [System.Guid]::NewGuid()
    New-Item -ItemType Directory -Path (Join-Path $parent $name)
}