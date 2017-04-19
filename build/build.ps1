 properties {
	[string]$base_directory   = Resolve-Path "..\."
	[string]$release_directory  = "$base_directory\release"
	[string]$source_directory = "$base_directory"
	[string]$tools_directory  = "$base_directory\lib"
	[string]$nuget_package_directory = "$release_directory\NuGetPackages"
	[string]$test_results_directory = "$release_directory\TestResults"

	[string]$packageVersion   = "4.8.0"
	[string]$version          = "0.0.0"
	[string]$configuration    = "Release"
	[bool]$backup_files        = $true

	[string]$common_assembly_info = "$base_directory\src\CommonAssemblyInfo.cs"
	[string]$copyright_year = [DateTime]::Today.Year.ToString() #Get the current year from the system
	[string]$copyright = "Copyright " + $([char]0x00A9) + " 2006 - $copyright_year The Apache Software Foundation"
	[string]$company_name = "The Apache Software Foundation"
	[string]$product_name = "Lucene.Net"
	
	#test paramters
	[string]$frameworks_to_test = "net451,netcoreapp1.0"
	[string]$where = ""
}

$backedUpFiles = New-Object System.Collections.ArrayList

task default -depends Pack

task Clean -description "This task cleans up the build directory" {
	Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
}

task Init -description "This task makes sure the build environment is correctly setup" {
	& where.exe dotnet.exe

	if ($LASTEXITCODE -ne 0) {
		Write-Error "Could not find dotnet CLI in PATH. Please install the .NET Core 1.1 SDK."
	}

	& dotnet.exe --version
	Write-Host "Base Directory: $base_directory"
	Write-Host "Release Directory: $release_directory"
	Write-Host "Source Directory: $source_directory"
	Write-Host "Tools Directory: $tools_directory"
	Write-Host "NuGet Package Directory: $nuget_package_directory"
	Write-Host "Version: $version"
	Write-Host "Package Version: $packageVersion"
	Write-Host "Configuration: $configuration"

	#If version is not passed in, parse it from $packageVersion
	if ($version -eq "0.0.0" -or [string]::IsNullOrEmpty($version)) {
		$version = $packageVersion
		if ($version.Contains("-") -eq $true) {
			$version = $version.SubString(0, $version.IndexOf("-"))
		}
		Write-Host "Updated version to: $version"
	}

	Ensure-Directory-Exists "$release_directory"

	#ensure we have the latest version of NuGet
	exec {
		&"$tools_directory\nuget\nuget.exe" update -self
	} -ErrorAction SilentlyContinue
}

task Compile -depends Clean, Init -description "This task compiles the solution" {
	try {
		pushd $base_directory
		$projects = Get-ChildItem -Path "project.json" -Recurse
		popd

		Backup-Files $projects
		Prepare-For-Build $projects
		& dotnet.exe restore $base_directory

		Build-Assemblies $projects

		Start-Sleep 10

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
		$packages = Get-ChildItem -Path "project.json" -Recurse | ? { !$_.Directory.Name.Contains(".Test") }
		popd

		Pack-Assemblies $packages

		$success = $true
	} finally {
		#if ($success -ne $true) {
			Restore-Files $backedUpFiles
		#}
	}
}

task Test -description "This task runs the tests" {
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

			#$testResultDirectory = "$test_results_directory\$framework\$testName"
			#Ensure-Directory-Exists $testResultDirectory

			$testExpression = "$testExpression --result:$projectDirectory\TestResult.xml"

			if ($where -ne $null -and (-Not [System.String]::IsNullOrEmpty($where))) {
				$testExpression = "$testExpression --where $where"
			}

			Write-Host $testExpression -ForegroundColor Magenta

			Invoke-Expression $testExpression
		}
	}
}

function Prepare-For-Build([string[]]$projects) {
	Backup-File $common_assembly_info 
		
	Generate-Assembly-Info `
		-product $product_name `
		-company $company_name `
		-copyright $copyright `
		-version $version `
		-packageVersion $packageVersion `
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
		$json | ConvertTo-Json -depth 100 | Out-File $project -encoding UTF8 -Force
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

  $asmInfo = "using System;
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

function Build-Assemblies([string[]]$projects) {
	foreach ($project in $projects) {
		& dotnet.exe build $project --configuration $configuration
	}
}

function Pack-Assemblies([string[]]$projects) {
	Ensure-Directory-Exists $nuget_package_directory
	foreach ($project in $projects) {
		Write-Host "Creating NuGet package for $project..." -ForegroundColor Magenta
		& dotnet.exe pack $project --configuration $Configuration --output $nuget_package_directory --no-build
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