 properties {
	[string]$base_directory   = Resolve-Path "..\."
	[string]$release_directory  = "$base_directory\release"
	[string]$source_directory = "$base_directory"
	[string]$tools_directory  = "$base_directory\lib"
	[string]$nuget_package_directory = "$release_directory\NuGetPackages"
	[string]$test_results_directory = "$release_directory\TestResults"

	[string]$packageVersion   = "1.0.0"
	[string]$version          = "0.0.0"
	[string]$configuration    = "Release"

	[string]$common_assembly_info = "$base_directory\src\CommonAssemblyInfo.cs"
	[string]$copyright_year = [DateTime]::Today.Year.ToString() #Get the current year from the system
	[string]$copyright = "Copyright © 2006 - $copyright_year The Apache Software Foundation"
	[string]$company_name = "The Apache Software Foundation"
	[string]$product_name = "Lucene.Net"
	
	#test paramters
	[string]$frameworks_to_test = "net451,netcoreapp1.0"
	[string]$where = ""
}

task default -depends Build

task Clean -description "This task cleans up the build directory" {
	#Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
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
	#exec {
	#	&"$tools_directory\nuget\nuget.exe" update -self
	#} -ErrorAction SilentlyContinue
}

task Restore -depends Clean -description "This task runs NuGet package restore" {
	#& dotnet.exe restore
}

#task Compile -depends Clean, Init, Restore -description "This task compiles the solution" -PreAction { Write-Host "Pre-action (compile)" } -Action {
#	Write-Host "Compile"
#} -PostAction { Write-Host "Post-action (compile)" }

#task Pack -depends Compile -description "This tasks creates the NuGet packages" -PreAction { Write-Host "Pre-action (pack)" } -Action {
#	Write-Host "Pack"
#	Start-Sleep 5
#} -PostAction { Write-Host "Post-action (pack)" }

task Build -depends Clean, Init, Restore -description "This task builds and packages the assemblies" {

}

task Test -description "This task runs the tests" {
	Write-Host "Running tests..." -ForegroundColor DarkCyan

	pushd $base_directory
	$testProjects = Get-ChildItem -Path "project.json" -Recurse | ? { $_.Directory.Name.Contains(".Tests") }
	popd

	$frameworksToTest = $frameworks_to_test -split "\s*?,\s*?"

	foreach ($framework in $frameworksToTest) {
		Write-Host "Framework: $framework" -ForegroundColor Blue

		foreach ($testProject in $testProjects) {

			$testName = $testProject.Directory.Name
			$projectDirectory = $testProject.DirectoryName
			Write-Host "Directory: $projectDirectory" -ForegroundColor Green

			if ($framework.StartsWith("netcore")) {
				$testExpression = "dotnet.exe test '$projectDirectory\project.json' --configuration $configuration --no-build"
			} else {
				$binaryRoot = "$projectDirectory\bin\$configuration\$framework"

				$testBinary = "$binaryRoot\win7-x64\$testName.dll"
				if (-not (Test-Path $testBinary)) {
					$testBinary = "$binaryRoot\win7-x32\$testName.dll"
				}
				if (-not (Test-Path $testBinary)) {
					$testBinary = "$binaryRoot\$testName.dll"
				} 

				$testExpression = "$tools_directory\NUnit\NUnit.ConsoleRunner.3.5.0\tools\nunit3-console.exe $testBinary --teamcity"
			}

			$testResultDirectory = "$test_results_directory\$framework\$testName"
			Ensure-Directory-Exists $testResultDirectory

			$testExpression = "$testExpression --result:$testResultDirectory\TestResult.xml"

			if ($where -ne $null -and (-Not [System.String]::IsNullOrEmpty($where))) {
				$testExpression = "$testExpression --where $where"
			}

			Write-Host $testExpression -ForegroundColor Magenta

			Invoke-Expression $testExpression
		}
	}
}






function Ensure-Directory-Exists([string] $path)
{
	if (!(Test-Path $path)) {
		New-Item $path -ItemType Directory
	}
}