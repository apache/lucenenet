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

<#
 .SYNOPSIS
    Generates GitHub Actions workflows for running tests upon a pull request action (either a
    new pull request or a push to an existing one).

 .DESCRIPTION
    Generates 1 GitHub Actions workflow file for each project containing the string ".Tests"
    in the name. The current project, ProjectReference dependencies, and common files
    Directory.Build.*, TestTargetFraemworks.*, TestReferences.Common.* and Dependencies.props
    are all used to build filter paths to determine when the workflow will run.

 .PARAMETER OutputDirectory
    The directory to output the files. This should be in a directory named /.github/workflows
    in the root of the repository. The default is the directory of this script file.

 .PARAMETER RepoRoot
    The directory of the repository root. Defaults to two levels above the directory
    of this script file.

 .PARAMETER TestFrameworks
    A string array of Dotnet target framework monikers to run the tests on. The default is
    @('net6.0', 'net5.0','net461','net48').

 .PARAMETER OperatingSystems
    A string array of Github Actions operating system monikers to run the tests on.
    The default is @('windows-latest', 'ubuntu-latest').

 .PARAMETER TestPlatforms
    A string array of platforms to run the tests on. Valid values are x64 and x86.
    The default is @('x64').

 .PARAMETER Configurations
    A string array of build configurations to run the tests on. The default is @('Release').

 .PARAMETER DotNet7SDKVersion
    The SDK version of .NET 7.x to install on the build agent to be used for building and
    testing. This SDK is always installed on the build agent. The default is 7.0.100.

 .PARAMETER DotNet6SDKVersion
    The SDK version of .NET 6.x to install on the build agent to be used for building and
    testing. This SDK is always installed on the build agent. The default is 6.0.403.

 .PARAMETER DotNet5SDKVersion
    The SDK version of .NET 5.x to install on the build agent to be used for building and
    testing. This SDK is always installed on the build agent. The default is 5.0.400.
#>
param(
    [string]$OutputDirectory =  $PSScriptRoot,

    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot)),

    [string[]]$TestFrameworks = @('net7.0', 'net5.0','net461','net48'), # targets under test: net6.0, netstandard2.1, netstanard2.0, net462

    [string[]]$OperatingSystems = @('windows-latest', 'ubuntu-latest'),

    [string[]]$TestPlatforms = @('x64'),

    [string[]]$Configurations = @('Release'),

    [string]$DotNet7SDKVersion = '7.0.100',

    [string]$DotNet6SDKVersion = '6.0.403',

    [string]$DotNet5SDKVersion = '5.0.400'
)


function Resolve-RelativePath([string]$RelativeRoot, [string]$Path) {
    Push-Location -Path $RelativeRoot
    try {
        return Resolve-Path $Path -Relative
    } finally {
        Pop-Location
    }
}

function Get-ProjectDependencies([string]$ProjectPath, [string]$RelativeRoot, [System.Collections.Generic.HashSet[string]]$Result) {
    $resolvedProjectPath = $ProjectPath
    $rootPath = [System.IO.Path]::GetDirectoryName($resolvedProjectPath)
    [xml]$project = Get-Content $resolvedProjectPath
    foreach ($name in $project.SelectNodes("//Project/ItemGroup/ProjectReference") | Where-Object { $_.Include -notmatch '^$' } | ForEach-Object { $_.Include -split ';'}) {
        $dependencyFullPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($rootPath, $name))
        Get-ProjectDependencies $dependencyFullPath $RelativeRoot $Result
        $dependency = Resolve-RelativePath $RelativeRoot $dependencyFullPath
        $result.Add($dependency) | Out-Null
    }
}

