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
	[Parameter(Mandatory=$false)]
	[int]
	$ServeDocs = 0,
	[Parameter(Mandatory=$false)]
	[int]
	$Clean = 1
)

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (get-item $PSScriptFilePath).Directory.Parent.FullName;
$ApiDocsFolder = Join-Path -Path $RepoRoot -ChildPath "apidocs";
$ToolsFolder = Join-Path -Path $ApiDocsFolder -ChildPath "tools";
#ensure the /build/tools folder
New-Item $ToolsFolder -type directory -force

# Go get docfx.exe if we don't have it
$DocFxExe = "$ToolsFolder\docfx\docfx.exe"
$FileExists = Test-Path $DocFxExe 
If ($FileExists -eq $False) {
	Write-Host "Retrieving docfx..."
	$DocFxZip = "$ToolsFolder\docfx.zip"
	$SourceDocFx = "https://github.com/dotnet/docfx/releases/download/v2.17.4/docfx.zip"
	Invoke-WebRequest $SourceDocFx -OutFile $DocFxZip
	#unzip
	Expand-Archive $DocFxZip -DestinationPath (Join-Path -Path $ToolsFolder -ChildPath "docfx")
}

# delete anything that already exists
if ($Clean -eq 1) {
	Remove-Item (Join-Path -Path $ApiDocsFolder "obj\*") -recurse -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $ApiDocsFolder "obj") -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $ApiDocsFolder "api\*") -recurse -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $ApiDocsFolder "api") -force -ErrorAction SilentlyContinue
}

$DocFxJson = Join-Path -Path $RepoRoot "apidocs\docfx.json"

Write-Host "Building metadata..."
& $DocFxExe metadata $DocFxJson
if($?) { 
	if ($ServeDocs -eq 0){
		# build the output		
		Write-Host "Building docs..."
		& $DocFxExe build $DocFxJson	
	}
	else {
		# build + serve (for testing)
		Write-Host "starting website..."
		& $DocFxExe $DocFxJson --serve
	}
}