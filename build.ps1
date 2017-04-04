<#
.SYNOPSIS
    Builds, runs, packages and uploads packages for Lucene.NET's .NET Core libraries

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
    Default is LongRunningTest, HasTimeout

.PARAMETER FrameworksToTest
    An array of frameworks to run tests against. Default is "net451" and "netcoreapp1.0"

.PARAMETER Quiet
    Silence output.  Useful for piping Test output into a log file instead of to console.

.PARAMETER TestResultsDirectory
    Directory for NUnit TestResults.  Default is $PSScriptRoot\release\TestResults
.PARAMETER NuGetPackageDirectory
    Directory for generated NuGet packages.  Default is $PSScriptRoot\release\NuGetPackages

.PARAMETER Version
	Version of the assembly (no pre-release tag). Default is 0.0.0 (indicating to parse the value from PackageVersion).
.PARAMETER PackageVersion
	Version of the NuGet Package (including the pre-release tag). Default is 4.8.0.

.PARAMETER AssemblyInfoFile
	Path to the common assembly info file. Default is PSScriptRoot\src\CommonAssemblyInfo.cs
.PARAMETER CopyrightYear
	The end year that will be on the copyright. Default is the current year on the system.
.PARAMETER Copyright
	The copyright message that will be applied to AssemblyCopyrightAttribute and AssemblyTrademarkAttribute.
	The default is "Copyright 2006 - $CopyrightYear The Apache Software Foundation".
.PARAMETER ProductName
	The value that will be used for the ProductNameAttribute. Default is "Lucene.Net".

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
    Build.ps1 -RunTests -ExcludeTestCategoriesNetCore @("HasTimeout", "LongRunningTest") -FrameworksToTest @("netcoreapp1.0")

    Build all .NET Core projects as Release and run all tests. Tests are run
    against "netcoreapp1.0" frameworks and excludes "HasTimeout" and
    "LongRunningTest".

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
    [string[]]$ExcludeTestCategoriesNetCore = @("LongRunningTest", "HasTimeout"),
    [string[]]$FrameworksToTest = @("netcoreapp1.0"),
    
    [switch]$Quiet,
    [string]$TestResultsDirectory,
    [string]$NuGetPackageDirectory,
	
	[string]$PackageVersion = "4.8.0",
	[string]$Version = "0.0.0",

	[string]$AssemblyInfoFile = "$PSScriptRoot\src\CommonAssemblyInfo.cs",
	[string]$CopyrightYear = [DateTime]::Today.Year.ToString(), #Get the current year from the system
	[string]$Copyright = "Copyright 2006 - $CopyrightYear The Apache Software Foundation",
	[string]$CompanyName = "The Apache Software Foundation",
	[string]$ProductName = "Lucene.Net"
)

#Get the current working directory
$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

$root = $PSScriptRoot

#Release directory for all build assets
$ReleaseDirectory = Join-Path $root "release"
$defaultNugetPackageDirectory = Join-Path $ReleaseDirectory "NuGetPackages"
$defaultTestResultsDirectory = Join-Path $ReleaseDirectory "TestResults"

if ([string]::IsNullOrEmpty($NuGetPackageDirectory)) {
    $NuGetPackageDirectory = $defaultNugetPackageDirectory
}

if ([string]::IsNullOrEmpty($TestResultsDirectory)) {
	$TestResultsDirectory = $defaultTestResultsDirectory
}

#If version is not passed in, parse it from $PackageVersion
if ($Version -eq "0.0.0" -or [string]::IsNullOrEmpty($Version)) {
	$Version = $PackageVersion
	if ($Version.Contains("-") -eq $true) {
		$Version = $Version.SubString(0, $Version.IndexOf("-"))
	}
}

function Ensure-Directory-Exists([string] $path)
{
	if (!(Test-Path $path)) {
		New-Item $path -ItemType Directory
	}
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
	$AssemblyVersion = $Matches[0]
	$AssemblyVersion = "$AssemblyVersion.0.0"

  $asmInfo = "using System;
using System.Reflection;

[assembly: AssemblyProduct(""$product"")]
[assembly: AssemblyCompany(""$company"")]
[assembly: AssemblyTrademark(""$copyright"")]
[assembly: AssemblyCopyright(""$copyright"")]
[assembly: AssemblyVersion(""$AssemblyVersion"")] 
[assembly: AssemblyFileVersion(""$version"")]
[assembly: AssemblyInformationalVersion(""$packageVersion"")]
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	Ensure-Directory-Exists $dir

	Write-Host "Generating assembly info file: $file"
	Out-File -filePath $file -encoding UTF8 -inputObject $asmInfo
}

