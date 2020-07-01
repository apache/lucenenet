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
	[Parameter(Mandatory)]
	[string]
	$JavaLuceneVersion
)

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).Directory
$RepoRoot = $PSScriptFilePath.Parent.Parent.FullName;
$ToolsFolder = Join-Path -Path $PSScriptFilePath -ChildPath "tools";

#ensure the /tools folder
New-Item $ToolsFolder -type directory -force
New-Item "$ToolsFolder\tmp" -type directory -force

# go get the java lucene tag release
New-Item "$ToolsFolder\java-lucene-release" -type directory -force
$releaseFolder = "$ToolsFolder\java-lucene-release"
$releaseUrl = "https://github.com/apache/lucene-solr/archive/releases/lucene-solr/$($JavaLuceneVersion).zip"
$releaseZip = "$releaseFolder\release.zip"
if (-not (test-path $releaseZip)) {
    Write-Host "Download Java Lucene release files..."
    Invoke-WebRequest $releaseUrl -OutFile $releaseZip -TimeoutSec 60
    Expand-Archive -LiteralPath $releaseZip -DestinationPath $releaseFolder
}
$releaseLuceneFolder = "$releaseFolder\lucene-solr-releases-lucene-solr-$JavaLuceneVersion\lucene"
if (-not (test-path $releaseLuceneFolder)) {
    Write-Error "Could not detect Java release files in folder $releaseLuceneFolder" -ErrorAction Stop
}

# ensure we have NuGet
New-Item "$ToolsFolder\nuget" -type directory -force
$nuget = "$ToolsFolder\nuget\nuget.exe"
if (-not (test-path $nuget)) {
    Write-Host "Download NuGet..."
    Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget -TimeoutSec 60
}

# ensure we have vswhere
New-Item "$ToolsFolder\vswhere" -type directory -force
$vswhere = "$ToolsFolder\vswhere\vswhere.exe"
if (-not (test-path $vswhere)) {
    Write-Host "Download VsWhere..."
    $path = "$ToolsFolder\tmp"
    &$nuget install vswhere -OutputDirectory $path
    $dir = ls "$path\vswhere.*" | sort -property Name -descending | select -first 1
    $file = ls -path "$dir" -name vswhere.exe -recurse
    mv "$dir\$file" $vswhere   
}

Remove-Item  -Recurse -Force "$ToolsFolder\tmp"

# Get MSBuild

$MSBuild = &$vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1
if (-not (test-path $MSBuild)) {
    throw "MSBuild not found!"
}

# Build the solution
$sln = (Join-Path -Path $PSScriptFilePath "DocumentationTools.sln")
& $nuget restore $sln
& $msbuild $sln

# Execute the program
$exe = (Join-Path -Path $PSScriptFilePath "JavaDocToMarkdownConverter\bin\Debug\JavaDocToMarkdownConverter.exe")
& $exe $releaseLuceneFolder $RepoRoot

