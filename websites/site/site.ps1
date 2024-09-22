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

param (
	[switch] $ServeDocs = $false,
	[switch] $Clean = $false,
	# LogLevel can be: Diagnostic, Verbose, Info, Warning, Error
	[Parameter(Mandatory=$false)]
	[string]
	$LogLevel = 'Info',
	[Parameter(Mandatory=$false)]
	[int]
	$StagingPort = 8081
)

$DocFxVersion = "2.77.0" # Required DocFx version

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (get-item $PSScriptFilePath).Directory.Parent.Parent.FullName;
$SiteFolder = Join-Path -Path $RepoRoot -ChildPath "websites\site";
$ToolsFolder = Join-Path -Path $SiteFolder -ChildPath "tools";
#ensure the /build/tools folder
New-Item $ToolsFolder -type directory -force

if ($Clean) {
	Write-Host "Cleaning tools..."
	Remove-Item (Join-Path -Path $ToolsFolder "\*") -recurse -force -ErrorAction SilentlyContinue
}

# install docfx tool
$InstallDocFx = $false
try {
	$InstalledDocFxVersion = (& docfx --version).Trim().Split('+')[0]

	if ([version]$InstalledDocFxVersion -lt [version]$DocFxVersion) {
		Write-Host "DocFx is installed, but the version is less than $DocFxVersion, will install it."
		$InstallDocFx = $true
	}
	else {
		Write-Host "DocFx is installed and the version is $InstalledDocFxVersion."
	}
} catch {
	Write-Host "DocFx is not installed or not in the PATH, will install it."
	$InstallDocFx = $true
}

if ($InstallDocFx -eq $true) {
	Write-Host "Installing docfx global tool..."
	dotnet tool install -g docfx --version $DocFxVersion
}

# delete anything that already exists
if ($Clean) {
	Write-Host "Cleaning..."
	Remove-Item (Join-Path -Path $SiteFolder "_site\*") -recurse -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $SiteFolder "_site") -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $SiteFolder "obj\*") -recurse -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $SiteFolder "obj") -force -ErrorAction SilentlyContinue
}

$DocFxJson = Join-Path -Path $SiteFolder "docfx.json"
$DocFxLog = Join-Path -Path $SiteFolder "obj\docfx.log"

if($?) {
	if ($ServeDocs -eq $false) {
		# build the output
		Write-Host "Building docs..."
		& docfx build $DocFxJson -l "$DocFxLog" --logLevel $LogLevel
	}
	else {
		# build + serve (for testing)
		Write-Host "starting website..."
		& docfx $DocFxJson --serve --port $StagingPort
	}
}
