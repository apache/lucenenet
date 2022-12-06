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
    [string]$baseDirectory   = Resolve-Path "../."
    [string]$artifactsDirectory  = "$baseDirectory/_artifacts"
    [string]$sourceDirectory = "$baseDirectory/src"
    [string]$testDirectory = "$baseDirectory/src"
    [string]$toolsDirectory  = "$baseDirectory/lib"
    [string]$nugetPackageDirectory = "$artifactsDirectory/NuGetPackages"
    [string]$testResultsDirectory = "$artifactsDirectory/TestResults"
    [string]$publishDirectory = "$artifactsDirectory/Publish"
    [string]$solutionFile = "$baseDirectory/Lucene.Net.sln"
    [string]$minimumSdkVersion = "7.0.100"
    [string]$globalJsonFile = "$baseDirectory/global.json"
    [string]$versionPropsFile = "$baseDirectory/version.props"
    [string]$luceneReadmeFile = "$baseDirectory/src/Lucene.Net/readme-nuget.md"
    [string]$luceneCLIReadmeFile = "$baseDirectory/src/dotnet/tools/lucene-cli/docs/index.md"
    [string]$rootWebsiteUrl = "https://lucenenet.apache.org"
    [string]$rootDocsWebsiteUrl = "$rootWebsiteUrl/docs"

    [string]$buildCounter     = $(if ($buildCounter) { $buildCounter } else { $env:BuildCounter }) #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$preReleaseCounterPattern = $(if ($preReleaseCounterPattern) { $preReleaseCounterPattern } else { if ($env:PreReleaseCounterPattern) { $env:PreReleaseCounterPattern } else { "0000000000" } })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$versionSuffix    = $(if ($versionSuffix -ne $null) { $versionSuffix } else { if ($env:VersionSuffix -ne $null) { $env:VersionSuffix } else { 'ci' }}) #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$packageVersion   = Get-Package-Version #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$version          = Get-Version
    [string]$configuration    = $(if ($configuration) { $configuration } else { if ($env:BuildConfiguration) { $env:BuildConfiguration } else { "Release" } })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [string]$platform   = $(if ($platform) { $platform } else { if ($env:BuildPlatform) { $env:BuildPlatform } else { "Any CPU" } })  #NOTE: Pass in as a parameter (not a property) or environment variable to override
    [bool]$backupFiles       = $true
    [bool]$prepareForBuild    = $true
    [bool]$zipPublishedArtifacts = $false
    [string]$publishedArtifactZipFileName = "artifact.zip"

    [int]$maximumParallelJobs = 8
    
    #test parameters
    #The build uses Lucene.Net.Tests.Analysis.Common to determine all of the targets for the solution:
    [string]$projectWithAllTestFrameworks = "$baseDirectory/src/Lucene.Net.Tests.Analysis.Common/Lucene.Net.Tests.Analysis.Common.csproj"
    [string]$where = ""
}

$backedUpFiles = New-Object System.Collections.ArrayList
$addedFiles = New-Object System.Collections.ArrayList

task default -depends Pack

task Clean -description "This task cleans up the build directory" {
    Write-Host "##teamcity[progressMessage 'Cleaning']"
    Write-Host "##vso[task.setprogress]'Cleaning'"
    Remove-Item $artifactsDirectory -Force -Recurse -ErrorAction SilentlyContinue
    Get-ChildItem $baseDirectory -Include *.bak -Recurse | foreach ($_) {Remove-Item $_.FullName}
}

task UpdateLocalSDKVersion -description "Backs up the project.json file and pins the version to $minimumSdkVersion" {
    Backup-File $globalJsonFile
    Generate-Global-Json `
        -sdkVersion $minimumSdkVersion `
        -file $globalJsonFile
}

task CheckSDK -description "This task makes sure the correct SDK version is installed" {
    # Check prerequisites
    $sdkVersion = ((& dotnet --version) | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command was not found. Please install .NET $minimumSdkVersion or higher SDK and make sure it is in your PATH."
    }
    $releaseVersion = if ($sdkVersion.Contains('-')) { "$sdkVersion".Substring(0, "$sdkVersion".IndexOf('-')) } else { $sdkVersion }
    if ([version]$releaseVersion -lt ([version]$minimumSdkVersion)) {
        throw "Minimum .NET SDK $minimumSdkVersion required. Current SDK version is $releaseVersion. Please install the required SDK before running the command."
    }
}

