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
    [string] $LuceneNetVersion, # TODO: Validate this with regex
    [switch] $ServeDocs = $false,
    [switch] $Clean = $false,
    [switch] $DisableMetaData = $false,
    [switch] $DisableBuild = $false,
    [switch] $DisablePlugins = $false,
    # LogLevel can be: Diagnostic, Verbose, Info, Warning, Error
    [Parameter(Mandatory = $false)]
    [string] $LogLevel = 'Warning',
    [Parameter(Mandatory = $false)]
    [string] $BaseUrl = 'https://lucenenet.apache.org/docs/',
    [Parameter(Mandatory = $false)]
    [int] $StagingPort = 8080
)

# if the base URL is the lucene live site default value we also need to include the version
if ($BaseUrl -eq 'https://lucenenet.apache.org/docs/') {
    $BaseUrl += $LuceneNetVersion
}
$BaseUrl = $BaseUrl.TrimEnd('/') # Remove any trailing slash
Write-Host "Base URL for xref map set to $BaseUrl"

# HACK: Our plugin only recognizes the version number through an environment variable,
# so we set it here.
$env:LuceneNetVersion = $LuceneNetVersion

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (get-item $PSScriptFilePath).Directory.Parent.Parent.FullName;
$ApiDocsFolder = Join-Path -Path $RepoRoot -ChildPath "websites\apidocs";
$ToolsFolder = Join-Path -Path $ApiDocsFolder -ChildPath "tools";
#ensure the /build/tools folder
New-Item $ToolsFolder -type directory -force

if ($Clean) {
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
    Invoke-WebRequest "https://github.com/dotnet/docfx/releases/download/v2.56.2/docfx.zip" -OutFile $DocFxZip -TimeoutSec 60

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
    $dir = Get-ChildItem "$path\vswhere.*" | Sort-Object -property Name -descending | Select-Object -first 1
    $file = Get-ChildItem -path "$dir" -name vswhere.exe -recurse
    Move-Item "$dir\$file" $vswhere
}

Remove-Item  -Recurse -Force "$ToolsFolder\tmp"

# delete anything that already exists
if ($Clean) {
    Write-Host "Cleaning..."
    Remove-Item (Join-Path -Path $ApiDocsFolder "_site\*") -recurse -force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path -Path $ApiDocsFolder "_site") -force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path -Path $ApiDocsFolder "obj\*") -recurse -force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path -Path $ApiDocsFolder "obj") -force -ErrorAction SilentlyContinue
}

# Build our custom docfx tools

if ($DisablePlugins -eq $false) {
    $MSBuild = &$vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1
    if (-not (test-path $MSBuild)) {
        throw "MSBuild not found!"
    }

    # Build the plugin solution
    $pluginSln = (Join-Path -Path $RepoRoot "src\docs\DocumentationTools.sln")
    & $nuget restore $pluginSln

    $PluginsFolder = (Join-Path -Path $ApiDocsFolder "Templates\LuceneTemplate\plugins")
    New-Item $PluginsFolder -type directory -force
    & $msbuild $pluginSln /target:LuceneDocsPlugins "/p:OutDir=$PluginsFolder"
}

# update the docjx.global.json file based
$DocFxGlobalJson = Join-Path -Path $ApiDocsFolder "docfx.global.json"
$DocFxJsonContent = Get-Content $DocFxGlobalJson | ConvertFrom-Json
$DocFxJsonContent._appFooter = "Copyright © $((Get-Date).Year) Licensed to the Apache Software Foundation (ASF)"
$DocFxJsonContent._appTitle = "Apache Lucene.NET $LuceneNetVersion Documentation"
$DocFxJsonContent._gitContribute.branch = "docs/$LuceneNetVersion"
$DocFxJsonContent | ConvertTo-Json -depth 100 | Set-Content $DocFxGlobalJson

# NOTE: The order of these depends on if one of the projects requries the xref map of another, normally all require the core xref map
$DocFxJsonMeta = @(
    "docfx.codecs.json",
    "docfx.core.json",
    "docfx.analysis-common.json",
    "docfx.analysis-kuromoji.json",
    "docfx.analysis-morfologik.json",
    "docfx.analysis-opennlp.json",
    "docfx.analysis-phonetic.json",
    "docfx.analysis-smartcn.json",
    "docfx.analysis-stempel.json",
    "docfx.benchmark.json",
    "docfx.classification.json",
    "docfx.expressions.json",
    "docfx.facet.json",
    "docfx.grouping.json",
    "docfx.highlighter.json",
    "docfx.icu.json",
    "docfx.join.json",
    "docfx.memory.json",
    "docfx.misc.json",
    "docfx.queries.json",
    "docfx.queryparser.json",
    "docfx.replicator.json",
    "docfx.sandbox.json",
    "docfx.spatial.json",
    "docfx.suggest.json",
    "docfx.test-framework.json",
    "docfx.demo.json"
)
$DocFxJsonSite = Join-Path -Path $ApiDocsFolder "docfx.site.json"

