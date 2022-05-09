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
    [ValidatePattern('\d+?\.\d+?\.\d+?(?:\.\d+?)?(?:-\w+)?')]
    [string] $LuceneNetVersion,
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
$MinimumSdkVersion = "3.1.100" # Minimum Required .NET SDK (must not be a pre-release)

$ErrorActionPreference = "Stop"

# if the base URL is the lucene live site default value we also need to include the version
if ($BaseUrl -eq 'https://lucenenet.apache.org/docs/') {
    $BaseUrl += $LuceneNetVersion
}
$BaseUrl = $BaseUrl.TrimEnd('/') # Remove any trailing slash
Write-Host "Base URL for xref map set to $BaseUrl"

# Generate the Git tag for the current version
$VCSLabel = 'Lucene.Net_' + $LuceneNetVersion.Replace('.', '_').Replace('-', '_')

# set env vars that will be replaced in Markdown
$env:LuceneNetVersion = $LuceneNetVersion
$env:LuceneNetReleaseTag = $VCSLabel

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (get-item $PSScriptFilePath).Directory.Parent.Parent.FullName;
$ApiDocsFolder = Join-Path -Path $RepoRoot -ChildPath "websites\apidocs";
$ToolsFolder = Join-Path -Path $ApiDocsFolder -ChildPath "tools";
$CliIndexPath = Join-Path -Path $RepoRoot -ChildPath "src\dotnet\tools\lucene-cli\docs\index.md";
$TocPath1 = Join-Path -Path $ApiDocsFolder -ChildPath "toc.yml"
$TocPath2 = Join-Path -Path $ApiDocsFolder -ChildPath "toc\toc.yml"
$BreadcrumbPath = Join-Path -Path $ApiDocsFolder -ChildPath "docfx.global.subsite.json"

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
    Invoke-WebRequest "https://github.com/dotnet/docfx/releases/download/v2.58/docfx.zip" -OutFile $DocFxZip -TimeoutSec 60

    #unzip
    Expand-Archive $DocFxZip -DestinationPath (Join-Path -Path $ToolsFolder -ChildPath "docfx")
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
     # Check prerequisites
    $SdkVersion = ((& dotnet --version) | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command was not found. Please install .NET $MinimumSdkVersion or higher SDK and make sure it is in your PATH."
    }
    $ReleaseVersion = if ($sdkVersion.Contains('-')) { "$SdkVersion".Substring(0, "$SdkVersion".IndexOf('-')) } else { $SdkVersion }
    if ([version]$ReleaseVersion -lt ([version]$MinimumSdkVersion)) {
        throw "Minimum .NET SDK $MinimumSdkVersion required. Current SDK version is $ReleaseVersion. Please install the required SDK before running the command."
    }

    $pluginProject = (Join-Path -Path $RepoRoot "src/docs/LuceneDocsPlugins/LuceneDocsPlugins.csproj")
    $PluginsFolder = (Join-Path -Path $ApiDocsFolder "Templates/LuceneTemplate/plugins")

    New-Item $PluginsFolder -type directory -force
    # This will restore, build, and copy all files (including dependencies) to the output folder
    & dotnet publish "$pluginProject" --configuration Release --output "$PluginsFolder" --verbosity normal

    if (-not $?) {throw "Failed to build plugin project"}
}

# update the docjx.global.json file based
$DocFxGlobalJson = Join-Path -Path $ApiDocsFolder "docfx.global.json"
$DocFxJsonContent = Get-Content $DocFxGlobalJson | ConvertFrom-Json
$DocFxJsonContent._appFooter = "Copyright &copy; $((Get-Date).Year) The Apache Software Foundation, Licensed under the <a href='http://www.apache.org/licenses/LICENSE-2.0' target='_blank'>Apache License, Version 2.0</a><br/> <small>Apache Lucene.Net, Lucene.Net, Apache, the Apache feather logo, and the Apache Lucene.Net project logo are trademarks of The Apache Software Foundation. <br/>All other marks mentioned may be trademarks or registered trademarks of their respective owners.</small>"
$DocFxJsonContent._appTitle = "Apache Lucene.NET $LuceneNetVersion Documentation"
$DocFxJsonContent._gitContribute.branch = "docs/$LuceneNetVersion"
$DocFxJsonContent._gitContribute.tag = "$VCSLabel"
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
    # HACK: DocFx doesn't seem to work with fenced code blocks, so we update the lucene-cli index file manually.
    # Note it works better this way anyway because we can store a real version number in the file in the repo.
    (Get-Content -Path $CliIndexPath -Raw) -Replace '(?<=--version\s)\d+?\.\d+?\.\d+?(?:\.\d+?)?(?:-\w+)?', $LuceneNetVersion | Set-Content -Path $CliIndexPath

    # Update our TOC to the latest LuceneNetVersion
    (Get-Content -Path $TocPath1 -Raw) -Replace '(?<=lucenenet\.apache\.org\/docs\/)\d+?\.\d+?\.\d+?(?:\.\d+?)?(?:-\w+)?', $LuceneNetVersion | Set-Content -Path $TocPath1
    (Get-Content -Path $TocPath2 -Raw) -Replace '(?<=lucenenet\.apache\.org\/docs\/)\d+?\.\d+?\.\d+?(?:\.\d+?)?(?:-\w+)?', $LuceneNetVersion | Set-Content -Path $TocPath2

    # Update the API link to the latest LuceneNetVersion
    # Note we don't update _rel because that is used for styles and js
    (Get-Content -Path $BreadcrumbPath -Raw) -Replace '(?<="_api":\s*?"https?\:\/\/lucenenet\.apache\.org\/docs\/)\d+?\.\d+?\.\d+?(?:\.\d+?)?(?:-\w+)?', $LuceneNetVersion | Set-Content -Path $BreadcrumbPath

    foreach ($proj in $DocFxJsonMeta) {
        $projFile = Join-Path -Path $ApiDocsFolder $proj

        $DocFxLog = Join-Path -Path $ApiDocsFolder "obj\${proj}.build.log"

        # build the output
        Write-Host "Building site output for $projFile..."

        # Specifying --force, --forcePostProcess and --cleanupCacheHistory
        # seems to be required in order for all xref links to resolve consistently across
        # all of the docfx builds (see https://dotnet.github.io/docfx/RELEASENOTE.html?tabs=csharp).
        # Previously we used to do this:
        #   Remove-Item (Join-Path -Path $ApiDocsFolder "obj\.cache") -recurse -force -ErrorAction SilentlyContinue
        # to force remove the cache else the xref's wouldn't work correctly. So far with these new parameters
        # it seems much happier.

        if ($Clean) {
            & $DocFxExe build $projFile --log "$DocFxLog" --loglevel $LogLevel --force --debug --cleanupCacheHistory --force --forcePostProcess
        }
        else {
            & $DocFxExe build $projFile --log "$DocFxLog" --loglevel $LogLevel --debug --cleanupCacheHistory --force --forcePostProcess
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