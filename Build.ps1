<#
.SYNOPSIS
    Builds, runs, packages and uploads packages for Lucenen.NET's .NET Core libraries

.PARAMETER NuGetSource
    URI to upload NuGet packages to. Required for uploading NuGet packages
.PARAMETER NuGetApiKey
    API Key used to upload package to NuGet source.  Required for uploading NuGet packages

.PARAMETER CreatePackages
    Create NuGet packages
.PARAMETER UploadPackages
    Upload NuGet packages
.PARAMETER RunTests
    Run all test libraries

.PARAMETER Configuration
    Runs scripts with either Debug or Release configuration

.PARAMETER ProjectsToTest
    An array of project names to test. (ie. @("Lucene.Net.Tests", "Lucene.Net.Tests.Codecs"))
.PARAMETER ExcludeTestCategories
    An array of test categories to exclude in test runs. Default is LongRunningTest
.PARAMETER ExcludeTestCategoriesNetCore
    An array of test categories to exclude in test runs when running against .NET Core.
    Default is LongRunningTest, DtdProcessingTest, HasTimeout

.PARAMETER FrameworksToTest
    An array of frameworks to run tests against. Default is "net451" and "netcoreapp1.0"

.PARAMETER Quiet
    Silence output.  Useful for piping Test output into a log file instead of to console.

.PARAMETER TestResultsDirectory
    Directory for NUnit TestResults.  Default is $PSScriptRoot\TestResults
.PARAMETER NuGetPackageDirectory
    Directory for generated NuGet packages.  Default is $PSScriptRoot\NuGetPackages

.EXAMPLE
    Build.ps1 -Configuration "Debug" -RunTests -Quiet

    Build all .NET Core projects as Debug and run all tests. Tests are run
    against "net451" and "netcoreapp1.0" frameworks and excludes
    "LongRunningTests".  All output for tests is piped into an output.log and
    then placed in the $TestResultsDirectory.
.EXAMPLE
    Build.ps1 -CreatePackages

    Creates NuGet packages for .NET Core projects compiled as Release.
.EXAMPLE
    Build.ps1 "http://myget.org/conniey/F/lucenenet-feed" "0000-0000-0000"

    Creates and uploads NuGet packages for .NET Core projects compiled as
    Release. Uploads projects to "http://myget.org/conniey/F/lucenenet-feed".
.EXAMPLE
    Build.ps1 -RunTests -ExcludeTestCategoriesNetCore @("DtdProcessingTest", "LongRunningTest") -FrameworksToTest @("netcoreapp1.0")

    Build all .NET Core projects as Release and run all tests. Tests are run
    against "netcoreapp1.0" frameworks and excludes "DtdProcessingTest" and
    "LongRunningTests".

.EXAMPLE
    Build.ps1 -ProjectsToTest @("Lucene.Net.Tests") -RunTests

    Builds all .NET Core projects as Release and runs the test project Lucene.Net.Tests.
#>

[CmdletBinding(DefaultParameterSetName="Default")]
param(
    [Parameter(Mandatory = $true, Position = 0, ParameterSetName="UploadPackages")]
    [string]$NuGetSource,
    [Parameter(Mandatory = $true, Position = 1, ParameterSetName="UploadPackages")]
    [string]$NuGetApiKey,

    [Parameter(Mandatory = $true, ParameterSetName="CreatePackages")]
    [switch]$CreatePackages,
    [Parameter(Mandatory = $true, ParameterSetName="UploadPackages")]
    [switch]$UploadPackages,
    [switch]$RunTests,
    
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [string[]]$ProjectsToTest,
    [string[]]$ExcludeTestCategories = @("LongRunningTest"),
    [string[]]$ExcludeTestCategoriesNetCore = @("LongRunningTest", "DtdProcessingTest", "HasTimeout"),
    [string[]]$FrameworksToTest = @("netcoreapp1.0"),
    
    [switch]$Quiet,
    [string]$TestResultsDirectory,
    [string]$NuGetPackageDirectory
)

$root = $PSScriptRoot
$defaultNugetPackageDirectory = Join-Path $root "NuGetPackages"
$defaultTestResultsDirectory = Join-Path $root "TestResults"

if ([string]::IsNullOrEmpty($NuGetPackageDirectory)) {
    $NuGetPackageDirectory = $defaultNugetPackageDirectory
}

function Compile-Projects($projects) {

    foreach ($project in $projects) {
        pushd $project.DirectoryName

        & dotnet.exe build --configuration $Configuration

        popd
    }
}