# set env vars that will be replaced in Markdown
$env:LuceneNetVersion = $LuceneNetVersion

if ($? -and $DisableMetaData -eq $false) {
    foreach ($proj in $DocFxJsonMeta) {
        $projFile = Join-Path -Path $ApiDocsFolder $proj

        $DocFxLog = Join-Path -Path $ApiDocsFolder "obj\${proj}.meta.log"

        # build the output
        Write-Host "Building api metadata for $projFile..."

        if ($Clean) {
            & $DocFxExe metadata $projFile --log "$DocFxLog" --loglevel $LogLevel --force
        }
        else {
            & $DocFxExe metadata $projFile --log "$DocFxLog" --loglevel $LogLevel
        }
    }
}

if ($? -and $DisableBuild -eq $false) {
    foreach ($proj in $DocFxJsonMeta) {
        $projFile = Join-Path -Path $ApiDocsFolder $proj

        $DocFxLog = Join-Path -Path $ApiDocsFolder "obj\${proj}.build.log"

        # build the output
        Write-Host "Building site output for $projFile..."

        # Before we build the site we have to clear the frickin docfx cache!
        # else the xref links don't work on the home page. That is crazy.
        Remove-Item (Join-Path -Path $ApiDocsFolder "obj\.cache") -recurse -force -ErrorAction SilentlyContinue

        if ($Clean) {
            & $DocFxExe build $projFile --log "$DocFxLog" --loglevel $LogLevel --force --debug
        }
        else {
            & $DocFxExe build $projFile --log "$DocFxLog" --loglevel $LogLevel --debug
        }

        # Add the baseUrl to the output xrefmap, see https://github.com/dotnet/docfx/issues/2346#issuecomment-356054027
        $projFileJson = Get-Content $projFile | ConvertFrom-Json
        $projBuildDest = $projFileJson.build.dest
        $buildOutputFolder = Join-Path -Path ((Get-Item $projFile).DirectoryName) $projBuildDest
        $xrefFile = Join-Path $buildOutputFolder "xrefmap.yml"
        $xrefMap = Get-Content $xrefFile -Raw
        $xrefMap = $xrefMap.Replace("### YamlMime:XRefMap", "").Trim()
        $projBaseUrl = $BaseUrl + $projBuildDest.Substring(5, $projBuildDest.Length - 5) # trim the _site part of the string
        $xrefMap = "### YamlMime:XRefMap" + [Environment]::NewLine + "baseUrl: " + $projBaseUrl + "/" + [Environment]::NewLine + $xrefMap
        Set-Content -Path $xrefFile -Value $xrefMap
    }
}

if ($?) {

    # Before we build the site we have to clear the frickin docfx cache!
    # else the xref links don't work on the home page. That is crazy.
    Remove-Item (Join-Path -Path $ApiDocsFolder "obj\.cache") -recurse -force -ErrorAction SilentlyContinue

    $DocFxLog = Join-Path -Path $ApiDocsFolder "obj\docfx.site.json.log"

    if ($ServeDocs -eq $false) {

        # build the output
        Write-Host "Building docs..."

        if ($Clean) {
            & $DocFxExe $DocFxJsonSite --log "$DocFxLog" --loglevel $LogLevel --force --debug
        }
        else {
            & $DocFxExe $DocFxJsonSite --log "$DocFxLog" --loglevel $LogLevel --debug
        }
    }
    else {
        # build + serve (for testing)
        Write-Host "starting website..."
        & $DocFxExe $DocFxJsonSite --log "$DocFxLog" --loglevel $LogLevel --serve --port $StagingPort --debug
    }
}

# need to create one for each site
# and then many of these params can be excluded from the json file

# .\docfx.exe ..\..\docfx.core.json --globalMetadataFiles docfx.global.json --output _TEST --serve --force --loglevel Warning
# docfx.exe --output TARGET --globalMetadataFiles docfx.global.json Warning