function Get-ProjectExternalPaths([string]$ProjectPath, [string]$RelativeRoot, [System.Collections.Generic.HashSet[string]]$Result) {
    $resolvedProjectPath = $ProjectPath
    $rootPath = [System.IO.Path]::GetDirectoryName($resolvedProjectPath)
    [xml]$project = Get-Content $resolvedProjectPath
    foreach ($name in $project.SelectNodes("//Project/ItemGroup/Compile") | Where-Object { $_.Include -notmatch '^$' } | ForEach-Object { $_.Include -split ';'}) {
        # Temporarily override wildcard patterns so we can resolve the path and then put them back.
        $name = $name -replace '\\\*\*\\\*', 'Wildcard1' -replace '\*', 'Wildcard2'
        $dependencyFullPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($rootPath, $name)) -replace 'Wildcard1', '\**\*' -replace 'Wildcard2', '*'
        # Make the path relative to the repo root.
        $dependency = $($($dependencyFullPath.Replace($RelativeRoot, '.')) -replace '\\', '/').TrimStart('./')
        $result.Add($dependency) | Out-Null
    }
    foreach ($name in $project.SelectNodes("//Project/ItemGroup/EmbeddedResource") | Where-Object { $_.Include -notmatch '^$' } | ForEach-Object { $_.Include -split ';'}) {
        # Temporarily override wildcard patterns so we can resolve the path and then put them back.
        $name = $name -replace '\\\*\*\\\*', 'Wildcard1' -replace '\*', 'Wildcard2'
        $dependencyFullPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($rootPath, $name)) -replace 'Wildcard1', '\**\*' -replace 'Wildcard2', '*'
        # Make the path relative to the repo root.
        $dependency = $($($dependencyFullPath.Replace($RelativeRoot, '.')) -replace '\\', '/').TrimStart('./')
        $result.Add($dependency) | Out-Null
    }
}

function Get-ProjectPathDirectories([string]$ProjectPath, [string]$RelativeRoot, [System.Collections.Generic.HashSet[string]]$Result) {
    $currentPath = New-Object System.IO.DirectoryInfo([System.IO.Path]::GetDirectoryName($ProjectPath))
    $currentRelativePath = Resolve-RelativePath $RelativeRoot $currentPath.FullName
    $Result.Add($currentRelativePath) | Out-Null
    while ($true) {
        $prevDirectory = New-Object System.IO.DirectoryInfo($currentPath.FullName)
        $currentPath = $prevDirectory.Parent
        if ($currentPath -eq $null) {
            break
        }
        if ($currentPath.FullName -eq $RelativeRoot) {
            $Result.Add(".") | Out-Null
            break
        }
        $currentRelativePath = Resolve-RelativePath $RelativeRoot $currentPath.FullName
        $Result.Add($currentRelativePath) | Out-Null
    }
}

function Ensure-Directory-Exists([string] $path) {
    if (!(Test-Path $path)) {
        New-Item $path -ItemType Directory
    }
}

function Write-TestWorkflow(
    [string]$OutputDirectory = $PSScriptRoot, #optional
    [string]$RelativeRoot,
    [string]$ProjectPath,
    [string[]]$Configurations = @('Release'),
    [string[]]$TestFrameworks = @('net5.0', 'net48'),
    [string[]]$TestPlatforms = @('x64'),
    [string[]]$OperatingSystems = @('windows-latest', 'ubuntu-latest', 'macos-latest'),
    [string]$DotNet7SDKVersion = $DotNet7SDKVersion,
    [string]$DotNet6SDKVersion = $DotNet6SDKVersion,
    [string]$DotNet5SDKVersion = $DotNet5SDKVersion) {

    $dependencies = New-Object System.Collections.Generic.HashSet[string]
    Get-ProjectDependencies $ProjectPath $RelativeRoot $dependencies
    $dependencyPaths = [System.Environment]::NewLine
    foreach ($dependency in $dependencies) {
        $dependencyRelativeDirectory = ([System.IO.Path]::GetDirectoryName($dependency) -replace '\\', '/').TrimStart('./')
        $dependencyPaths += "    - '$dependencyRelativeDirectory/**/*'" + [System.Environment]::NewLine
    }

    $projectRelativePath = $(Resolve-RelativePath $RelativeRoot $ProjectPath) -replace '\\', '/'
    $projectRelativeDirectory = ([System.IO.Path]::GetDirectoryName($projectRelativePath) -replace '\\', '/').TrimStart('./')
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)

    [bool]$isCLI = if ($projectName -eq "Lucene.Net.Tests.Cli") { $true } else { $false }       # Special case
    $luceneCliProjectPath = $projectRelativePath -replace "Lucene.Net.Tests.Cli", "lucene-cli"  # Special case

    [string]$frameworks = '[' + $($TestFrameworks -join ', ') + ']'
    [string]$platforms = '[' + $($TestPlatforms -join ', ') + ']'
    [string]$oses = '[' + $($OperatingSystems -join ', ') + ']'
    [string]$configurations = '[' + $($Configurations -join ', ') + ']'

    $directories = New-Object System.Collections.Generic.HashSet[string]
    Get-ProjectPathDirectories $projectPath $RepoRoot $directories

    $directoryBuildPaths = [System.Environment]::NewLine
    foreach ($directory in $directories) {
        $relativeDirectory = ([System.IO.Path]::Combine($directory, 'Directory.Build.*') -replace '\\', '/').TrimStart('./')
        $directoryBuildPaths += "    - '$relativeDirectory'" + [System.Environment]::NewLine
    }

    $paths = New-Object System.Collections.Generic.HashSet[string]
    Get-ProjectExternalPaths $ProjectPath $RelativeRoot $paths

    foreach ($path in $paths) {
        $directoryBuildPaths += "    - '$path'" + [System.Environment]::NewLine
    }

    

    $fileText = "####################################################################################
