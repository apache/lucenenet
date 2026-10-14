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
    Generates GitHub Actions workflow files for projects containing the string ".Tests"
    in the name. The current project, ProjectReference dependencies, and common files
    Directory.Build.*, TestTargetFraemworks.*, TestReferences.Common.* and Dependencies.props
    are all used to build filter paths to determine when the workflow will run.

    Most projects are grouped into a shared workflow rather than getting a file of their own.
    A GitHub Actions job spends roughly 110 seconds on checkout, SDK setup, and restore before
    a single test runs, so a project with only a handful of tests spends ~96% of its job doing
    setup. Grouping several such projects into one job pays that cost once instead of per
    project.

    A project joins a group by setting the TestShard MSBuild property in its .csproj file:

        <TestShard>Search</TestShard>

    All projects with the same TestShard value run in one workflow named
    Lucene.Net.Tests.<TestShard>. Every project in a group must support the same set of
    target frameworks, because the members share a single build matrix, and a group must
    have more than one member.

    A project that should get a workflow file of its own sets the TestShardIsolated MSBuild
    property instead:

        <TestShardIsolated>true</TestShardIsolated>

    Projects with a large number of tests (the Lucene.Net.Tests._* letter shards and
    Lucene.Net.Tests.Analysis.Common), and those needing a non-default build matrix or extra
    build steps (Lucene.Net.Tests.Cli, Lucene.Net.Tests.CodeAnalysis and
    Lucene.Net.Tests.Analysis.OpenNLP), are isolated.

    Every test project must set exactly one of these two properties, so that a new project is
    placed deliberately rather than by default. The script fails, listing every offending
    project, if any project sets neither or both, or if any of the group rules above are
    broken.

 .PARAMETER OutputDirectory
    The directory to output the files. This should be in a directory named /.github/workflows
    in the root of the repository. The default is the directory of this script file.

 .PARAMETER RepoRoot
    The directory of the repository root. Defaults to two levels above the directory
    of this script file.

 .PARAMETER TestFrameworks
    A string array of Dotnet target framework monikers to run the tests on. The default is
    @('net10.0','net8.0','net472','net48').

 .PARAMETER OperatingSystems
    A string array of Github Actions operating system monikers to run the tests on.
    The default is @('windows-latest', 'ubuntu-latest').

 .PARAMETER TestPlatforms
    A string array of platforms to run the tests on. Valid values are x64 and x86.
    The default is @('x64').

 .PARAMETER Configurations
    A string array of build configurations to run the tests on. The default is @('Release').

 .PARAMETER DotNet10SDKVersion
    The SDK version of .NET 10.x to install on the build agent to be used for building and
    testing. This SDK is always installed on the build agent. The default is 10.0.x.

 .PARAMETER DotNet8SDKVersion
    The SDK version of .NET 8.x to install on the build agent to be used for building and
    testing. This SDK is always installed on the build agent. The default is 8.0.x.

#>
param(
    [string]$OutputDirectory =  $PSScriptRoot,

    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot)),

    [string[]]$TestFrameworks = @('net10.0', 'net8.0', 'net472', 'net48'), # targets under test: net10.0, net8.0, netstandard2.0, net462

    [string[]]$OperatingSystems = @('windows-latest', 'ubuntu-latest'),

    [string[]]$TestPlatforms = @('x64'),

    [string[]]$Configurations = @('Release'),

    [string]$DotNet10SDKVersion = '10.0.x',

    [string]$DotNet8SDKVersion = '8.0.x'
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

function Get-SupportedTargetFrameworksString([Parameter(Mandatory)][string] $ProjectPath) {
    # NOTE: This will not appear when run directly in the console with minimal verbosity. MSBuild only produces the output when using a pipe, which is what we are doing here.
    $output = dotnet build "$ProjectPath" --verbosity minimal --nologo --no-restore /t:PrintTargetFrameworks /p:TestProjectsOnly=true /p:TestFrameworks=true 2>&1 | Out-String
    if ($output -match 'SupportedTargetFrameworks=([^\s]+)') {
        return $matches[1]
    }
    throw "Failed to determine supported target frameworks for project: $ProjectPath"
}