function Backup-Assembly-Info() {
	Move-Item $AssemblyInfoFile "$AssemblyInfoFile.bak" -Force
}

function Restore-Assembly-Info() {
	Move-Item "$AssemblyInfoFile.bak" $AssemblyInfoFile -Force
}


if (Test-Path $ReleaseDirectory) {
	Write-Host "Removing old build assets..."

	Remove-Item $ReleaseDirectory -Recurse -Force
}
Ensure-Directory-Exists $ReleaseDirectory

function Compile-Projects($projects) {

	try {
		Backup-Assembly-Info 
		
		Generate-Assembly-Info `
			-product $ProductName `
			-company $CompanyName `
			-copyright $Copyright `
			-version $Version `
			-packageVersion $PackageVersion `
			-file $AssemblyInfoFile
	
		foreach ($project in $projects) {
			pushd $project.DirectoryName

			& dotnet.exe build --configuration $Configuration

			popd
		}
	} finally {
		Restore-Assembly-Info
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
    
    if (Test-Path $TestResultsDirectory) {
        Write-Host "Removing old test results..."

        Remove-Item $TestResultsDirectory -Recurse -Force
    }

	Ensure-Directory-Exists $TestResultsDirectory
    
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

# Gets the description from the AssemblyDescriptionAttribute
function Get-Assembly-Description($project) {
	#project path has a project.json file, we need the path without it
	$dir = [System.IO.Path]::GetDirectoryName($project).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
	$projectName = [System.IO.Path]::GetFileName($dir)
	$projectAssemblyPath = "$dir\bin\$Configuration\net451\$projectName.dll"

	$assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($projectAssemblyPath)
	$descriptionAttributes = [reflection.customattributedata]::GetCustomAttributes($assembly) | Where-Object {$_.AttributeType -like "System.Reflection.AssemblyDescriptionAttribute"}

	if ($descriptionAttributes.Length -gt 0) {
		$descriptionAttributes[0].ToString()-match "(?<=\[System.Reflection.AssemblyDescriptionAttribute\("")([^""]*)" | Out-Null
		return $Matches[0]
	}
}

function Create-NuGetPackages($projects) {
	
	if (Test-Path $NuGetPackageDirectory) {
        Write-Host "Removing old NuGet packages..."

        Remove-Item $NuGetPackageDirectory -Recurse -Force
    }

	Ensure-Directory-Exists $NuGetPackageDirectory

	foreach ($project in $projects) {
		pushd $project.DirectoryName

		# Update the packOptions.summary with the value from AssemblyDescriptionAttribute
		$assemblyDescription = Get-Assembly-Description $project
		Write-Host "Updating package description with '$assemblyDescription'" -ForegroundColor Yellow

		(Get-Content $project) | % {
			$_-replace "(?<=""summary""\s*?:\s*?"")([^""]*)", $assemblyDescription
		} | Set-Content $project -Force

		Write-Host "Creating NuGet package for $project..." -ForegroundColor Magenta
			
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

$projectJsons = Get-ChildItem -Path "project.json" -Recurse

try {

	foreach ($projectJson in $projectJsons) {
		#Backup the project.json file
		Copy-Item $projectJson "$projectJson.bak" -Force

		#Update version (for NuGet package) and dependency version of this project's inter-dependencies
		(Get-Content $projectJson) | % {
			$_-replace "(?<=""Lucene.Net[\w\.]*?""\s*?:\s*?"")([^""]+)", $PackageVersion
		} | Set-Content $projectJson -Force

		$json = (Get-Content $projectJson) | ConvertFrom-Json
		$json.version = $PackageVersion
		$json | ConvertTo-Json -depth 100 | Out-File $projectJson -encoding UTF8 -Force
	}

	& dotnet.exe restore

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

} finally {
	#Restore the project.json files
	foreach ($projectJson in $projectJsons) {
		Move-Item "$projectJson.bak" $projectJson -Force
	}
}

if ($UploadPackages) {
    
    Write-Host "Uploading NuGet packages..."

    Upload-NuGetPackages
}