function Generate-ExcludeCategoryString ($categories) {
    $contents = ""

    if ($categories.Count -gt 0) {
        foreach ($category in $categories) {
            $formatted = [String]::Format("Category!={0}", $category);

            if ([string]::IsNullOrEmpty($contents)) {
                $contents = "--where=""$formatted"
            } else {
                $contents += " && $formatted"
            }
        }

        $contents += '"'
    }

    return $contents
}

function Test-Projects($projects) {
    if ([string]::IsNullOrEmpty($TestResultsDirectory)) {
        $TestResultsDirectory = $defaultTestResultsDirectory
    }

    if (Test-Path $TestResultsDirectory) {
        Write-Host "Removing old test results..."

        Remove-Item $TestResultsDirectory -Recurse -Force
    }

    New-Item $TestResultsDirectory -ItemType Directory | Out-Null
    
    # Setting the preference so that we can run all the tests regardless of
    # errors that may happen.
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    # Generate the string to exclude categories from being tested
    $excludeCategories = Generate-ExcludeCategoryString $ExcludeTestCategories
    $excludeCategoriesNetCoreApp = Generate-ExcludeCategoryString $ExcludeTestCategoriesNetCore

    foreach ($project in $projects) {
        
        pushd $project.DirectoryName

        $testName = $project.Directory.Name
        $testFolder = Join-Path $TestResultsDirectory $testName

        New-Item $testFolder -ItemType Directory | Out-Null

        foreach ($framework in $FrameworksToTest) {
            Write-Host "Testing [$testName] on [$framework]..."
            
            $testResult = "TestResult.$framework.xml"

            if ($framework.StartsWith("netcore")) {
                $testExpression = "dotnet.exe test --configuration $Configuration --framework $framework --no-build $excludeCategoriesNetCoreApp"
            } else {
                $testExpression = "dotnet.exe test --configuration $Configuration --framework $framework --no-build $excludeCategories"
            }

            Write-Host $testExpression
            
            if ($Quiet) {
                $outputLog = "output.$framework.log"

                Invoke-Expression $testExpression | Set-Content $outputLog
                Move-Item $outputLog $testFolder\
            } else {
                Invoke-Expression $testExpression
            }

            if (Test-Path ".\TestResult.xml") {
                Move-Item ".\TestResult.xml" $(Join-Path $testFolder $testResult)
            } else {
                Write-Warning "Could not find TestResult.xml."
            }
        }

        popd
    }

    $ErrorActionPreference = $oldPreference
}

function Create-NuGetPackages($projects) {

    if (!(Test-Path $NuGetPackageDirectory)) {
        New-Item $NuGetPackageDirectory -ItemType Directory
    }
    
    foreach ($project in $projects) {
        pushd $project.DirectoryName

        & dotnet.exe pack --configuration $Configuration --output $NuGetPackageDirectory --no-build

        popd
    }

    return $NuGetPackageDirectory
}

function Upload-NuGetPackages {
    $NuGetExe = & "$root\lib\Nuget\Get-NuGet.ps1"

    $packagesToUpload = Get-ChildItem $NuGetPackageDirectory | ? { $_.Extension.Equals(".nupkg") -and !$_.BaseName.Contains(".symbols") }

    foreach ($package in $packagesToUpload) {

        Write-Host "Uploading $($package)..."

        Invoke-Expression "$NuGetExe push $($package.FullName) -ApiKey $NuGetApiKey -Source $NuGetSource"
    }
}

& where.exe dotnet.exe

if ($LASTEXITCODE -ne 0) {
    Write-Error "Could not find .NET CLI in PATH. Please install it."
}

# Stopping script if any errors occur
$ErrorActionPreference = "Stop"

& dotnet.exe restore

$projectJsons = Get-ChildItem $root\project.json -Recurse

Compile-Projects $projectJsons

if ($RunTests) {
    Write-Host "Running tests..."

    if ($ProjectsToTest -ne $null -and $ProjectsToTest.Count -gt 0) {
        $testProjects = $projectJsons | ? { $ProjectsToTest.Contains($_.Directory.Name) }

        if (@($testProjects).Count -eq 0) {
            Write-Warning "Could not find any test projects matching the given ProjectsToTest. No tests run."
        }
    } else {
        $testProjects = $projectJsons | ? { $_.Directory.Name.Contains(".Tests") }
    }

    Test-Projects $testProjects
}

if ($CreatePackages -or $UploadPackages) {
    Write-Host "Creating NuGet packages..."

    $projectsToPackage = $projectJsons | ? { !$_.Directory.Name.Contains(".Test") }
    Create-NuGetPackages $projectsToPackage
}

if ($UploadPackages) {
    
    Write-Host "Uploading NuGet packages..."

    Upload-NuGetPackages
}