# Returns the TestShard and TestShardIsolated MSBuild properties for the project, as an object
# with TestShard and TestShardIsolated members (empty strings when unset). Projects that share
# a TestShard value run in the same workflow; isolated projects get a workflow of their own.
function Get-TestShardProperties([Parameter(Mandatory)][string] $ProjectPath) {
    # With more than one -getProperty, MSBuild prints the evaluated values as JSON.
    $output = dotnet msbuild "$ProjectPath" -nologo -getProperty:TestShard -getProperty:TestShardIsolated 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to determine the TestShard properties for project: $ProjectPath$([System.Environment]::NewLine)$output"
    }
    $properties = ($output | ConvertFrom-Json).Properties
    return [pscustomobject]@{
        TestShard = "$($properties.TestShard)".Trim()
        TestShardIsolated = "$($properties.TestShardIsolated)".Trim()
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
    [string[]]$ProjectPaths,
    [string]$WorkflowName,
    [string[]]$Configurations = @('Release'),
    [string[]]$TestFrameworks = @('net6.0', 'net48'),
    [string[]]$TestPlatforms = @('x64'),
    [string[]]$OperatingSystems = @('windows-latest', 'ubuntu-latest', 'macos-latest'),
    [string]$DotNet10SDKVersion = $DotNet10SDKVersion,
    [string]$DotNet8SDKVersion = $DotNet8SDKVersion) {

    # A workflow may run more than one test project in the same job. The path filters below are
    # the union over every project in the workflow, so the workflow runs whenever any member
    # (or any of its dependencies) changes.
    $projectRelativePaths = @($ProjectPaths | ForEach-Object { $(Resolve-RelativePath $RelativeRoot $_) -replace '\\', '/' })
    $projectNames = @($ProjectPaths | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) })

    # When no explicit name is given, a single-project workflow is named after its project.
    if ([string]::IsNullOrEmpty($WorkflowName)) {
        $WorkflowName = $projectNames[0]
    }

    $dependencies = New-Object System.Collections.Generic.HashSet[string]
    $directories = New-Object System.Collections.Generic.HashSet[string]
    $paths = New-Object System.Collections.Generic.HashSet[string]
    $projectDirectories = New-Object System.Collections.Generic.HashSet[string]

    foreach ($currentProjectPath in $ProjectPaths) {
        Get-ProjectDependencies $currentProjectPath $RelativeRoot $dependencies
        Get-ProjectPathDirectories $currentProjectPath $RepoRoot $directories
        Get-ProjectExternalPaths $currentProjectPath $RelativeRoot $paths

        $currentRelativePath = $(Resolve-RelativePath $RelativeRoot $currentProjectPath) -replace '\\', '/'
        $projectDirectories.Add((([System.IO.Path]::GetDirectoryName($currentRelativePath) -replace '\\', '/').TrimStart('./'))) | Out-Null
    }

    # A grouped workflow lists a dependency that is itself one of its own projects; that path is
    # already covered by the project paths, so drop it to avoid emitting a duplicate filter.
    $dependencyPaths = [System.Environment]::NewLine
    foreach ($dependency in $dependencies) {
        $dependencyRelativeDirectory = ([System.IO.Path]::GetDirectoryName($dependency) -replace '\\', '/').TrimStart('./')
        if ($projectDirectories.Contains($dependencyRelativeDirectory)) {
            continue
        }
        $dependencyPaths += "    - '$dependencyRelativeDirectory/**/*'" + [System.Environment]::NewLine
    }

    $projectPathFilters = ''
    foreach ($projectDirectory in $projectDirectories) {
        $projectPathFilters += "    - '$projectDirectory/**/*'" + [System.Environment]::NewLine
    }
    # Trim the trailing newline; the template supplies the line break that follows.
    $projectPathFilters = $projectPathFilters.TrimEnd([System.Environment]::NewLine.ToCharArray())

    [bool]$isCLI = if ($projectNames -contains "Lucene.Net.Tests.Cli") { $true } else { $false }        # Special case
    $luceneCliProjectPath = $projectRelativePaths[0] -replace "Lucene.Net.Tests.Cli", "lucene-cli"      # Special case

    [string]$frameworks = '[' + $($TestFrameworks -join ', ') + ']'
    [string]$platforms = '[' + $($TestPlatforms -join ', ') + ']'
    [string]$oses = '[' + $($OperatingSystems -join ', ') + ']'
    [string]$configurations = '[' + $($Configurations -join ', ') + ']'

    $directoryBuildPaths = [System.Environment]::NewLine
    foreach ($directory in $directories) {
        $relativeDirectory = ([System.IO.Path]::Combine($directory, 'Directory.Build.*') -replace '\\', '/').TrimStart('./')
        $directoryBuildPaths += "    - '$relativeDirectory'" + [System.Environment]::NewLine
    }

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