task Init -depends CheckSDK, UpdateLocalSDKVersion -description "This task makes sure the build environment is correctly setup" {
    #Update TeamCity, MyGet, or Azure Pipelines with packageVersion
    Write-Output "##teamcity[buildNumber '$packageVersion']"
    Write-Output "##myget[buildNumber '$packageVersion']"
    Write-Host "##vso[build.updatebuildnumber]$packageVersion"

    & dotnet --version
    & dotnet --info
    Write-Host "Base Directory: $(Normalize-FileSystemSlashes "$baseDirectory")"
    Write-Host "Release Directory: $(Normalize-FileSystemSlashes "$artifactsDirectory")"
    Write-Host "Source Directory: $(Normalize-FileSystemSlashes "$sourceDirectory")"
    Write-Host "Test Directory: $(Normalize-FileSystemSlashes "$testDirectory")"
    Write-Host "Tools Directory: $(Normalize-FileSystemSlashes "$toolsDirectory")"
    Write-Host "NuGet Package Directory: $(Normalize-FileSystemSlashes "$nugetPackageDirectory")"
    Write-Host "BuildCounter: $buildCounter"
    Write-Host "PreReleaseCounterPattern: $preReleaseCounterPattern"
    Write-Host "VersionSuffix: $versionSuffix"
    Write-Host "Package Version: $packageVersion"
    Write-Host "File Version: $version"
    Write-Host "Configuration: $configuration"
    Write-Host "Platform: $platform"
    Write-Host "MaximumParallelJobs: $($maximumParallelJobs.ToString())"
    Write-Host "Powershell Version: $($PSVersionTable.PSVersion)"

    Ensure-Directory-Exists "$artifactsDirectory"
}

task Restore -description "This task restores the dependencies" {
    Write-Host "##teamcity[progressMessage 'Restoring']"
    Write-Host "##vso[task.setprogress]'Restoring'"
    Exec { 
        & dotnet restore $solutionFile --no-dependencies /p:TestFrameworks=true
    }
}

task Compile -depends Clean, Init, Restore -description "This task compiles the solution" {
    Write-Host "##teamcity[progressMessage 'Compiling']"
    Write-Host "##vso[task.setprogress]'Compiling'"
    try {
        if ($prepareForBuild -eq $true) {
            Prepare-For-Build
        }

        Exec {
            # NOTE: Version information is not passed in at the command line,
            # instead it is output to the version.props file. This file is then
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
            Delete-Added-Files $addedFiles
            Restore-Files $backedUpFiles
        }
    }
}

task Pack -depends Compile -description "This task creates the NuGet packages" {
    Write-Host "##teamcity[progressMessage 'Packing']"
    Write-Host "##vso[task.setprogress]'Packing'"
    #create the nuget package output directory
    Ensure-Directory-Exists "$nugetPackageDirectory"
    Update-Lucene-Readme-For-Pack $packageVersion
    Update-LuceneCLI-Readme-For-Pack $packageVersion

    try {
        Exec {
            # NOTE: Package version information is not passed in at the command line,
            # instead it is output to the version.props file. This file is then
            # used during a release to "freeze" the build at a specific version
            # so it is always a constant in release distributions.
            & dotnet pack $solutionFile --configuration $configuration --output $nugetPackageDirectory --no-build
        }

        # Set the SYSTEM_DEFAULTWORKINGDIRECTORY if we are not on Azure DevOps. This will optimize the lucene-cli
        # installation test so it doesn't rebuild the NuGet package during a local build.
        if (-Not (Test-Path 'env:SYSTEM_DEFAULTWORKINGDIRECTORY')) {
            $env:SYSTEM_DEFAULTWORKINGDIRECTORY = "$baseDirectory"
        }

        Write-Host ""
        Write-Host "\nSYSTEM_DEFAULTWORKINGDIRECTORY is $env:SYSTEM_DEFAULTWORKINGDIRECTORY (for lucene-cli installation tests)" -ForegroundColor Yellow

        $success = $true
    } finally {
        #if ($success -ne $true) {
            Delete-Added-Files $addedFiles
            Restore-Files $backedUpFiles
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
            $outDirectory = $publishDirectory
        }
        
        foreach ($framework in $frameworksToTest) {

            # Pause if we have queued too many parallel jobs
            $running = @(Get-Job | Where-Object { $_.State -eq 'Running' })
            if ($running.Count -ge $maximumParallelJobs) {
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
            Ensure-Directory-Exists $publishDirectory
            Add-Type -assembly "System.IO.Compression.Filesystem"
            [System.IO.Compression.ZipFile]::CreateFromDirectory($outDirectory, "$publishDirectory/$publishedArtifactZipFileName")
        }

        $success = $true
    } finally {
        #if ($success -ne $true) {
            Delete-Added-Files $addedFiles
            Restore-Files $backedUpFiles
        #}
    }
}