# DO NOT EDIT: This file was automatically generated by Generate-TestWorkflows.ps1
####################################################################################
# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# `"License`"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
# 
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# `"AS IS`" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.

name: '$projectName'

on:
  workflow_dispatch:
  pull_request:
    paths:
    - '$projectRelativeDirectory/**/*'
    - '.build/dependencies.props'
    - '.build/TestReferences.Common.*'
    - 'TestTargetFrameworks.*'
    - '*.sln'$directoryBuildPaths
    # Dependencies$dependencyPaths
    - '!**/*.md'

jobs:

  Test:
    runs-on: `${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: $oses
        framework: $frameworks
        platform: $platforms
        configuration: $configurations
        exclude:
          - os: ubuntu-latest
            framework: net48
          - os: ubuntu-latest
            framework: net461
          - os: macos-latest
            framework: net48
          - os: macos-latest
            framework: net461
    env:
      project_path: '$projectRelativePath'"

    if ($isCLI) {
        $fileText += "
      project_under_test_path: '$luceneCliProjectPath'
      run_slow_tests: 'true'"
    } else {
        $fileText += "
      run_slow_tests: 'false'"
    }

    $fileText += "
      trx_file_name: 'TestResults.trx'
      md_file_name: 'TestResults.md' # Report file name for LiquidTestReports.Markdown

    steps:
      - name: Checkout Source Code
        uses: actions/checkout@v2

      - name: Disable .NET SDK Telemetry and Logo
        run: |
          echo `"DOTNET_NOLOGO=1`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append
          echo `"DOTNET_CLI_TELEMETRY_OPTOUT=1`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append
        shell: pwsh

      - name: Setup .NET 5 SDK
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '$DotNet5SDKVersion'
        if: `${{ startswith(matrix.framework, 'net5.') }}

      - name: Setup .NET 6 SDK
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '$DotNet6SDKVersion'
        if: `${{ startswith(matrix.framework, 'net6.') }}

      - name: Setup .NET 7 SDK
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '$DotNet7SDKVersion'

      - name: Setup Environment Variables
        run: |
          `$project_name = [System.IO.Path]::GetFileNameWithoutExtension(`$env:project_path)
          `$test_results_artifact_name = `"testresults_`${{matrix.os}}_`${{matrix.framework}}_`${{matrix.platform}}_`${{matrix.configuration}}`"
          `$working_directory = `"`$env:GITHUB_WORKSPACE`"
          Write-Host `"Project Name: `$project_name`"
          Write-Host `"Results Artifact Name: `$test_results_artifact_name`"
          Write-Host `"Working Directory: `$working_directory`"
          echo `"project_name=`$project_name`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append
          echo `"test_results_artifact_name=`$test_results_artifact_name`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append
          # Set the Azure DevOps default working directory env variable, so our tests only need to deal with a single env variable
          echo `"SYSTEM_DEFAULTWORKINGDIRECTORY=`$working_directory`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append
          # Title for LiquidTestReports.Markdown
          echo `"title=Test Run for `$project_name - `${{matrix.framework}} - `${{matrix.platform}} - `${{matrix.os}}`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append
        shell: pwsh"

    if ($isCLI) {
        # Special case: Generate lucene-cli.nupkg for installation test so the test runner doesn't have to do it
        $fileText += "
      - run: dotnet pack `${{env.project_under_test_path}} --configuration `${{matrix.configuration}} /p:TestFrameworks=true /p:PortableDebugTypeOnly=true"
    }

    $fileText += "
      - run: dotnet build `${{env.project_path}} --configuration `${{matrix.configuration}} --framework `${{matrix.framework}} /p:TestFrameworks=true
      - run: dotnet test `${{env.project_path}} --configuration `${{matrix.configuration}} --framework `${{matrix.framework}} --no-build --no-restore --blame-hang --blame-hang-dump-type mini --blame-hang-timeout 20minutes --logger:`"console;verbosity=normal`" --logger:`"trx;LogFileName=`${{env.trx_file_name}}`" --logger:`"liquid.md;LogFileName=`${{env.md_file_name}};Title=`${{env.title}};`" --results-directory:`"`${{github.workspace}}/`${{env.test_results_artifact_name}}/`${{env.project_name}}`" -- RunConfiguration.TargetPlatform=`${{matrix.platform}} TestRunParameters.Parameter\(name=\`"tests:slow\`",\ value=\`"\`${{env.run_slow_tests}}\`"\)
        shell: bash
      # upload reports as build artifacts
      - name: Upload a Build Artifact
        uses: actions/upload-artifact@v2
        if: `${{always()}}
        with:
          name: '`${{env.test_results_artifact_name}}'
          path: '`${{github.workspace}}/`${{env.test_results_artifact_name}}'