name: '$WorkflowName'

on:
  workflow_dispatch:
  pull_request:
    paths:
$projectPathFilters
    - '.build/dependencies.props'
    - '.build/TestReferences.Common.*'
    - 'TestTargetFrameworks.*'
    - '.github/**/*.yml'
    - '*.sln'$directoryBuildPaths
    # Dependencies$dependencyPaths
    - '!**/*.md'

jobs:

  Test:
    runs-on: `${{ matrix.os }}
    # Without this, a job inherits the GitHub default of 360 minutes. The slowest job
    # observed takes well under 30 minutes, grouped ones included, so this is a cap on a
    # stuck job rather than a budget to run within.
    timeout-minutes: 30
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
            framework: net472
          - os: macos-latest
            framework: net48
          - os: macos-latest
            framework: net472
    env:
      DOTNET_CLI_TELEMETRY_OPTOUT: 1
      DOTNET_NOLOGO: 1
      NUGET_PACKAGES: `${{ github.workspace }}/.nuget/packages
      BUILD_FOR_ALL_TEST_TARGET_FRAMEWORKS: 'true'"

    # A grouped workflow builds and tests each project in its own step, so there is no single
    # project_path for the job; each step passes its own path instead.
    if ($projectRelativePaths.Count -eq 1) {
        $fileText += "
      project_path: '$($projectRelativePaths[0])'"
    }

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
        uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Setup .NET 8 SDK
        uses: actions/setup-dotnet@9a946fdbd5fb07b82b2f5a4466058b876ab72bb2 # v5.3.0
        with:
          dotnet-version: '$DotNet8SDKVersion'
        if: `${{ startswith(matrix.framework, 'net8.') }}

      - name: Setup .NET 10 SDK
        uses: actions/setup-dotnet@9a946fdbd5fb07b82b2f5a4466058b876ab72bb2 # v5.3.0
        with:
          dotnet-version: '$DotNet10SDKVersion'

      - name: Cache NuGet Packages
        uses: actions/cache@27d5ce7f107fe9357f9df03efb73ab90386fccae # v5.0.5
        with:
          # '**/*.*proj' includes .csproj, .vbproj, .fsproj, msbuildproj, etc.
          # '**/*.props' includes Directory.Packages.props, Directory.Build.props and Dependencies.props
          # '**/*.targets' includes Directory.Build.targets
          # '**/*.sln' and '*.sln' ensure root solution files are included (minimatch glitch for file extension .sln)
          # 'global.json' included for SDK version changes
          key: nuget-`${{ runner.os }}-`${{ env.BUILD_FOR_ALL_TEST_TARGET_FRAMEWORKS }}-`${{ hashFiles('**/*.*proj', '**/*.props', '**/*.targets', '**/*.sln', '*.sln', 'global.json') }}
          path: `${{ env.NUGET_PACKAGES }}

      - name: Restore
        run: dotnet restore /p:TestFrameworks=`${{ env.BUILD_FOR_ALL_TEST_TARGET_FRAMEWORKS }}

      - name: Setup Environment Variables
        run: |"

    # In a grouped workflow the project name and report title vary per project, so they are set
    # by each project's own test step rather than once for the job.
    if ($projectRelativePaths.Count -eq 1) {
        $fileText += "
          `$project_name = [System.IO.Path]::GetFileNameWithoutExtension(`$env:project_path)"
    }

    $fileText += "
          `$test_results_artifact_name = `"testresults_`${{matrix.os}}_`${{matrix.framework}}_`${{matrix.platform}}_`${{matrix.configuration}}`"
          `$working_directory = `"`$env:GITHUB_WORKSPACE`""

    if ($projectRelativePaths.Count -eq 1) {
        $fileText += "
          Write-Host `"Project Name: `$project_name`""
    }

    $fileText += "
          Write-Host `"Results Artifact Name: `$test_results_artifact_name`"
          Write-Host `"Working Directory: `$working_directory`""

    if ($projectRelativePaths.Count -eq 1) {
        $fileText += "
          echo `"project_name=`$project_name`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append"
    }

    $fileText += "
          echo `"test_results_artifact_name=`$test_results_artifact_name`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append
          # Set the Azure DevOps default working directory env variable, so our tests only need to deal with a single env variable
          echo `"SYSTEM_DEFAULTWORKINGDIRECTORY=`$working_directory`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append"

    if ($projectRelativePaths.Count -eq 1) {
        $fileText += "
          # Title for LiquidTestReports.Markdown
          echo `"title=Test Results for `$project_name - `${{matrix.framework}} - `${{matrix.platform}} - `${{matrix.os}}`" | Out-File -FilePath  `$env:GITHUB_ENV -Encoding utf8 -Append"
    }

    $fileText += "
        shell: pwsh"

    if ($isCLI) {
        # Special case: Generate lucene-cli.nupkg for installation test so the test runner doesn't have to do it
        $fileText += "
      - run: dotnet pack `"`${{env.project_under_test_path}}`" --configuration `"`${{matrix.configuration}}`" --no-restore -p:TestFrameworks=`${{ env.BUILD_FOR_ALL_TEST_TARGET_FRAMEWORKS }} -p:PortableDebugTypeOnly=true
        shell: bash"
    }

    if ($projectRelativePaths.Count -eq 1) {
        $fileText += "
      - run: dotnet build `"`${{env.project_path}}`" --configuration `"`${{matrix.configuration}}`" --framework `"`${{matrix.framework}}`" --no-restore -p:TestFrameworks=`${{ env.BUILD_FOR_ALL_TEST_TARGET_FRAMEWORKS }}
        shell: bash
      - run: dotnet test `"`${{env.project_path}}`" --configuration `"`${{matrix.configuration}}`" --framework `"`${{matrix.framework}}`" --no-build --no-restore --blame-hang --blame-hang-dump-type mini --blame-hang-timeout 20minutes --logger:`"console;verbosity=normal`" --logger:`"trx;LogFileName=`${{env.trx_file_name}}`" --logger:`"liquid.md;LogFileName=`${{env.md_file_name}};Title=`${{env.title}};`" --results-directory:`"`${{github.workspace}}/`${{env.test_results_artifact_name}}/`${{env.project_name}}`" -- RunConfiguration.TargetPlatform=`${{matrix.platform}} NUnit.DisplayName=FullName TestRunParameters.Parameter\(name=\`"tests:slow\`",\ value=\`"\`${{env.run_slow_tests}}\`"\)
        shell: bash"
    } else {
        # Each project in the group gets its own build and test step. Results go into a
        # per-project subdirectory of the shared artifact, matching the layout the
        # single-project workflows produce.
        #
        # Every step after the first uses always(), so that a failure in one project still
        # builds and tests the projects after it; a grouped workflow stays as informative as
        # the separate workflows it replaces. Each test step additionally requires its own
        # build to have succeeded: running 'dotnet test --no-build' after a failed compile
        # reports a missing or stale test assembly rather than the build error, which buries
        # the real cause. The job still fails, because the build step itself failed.
        for ($i = 0; $i -lt $projectRelativePaths.Count; $i++) {
            $currentRelativePath = $projectRelativePaths[$i]
            $currentName = $projectNames[$i]
            # Step ids may only contain alphanumerics, '-' and '_'.
            $buildStepId = 'build_' + ($currentName -replace '[^A-Za-z0-9_]', '_')
            $buildStepCondition = if ($i -eq 0) { '' } else { "
        if: `${{always()}}" }

            $fileText += "

      # $currentName
      - name: Build $currentName
        id: $buildStepId
        run: dotnet build `"$currentRelativePath`" --configuration `"`${{matrix.configuration}}`" --framework `"`${{matrix.framework}}`" --no-restore -p:TestFrameworks=`${{ env.BUILD_FOR_ALL_TEST_TARGET_FRAMEWORKS }}
        shell: bash$buildStepCondition
      - name: Test $currentName
        run: dotnet test `"$currentRelativePath`" --configuration `"`${{matrix.configuration}}`" --framework `"`${{matrix.framework}}`" --no-build --no-restore --blame-hang --blame-hang-dump-type mini --blame-hang-timeout 20minutes --logger:`"console;verbosity=normal`" --logger:`"trx;LogFileName=`${{env.trx_file_name}}`" --logger:`"liquid.md;LogFileName=`${{env.md_file_name}};Title=Test Results for $currentName - `${{matrix.framework}} - `${{matrix.platform}} - `${{matrix.os}};`" --results-directory:`"`${{github.workspace}}/`${{env.test_results_artifact_name}}/$currentName`" -- RunConfiguration.TargetPlatform=`${{matrix.platform}} NUnit.DisplayName=FullName TestRunParameters.Parameter\(name=\`"tests:slow\`",\ value=\`"\`${{env.run_slow_tests}}\`"\)
        shell: bash
        if: `${{always() && steps.$buildStepId.outcome == 'success'}}"
        }
    }

    $fileText += "
      # upload reports as build artifacts
      - name: Upload a Build Artifact
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        if: `${{always()}}
        with:
          name: '`${{env.test_results_artifact_name}}'
          path: '`${{github.workspace}}/`${{env.test_results_artifact_name}}'
      - name: Output Test Summary
        if: `${{always()}}
        shell: pwsh
        run: |"

    if ($projectRelativePaths.Count -eq 1) {
        $fileText += "
          `$md_file = Join-Path `${{github.workspace}} `${{env.test_results_artifact_name}} `${{env.project_name}} `${{env.md_file_name}}
          if (Test-Path `$md_file) {
              Get-Content `$md_file | Add-Content `$env:GITHUB_STEP_SUMMARY
          }
"
    } else {
        # A grouped workflow produces one report per project; append each one that exists.
        $fileText += "
          `$project_names = @('$($projectNames -join "', '")')
          foreach (`$project_name in `$project_names) {
              `$md_file = Join-Path `${{github.workspace}} `${{env.test_results_artifact_name}} `$project_name `${{env.md_file_name}}
              if (Test-Path `$md_file) {
                  Get-Content `$md_file | Add-Content `$env:GITHUB_STEP_SUMMARY
              }
          }
