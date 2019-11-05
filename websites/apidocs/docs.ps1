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
    [Parameter(Mandatory = $false)]
    [int]
    $ServeDocs = 1,
    [Parameter(Mandatory = $false)]
    [int]
    $Clean = 0,
    # LogLevel can be: Diagnostic, Verbose, Info, Warning, Error
    [Parameter(Mandatory = $false)]
    [string]
    $LogLevel = 'Info'
)

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (get-item $PSScriptFilePath).Directory.Parent.Parent.FullName;
$ApiDocsFolder = Join-Path -Path $RepoRoot -ChildPath "websites\apidocs";
$ToolsFolder = Join-Path -Path $ApiDocsFolder -ChildPath "tools";
#ensure the /build/tools folder
New-Item $ToolsFolder -type directory -force

if ($Clean -eq 1) {
    Write-Host "Cleaning tools..."
    Remove-Item (Join-Path -Path $ToolsFolder "\*") -recurse -force -ErrorAction SilentlyContinue
}

New-Item "$ToolsFolder\tmp" -type directory -force

# Go get docfx.exe if we don't have it
New-Item "$ToolsFolder\docfx" -type directory -force
$DocFxExe = "$ToolsFolder\docfx\docfx.exe"
if (-not (test-path $DocFxExe)) {
    Write-Host "Retrieving docfx..."
    $DocFxZip = "$ToolsFolder\tmp\docfx.zip"	
    Invoke-WebRequest "https://github.com/dotnet/docfx/releases/download/v2.47/docfx.zip" -OutFile $DocFxZip -TimeoutSec 60 
	
    #unzip
    Expand-Archive $DocFxZip -DestinationPath (Join-Path -Path $ToolsFolder -ChildPath "docfx")
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

# delete anything that already exists
if ($Clean -eq 1) {
    Write-Host "Cleaning..."
    Remove-Item (Join-Path -Path $ApiDocsFolder "_site\*") -recurse -force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path -Path $ApiDocsFolder "_site") -force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path -Path $ApiDocsFolder "obj\*") -recurse -force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path -Path $ApiDocsFolder "obj") -force -ErrorAction SilentlyContinue
}

# Build our custom docfx tools

$MSBuild = &$vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1
if (-not (test-path $MSBuild)) {
    throw "MSBuild not found!"
}

# Build the plugin solution
$pluginSln = (Join-Path -Path $RepoRoot "src\docs\LuceneDocsPlugins\LuceneDocsPlugins.sln")
& $nuget restore $pluginSln

$PluginsFolder = (Join-Path -Path $ApiDocsFolder "lucenetemplate\plugins")
New-Item $PluginsFolder -type directory -force
& $msbuild $pluginSln "/p:OutDir=$PluginsFolder"

# Due to a bug with docfx and msbuild, we also need to set environment vars here
# https://github.com/dotnet/docfx/issues/1969
# Then it turns out we also need 2017 build tools installed, wat!? 
# https://www.microsoft.com/en-us/download/details.aspx?id=48159
# NOTE: There's a ton of Lucene docs that we want to copy and re-format. I'm not sure if we can really automate this 
# in a great way since the docs seem to be in many places, for example:
# https://github.com/dotnet/docfx/issues/1969
# Then it turns out we also need 2017 build tools installed, wat!? 
# https://www.microsoft.com/en-us/download/details.aspx?id=48159
# Home page - 	https://github.com/apache/lucene-solr/blob/branch_4x/lucene/site/xsl/index.xsl
# Wiki docs - 	https://wiki.apache.org/lucene-java/FrontPage?action=show&redirect=FrontPageEN - not sure where the source is for this
# The only way i can get this building currently is to have the VS2017 build tools installed.
# UPDATE: Interestingly it now works by passing in the most recent msbuild target...
# TODO: Need to upgrade to latest docfx and figure out why there are issues.

# [Environment]::SetEnvironmentVariable("VSINSTALLDIR", $msbuild)
# [Environment]::SetEnvironmentVariable("VisualStudioVersion", "15.0")

$DocFxJson = Join-Path -Path $ApiDocsFolder "docfx.json"
$DocFxLog = Join-Path -Path $ApiDocsFolder "obj\docfx.log"

if ($?) { 
    if ($ServeDocs -eq 0) {

        Write-Host "Building metadata..."
        if ($Clean -eq 1) {
            & $DocFxExe metadata $DocFxJson -l "$DocFxLog" --loglevel $LogLevel --force
        }
        else {
            & $DocFxExe metadata $DocFxJson -l "$DocFxLog" --loglevel $LogLevel
        }

        # build the output		
        Write-Host "Building docs..."
        & $DocFxExe build $DocFxJson -l "$DocFxLog" --loglevel $LogLevel
    }
    else {
        # build + serve (for testing)
        Write-Host "starting website..."
        & $DocFxExe $DocFxJson --serve
    }
}