"

    # GitHub Actions does not support filenames with "." in them, so replace
    # with "-"
    $projectFileName = $projectName -replace '\.', '-'
    $FilePath = "$OutputDirectory/$projectFileName.yml"

    #$dir = [System.IO.Path]::GetDirectoryName($File)
    Ensure-Directory-Exists $OutputDirectory

    Write-Host "Generating workflow file: $FilePath"
    Out-File -filePath $FilePath -encoding UTF8 -inputObject $fileText

    #Write-Host $fileText
}


Push-Location $RelativeRoot
try {
    [string[]]$TestProjects = Get-ChildItem -Path "$RepoRoot/**/*.csproj" -Recurse | where { $_.Directory.Name.Contains(".Tests") -and !($_.Directory.FullName.Contains('svn-')) } | Select-Object -ExpandProperty FullName
} finally {
    Pop-Location
}

#Write-TestWorkflow -OutputDirectory $OutputDirectory -ProjectPath $projectPath -RelativeRoot $repoRoot -TestFrameworks @('net5.0') -OperatingSystems $OperatingSystems -TestPlatforms $TestPlatforms -Configurations $Configurations -DotNet7SDKVersion $DotNet7SDKVersion -DotNet6SDKVersion $DotNet6SDKVersion -DotNet5SDKVersion $DotNet5SDKVersion

#Write-Host $TestProjects

foreach ($testProject in $TestProjects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
    
     # Call the target to get the configured test frameworks for this project. We only read the first line because MSBuild adds extra output.
    $frameworksString = $(dotnet build "$testProject" --verbosity minimal --nologo --no-restore /t:PrintTargetFrameworks /p:TestProjectsOnly=true /p:TestFrameworks=true)[0].Trim()

    if ($frameworksString -eq 'none') {
        Write-Host "WARNING: Skipping project '$projectName' because it is not marked with `<IsTestProject`>true`<`/IsTestProject`> and/or it contains no test frameworks for the current environment." -ForegroundColor Yellow
        continue
    }

    [string[]]$frameworks = $frameworksString -split '\s*;\s*'
    $frameworks = $frameworks | ? { $TestFrameworks -contains $_ } # IntersectWith

    if ($frameworks.Count -eq 0) {
        Write-Host "WARNING: ${projectName} contains no matching target frameworks: $frameworksString" -ForegroundColor Yellow
        continue
    }

    Write-Host ""
    Write-Host "Frameworks To Test for ${projectName}: $($frameworks -join ';')" -ForegroundColor Cyan

    #Write-Host "Project: $projectName"
    Write-TestWorkflow -OutputDirectory $OutputDirectory -ProjectPath $testProject -RelativeRoot $RepoRoot -TestFrameworks $frameworks -OperatingSystems $OperatingSystems -TestPlatforms $TestPlatforms -Configurations $Configurations -DotNet7SDKVersion $DotNet7SDKVersion -DotNet6SDKVersion $DotNet6SDKVersion -DotNet5SDKVersion $DotNet5SDKVersion
}