"
    }

    # GitHub Actions does not support filenames with "." in them, so replace
    # with "-"
    $projectFileName = $WorkflowName -replace '\.', '-'
    $FilePath = "$OutputDirectory/$projectFileName.yml"

    #$dir = [System.IO.Path]::GetDirectoryName($File)
    Ensure-Directory-Exists $OutputDirectory

    Write-Host "Generating workflow file: $FilePath"

    # Ensure the file does not get generated with a BOM
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($FilePath, $fileText, $utf8NoBom)

    #Write-Host $fileText
}


Push-Location $RelativeRoot
try {
    [string[]]$TestProjects = Get-ChildItem -Path "$RepoRoot/**/*.csproj" -Recurse | where { $_.Directory.Name.Contains(".Tests") -and !($_.Directory.FullName.Contains('svn-')) } | Select-Object -ExpandProperty FullName
} finally {
    Pop-Location
}

#Write-TestWorkflow -OutputDirectory $OutputDirectory -ProjectPath $projectPath -RelativeRoot $repoRoot -TestFrameworks @('net6.0') -OperatingSystems $OperatingSystems -TestPlatforms $TestPlatforms -Configurations $Configurations -DotNet8SDKVersion $DotNet8SDKVersion

#Write-Host $TestProjects

# First pass: resolve the target frameworks for each project and assign it to a workflow.
# Grouped projects are collected so the whole group can be emitted as one file afterwards.
$workflowProjects = [ordered]@{}     # workflow name -> ordered list of project paths
$workflowFrameworks = @{}            # workflow name -> frameworks shared by the whole group
$groupedWorkflows = New-Object System.Collections.Generic.HashSet[string] # workflow names set by TestShard
$unassignedProjects = New-Object System.Collections.Generic.List[string]  # projects setting neither property
$errors = New-Object System.Collections.Generic.List[string]