task Test -depends CheckSDK, UpdateLocalSDKVersion, Restore -description "This task runs the tests" {
    Write-Host "##teamcity[progressMessage 'Testing']"
    Write-Host "##vso[task.setprogress]'Testing'"
    Write-Host "Running tests..." -ForegroundColor DarkCyan

    pushd $baseDirectory
    $testProjects = Get-ChildItem -Path "$testDirectory/**/*.csproj" -Recurse | ? { $_.Directory.Name.Contains(".Tests") }
    popd

    $testProjects = $testProjects | Sort-Object -Property FullName
    
    $frameworksToTest = Get-FrameworksToTest

    [int]$totalProjects = $testProjects.Length * $frameworksToTest.Length
    [int]$remainingProjects = $totalProjects

    Ensure-Directory-Exists $testResultsDirectory

    foreach ($testProject in $testProjects) {
        $testName = $testProject.Directory.Name
        
        # Call the target to get the configured test frameworks for this project. We only read the first line because MSBuild adds extra output.
        $frameworksString = $(dotnet build "$testProject" --verbosity minimal --nologo --no-restore /t:PrintTargetFrameworks /p:TestProjectsOnly=true /p:TestFrameworks=true)[0].Trim()

        Write-Host ""
        Write-Host "Frameworks To Test for ${testProject}: $frameworksString" -ForegroundColor Yellow

        if ($frameworksString -eq 'none') {
            Write-Host ""
            Write-Host "Skipping project '$testProject' because it is not marked with `<IsTestProject`>true`<`/IsTestProject`> and/or it contains no test frameworks for the current environment." -ForegroundColor DarkYellow
            continue
        }

        $frameworks = [System.Collections.Generic.HashSet[string]]::new($frameworksString -split '\s*;\s*')
        foreach ($framework in $frameworksToTest) {
            
            # If the framework is not valid for this configuration, we need to adjust our
            # initial estimate and skip the combination.
            if (-not $frameworks.Contains($framework)) {
                $totalProjects--
                $remainingProjects--
                continue
            }
            
            Write-Host ""
            Write-Host "  Next Project in Queue: $testName, Framework: $framework" -ForegroundColor Yellow

            # Pause if we have queued too many parallel jobs
            $running = @(Get-Job | Where-Object { $_.State -eq 'Running' })
            if ($running.Count -ge $maximumParallelJobs) {
                Write-Host ""
                Write-Host "  Running tests in parallel on $($running.Count) projects out of approximately $totalProjects total." -ForegroundColor Cyan
                Write-Host "  $remainingProjects projects are waiting in the queue to run. This will take a bit, please wait..." -ForegroundColor Cyan
                $running | Wait-Job -Any | Out-Null
            }
            $remainingProjects -= 1

            $testResultDirectory = "$testResultsDirectory/$framework/$testName"
            Ensure-Directory-Exists $testResultDirectory

            $testProjectPath = $testProject.FullName
            $testExpression = "dotnet test $testProjectPath --configuration $configuration --framework $framework --no-build"
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
            #   throw "Test execution failed"
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
        $versionFile = "$baseDirectory/Directory.Build.props"
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
    # Call the target to get the configured test frameworks for a project known to contain all of them. We only read the first line because MSBuild adds extra output.
    $frameworksString = $(dotnet build "$projectWithAllTestFrameworks" --verbosity minimal --nologo --no-restore /t:PrintTargetFrameworks /p:TestProjectsOnly=true /p:TestFrameworks=true)[0].Trim()
    $frameworksToTest = $frameworksString -split '\s*;\s*'
    Assert($frameworksToTest.Length -gt 0) "The project at $(Normalize-FileSystemSlashes "$projectWithAllTestFrameworks") contains no target frameworks. Please configure a project that includes all testable target frameworks."
    return $frameworksToTest
}

function Prepare-For-Build() {
    #Use only the major version as the assembly version.
    #This ensures binary compatibility unless the major version changes.
    $version -match "(^\d+)"
    $assemblyVersion = $Matches[0]
    $assemblyVersion = "$assemblyVersion.0.0"

    Write-Host "Assembly version set to: $assemblyVersion" -ForegroundColor Green

    $informationalVersion = $packageVersion

    # Only update the version if git is present and the command to read the commit succeeds
    $gitCommit = ((git rev-parse --verify --short=10 head) | Out-String).Trim()
    if ($LASTEXITCODE -eq 0) {
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
}

function Update-Constants-Version([string]$version) {
    $constantsFile = "$baseDirectory/src/Lucene.Net/Util/Constants.cs"

    Backup-File $constantsFile
    (Get-Content $constantsFile) | % {
        $_-replace "(?<=LUCENE_VERSION\s*?=\s*?"")([^""]*)", $version
    } | Set-Content $constantsFile -Force
}

function Update-Lucene-Readme-For-Pack([string]$version) {
    Backup-File $luceneReadmeFile
    (Get-Content $luceneReadmeFile) | % {
        # Replace version in lucene-cli install command
        $_ -replace "(?<=lucene-cli(?:\s?-{1,2}\w*?)*?\s+--version\s+)(\d+\.\d+\.\d+(?:\.\d+)?(?:-\w*)?)", $version
    } | % {
        # NuGet absoluteLatest package URL references with current version
        $_ -replace "(?<=https?://(?:[\w/\.]*?))(absoluteLatest)", $version
    } | % {
        # Replace doc version number URL with the current version
        $_ -replace "(?<=https?://(?:[\w/\.]*?)/docs/)(\d+\.\d+\.\d+(?:\.\d+)?(?:-\w*)?)", $version
    } | Set-Content $luceneReadmeFile -Force
}

function Update-LuceneCLI-Readme-For-Pack([string]$version) {
    Backup-File $luceneCLIReadmeFile
    (Get-Content $luceneCLIReadmeFile) | % {
        # Replace version in lucene-cli install command
        $_ -replace "(?<=lucene-cli(?:\s?-{1,2}\w*?)*?\s+--version\s+)(\d+\.\d+\.\d+(?:\.\d+)?(?:-\w*)?)", $version
    } | % {
        # Replace markdown file references with website URLs to the correct documentation version
        $_ -replace "(?<=\()(\w*/index).md(?=\))", "$rootDocsWebsiteUrl/$version/cli/`$1.html"
    } | Set-Content $luceneCLIReadmeFile -Force
}

function Generate-Global-Json {
param(
    [string]$sdkVersion,
    [string]$file = $(throw "file is a required parameter.")
)

$fileText = "{
  ""sources"": [ ""src"" ],
  ""sdk"": {
    ""version"": ""$sdkVersion"",
    ""rollForward"": ""latestMajor""
  }
}"
    $dir = [System.IO.Path]::GetDirectoryName($file)
    Ensure-Directory-Exists $dir

    Write-Host "Generating global.json file: $(Normalize-FileSystemSlashes "$file")"
    Track-Added-File $file
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

    Write-Host "Generating version.props file: $(Normalize-FileSystemSlashes "$file")"
    Track-Added-File $file
    Out-File -filePath $file -encoding UTF8 -inputObject $fileText
}

function New-CountersObject ([string]$project, [string]$outcome, [int]$total, [int]$executed, [int]$passed, [int]$failed, [int]$warning, [int]$inconclusive) {
    $counters = New-Object -TypeName PSObject
    $fields = [ordered]@{Project=$project;Outcome=$outcome;Total=$total;Executed=$executed;Passed=$passed;Failed=$failed;Warning=$warning;Inconclusive=$inconclusive}
    $counters | Add-Member -NotePropertyMembers $fields -TypeName Counters
    return $counters
}

function Summarize-Test-Results([string[]]$frameworksToTest) {

    # Workaround for issue when ForeGroundColor cannot be read. https://stackoverflow.com/a/26583010
    $defaultForeground = (Get-Host).UI.RawUI.ForegroundColor
    if ($defaultForeground -eq -1) {
        $defaultForeground = 'White'
    }

    foreach ($framework in $frameworksToTest) {
        pushd $baseDirectory
        $testReports = Get-ChildItem -Path "$testResultsDirectory/$framework" -Recurse -File -Filter "*.trx" | ForEach-Object {
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
        $foreground = if ($failedCountForFramework -gt 0) { 'Green' } else { $defaultForeground }
        Write-Host "Passed: " -NoNewline; Write-Host "$passedCountForFramework" -ForegroundColor $foreground
        $foreground = if ($failedCountForFramework -gt 0) { 'Red' } else { $defaultForeground }
        Write-Host "Failed: " -NoNewline; Write-Host "$failedCountForFramework" -ForegroundColor $foreground
        $foreground = if ($failedCountForFramework -gt 0) { 'Yellow' } else { $defaultForeground }
        Write-Host "Warning: " -NoNewline; Write-Host "$warningCountForFramework" -ForegroundColor $foreground
        $foreground = if ($failedCountForFramework -gt 0) { 'Cyan' } else { $defaultForeground }
        Write-Host "Inconclusive: " -NoNewline; Write-Host "$inconclusiveCountForFramework" -ForegroundColor $foreground
        Write-Host ""
        Write-Host "See the .trx logs in $(Normalize-FileSystemSlashes "$testResultsDirectory/$framework") for more details." -ForegroundColor DarkCyan
    }
}

function Backup-Files([string[]]$paths) {
    foreach ($path in $paths) {
        Backup-File $path
    }
}

function Backup-File([string]$path) {
    if ($backupFiles -eq $true) {
        Copy-Item $path "$path.bak" -Force
        $backedUpFiles.Insert(0, $path)
    } else {
        Write-Host "Ignoring backup of file $(Normalize-FileSystemSlashes "$path")" -ForegroundColor DarkRed
    }
}

function Restore-Files([string[]]$paths) {
    foreach ($path in $paths) {
        Restore-File $path
    }
}

function Restore-File([string]$path) {
    if ($backupFiles -eq $true) {
        if (Test-Path "$path.bak") {
            Move-Item "$path.bak" $path -Force
        }
        $backedUpFiles.Remove($path)
    }
}

function Track-Added-Files([string[]]$paths) {
    foreach ($path in $paths) {
        Track-Added-File $path
    }
}

function Track-Added-File([string]$path) {
    if ($backupFiles -eq $true) {
        $addedFiles.Insert(0, $path)
    } else {
        Write-Host "Ignoring tracking of file $(Normalize-FileSystemSlashes "$path")" -ForegroundColor DarkRed
    }
}

function Delete-Added-Files([string[]]$paths) {
    foreach ($path in $paths) {
        Delete-Added-File $path
    }
}

function Delete-Added-File([string]$path) {
    if ($backupFiles -eq $true) {
        if (Test-Path "$path") {
            Remove-Item "$path" -Force
        }
        $addedFiles.Remove($path)
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

function Normalize-FileSystemSlashes([string]$path) {
    $sep = [System.IO.Path]::DirectorySeparatorChar
    return $($path -replace '/',$sep -replace '\\',$sep)
}