foreach ($testProject in $TestProjects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)

     # Call the target to get the configured test frameworks for this project.
    $frameworksString = Get-SupportedTargetFrameworksString $testProject

    if ($frameworksString -eq 'none') {
        Write-Host "WARNING: Skipping project '$projectName' because it is not marked with `<IsTestProject`>true`<`/IsTestProject`> and/or it contains no test frameworks for the current environment." -ForegroundColor Yellow
        continue
    }

    [string[]]$frameworks = $frameworksString -split '\s*;\s*'
    # @(...) keeps this an array when only one framework matches, so that the Count check
    # below and the group comparison further down behave the same for one or many.
    $frameworks = @($frameworks | ? { $TestFrameworks -contains $_ }) # IntersectWith

    if ($frameworks.Count -eq 0) {
        Write-Host "WARNING: ${projectName} contains no matching target frameworks: $frameworksString" -ForegroundColor Yellow
        continue
    }

    Write-Host ""
    Write-Host "Frameworks To Test for ${projectName}: $($frameworks -join ';')" -ForegroundColor Cyan

    # Every project must choose a group or isolation explicitly. Errors are collected rather
    # than thrown here so that a single run reports every project that needs fixing.
    $shardProperties = Get-TestShardProperties $testProject
    $testShard = $shardProperties.TestShard
    $isolated = $shardProperties.TestShardIsolated

    if (-not [string]::IsNullOrEmpty($isolated) -and $isolated -ne 'true') {
        $errors.Add("${projectName} sets TestShardIsolated to '${isolated}'. The only supported value is 'true'; remove the property to join a group with TestShard instead.")
        continue
    }
    if ([string]::IsNullOrEmpty($testShard) -and [string]::IsNullOrEmpty($isolated)) {
        $unassignedProjects.Add($projectName)
        continue
    }
    if (-not [string]::IsNullOrEmpty($testShard) -and -not [string]::IsNullOrEmpty($isolated)) {
        $errors.Add("${projectName} sets both TestShard ('${testShard}') and TestShardIsolated. Remove one of them.")
        continue
    }

    $workflowName = if ($isolated -eq 'true') { $projectName } else { "Lucene.Net.Tests.$testShard" }
    if ($isolated -ne 'true') {
        [void]$groupedWorkflows.Add($workflowName)
    }

    if (-not $workflowProjects.Contains($workflowName)) {
        $workflowProjects[$workflowName] = New-Object System.Collections.Generic.List[string]
        $workflowFrameworks[$workflowName] = $frameworks
    } elseif (@(Compare-Object $workflowFrameworks[$workflowName] $frameworks).Count -ne 0) {
        # All projects sharing a workflow share a single build matrix, so differing frameworks
        # would silently drop coverage for the frameworks that are not shared by every member.
        $errors.Add("${projectName} targets '$($frameworks -join ';')', which differs from the other projects in workflow '${workflowName}' ('$($workflowFrameworks[$workflowName] -join ';')'). Projects in a group share a single build matrix, so this would drop coverage. Move it to a group with matching frameworks or set <TestShardIsolated>true</TestShardIsolated>.")
        continue
    }

    $workflowProjects[$workflowName].Add($testProject)
}

# A TestShard value that no other project shares is most likely a typo, which would quietly
# split the project out of the group it was meant to join. Projects meant to run alone should
# say so with TestShardIsolated.
foreach ($workflowName in $groupedWorkflows) {
    $projectPaths = @($workflowProjects[$workflowName])
    if ($projectPaths.Count -eq 1) {
        $errors.Add("Workflow '${workflowName}' contains only $([System.IO.Path]::GetFileNameWithoutExtension($projectPaths[0])). Check that its TestShard value matches the other projects in the group, or set <TestShardIsolated>true</TestShardIsolated> instead.")
    }
}

if ($unassignedProjects.Count -gt 0) {
    $existingGroups = ($groupedWorkflows | ForEach-Object { $_ -replace '^Lucene\.Net\.Tests\.', '' } | Sort-Object) -join ', '
    foreach ($projectName in $unassignedProjects) {
        $errors.Add("${projectName} sets neither <TestShard> nor <TestShardIsolated>true</TestShardIsolated>. Add one of them to its .csproj file. Existing groups: ${existingGroups}.")
    }
}

if ($errors.Count -gt 0) {
    $nl = [System.Environment]::NewLine
    throw "Cannot generate test workflows. Fix the following $($errors.Count) problem(s) and run this script again:$nl$(($errors | ForEach-Object { " - $_" }) -join $nl)"
}

# Second pass: emit one workflow file per group and per isolated project.
foreach ($workflowName in $workflowProjects.Keys) {
    $projectPaths = @($workflowProjects[$workflowName])
    $frameworks = $workflowFrameworks[$workflowName]

    if ($projectPaths.Count -gt 1) {
        Write-Host ""
        Write-Host "Grouping $($projectPaths.Count) projects into workflow '${workflowName}': $(($projectPaths | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) }) -join ', ')" -ForegroundColor Green
    }

    Write-TestWorkflow -OutputDirectory $OutputDirectory -ProjectPaths $projectPaths -WorkflowName $workflowName -RelativeRoot $RepoRoot -TestFrameworks $frameworks -OperatingSystems $OperatingSystems -TestPlatforms $TestPlatforms -Configurations $Configurations -DotNet8SDKVersion $DotNet8SDKVersion
}
