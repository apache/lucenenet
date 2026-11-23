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

# Applies the website updates for a new Lucene.NET release:
#
# - Creates the download page /websites/site/download/version-<Version>.md (based on the previous release's page)
# - Adds the release to /websites/site/download/toc.yml and /websites/site/download/download.md
# - Adds the API docs link to /websites/site/docs.md
# - Points the absolute-latest (and for stable releases, latest) API docs redirects in .htaccess to the release
# - Adds the release to doap_Lucene_Net.rdf (stable releases only)
# - Updates the package versions in the quick-start tutorial and the lucene-cli install instructions
#
# The script is idempotent: each step is skipped if it has already been applied, so it can be re-run safely.
#
# Usage:
#   ./websites/site/add-release.ps1 -Version 4.8.0-beta00019 -ReleaseDate 2026-11-01

param (
    # The release version, for example 4.8.0-beta00019, 4.8.0 or 4.8.0.1
    [Parameter(Mandatory=$true)]
    [string]
    $Version,
    # The release date in yyyy-MM-dd format. Defaults to today (UTC).
    [Parameter(Mandatory=$false)]
    [string]
    $ReleaseDate = [DateTime]::UtcNow.ToString('yyyy-MM-dd')
)

$ErrorActionPreference = 'Stop'

# See /websites/site/contributing/versioning.md. Pre-release versions do not support revision numbers.
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+|-[a-z]+\d+)?$') {
    throw "Version '$Version' is not in the expected format, for example 4.8.0-beta00019, 4.8.0 or 4.8.0.1"
}
if ($ReleaseDate -notmatch '^\d{4}-\d{2}-\d{2}$') {
    throw "ReleaseDate '$ReleaseDate' is not in the expected format yyyy-MM-dd"
}

$IsPrerelease = $Version.Contains('-')
$Status = if ($IsPrerelease) { 'Beta' } else { 'Stable' }

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (Get-Item $PSScriptFilePath).Directory.Parent.Parent.FullName
$SiteFolder = Join-Path -Path $RepoRoot -ChildPath 'websites/site'
$DownloadFolder = Join-Path -Path $SiteFolder -ChildPath 'download'

$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

# Converts a version such as 4.8.0-beta00019 to its release tag, Lucene.Net_4_8_0_beta00019
function Get-ReleaseTag([string] $v) {
    return 'Lucene.Net_' + $v.Replace('.', '_').Replace('-', '_')
}

function Read-TextFile([string] $path) {
    return [System.IO.File]::ReadAllText($path, $Utf8NoBom)
}

# Writes without a BOM. Some files (such as .htaccess) MUST NOT have a BOM.
function Write-TextFile([string] $path, [string] $text) {
    [System.IO.File]::WriteAllText($path, $text, $Utf8NoBom)
}

function Get-NewLine([string] $text) {
    if ($text.Contains("`r`n")) { return "`r`n" }
    return "`n"
}

# Inserts $insert immediately before the first line matching $pattern
function Add-BeforeFirstMatch([string] $path, [string] $pattern, [string[]] $insert) {
    $text = Read-TextFile $path
    $nl = Get-NewLine $text
    $match = [regex]::Match($text, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $match.Success) {
        throw "Could not find a line matching '$pattern' in $path"
    }
    $block = ($insert -join $nl) + $nl
    Write-TextFile $path ($text.Insert($match.Index, $block))
}

function Update-File([string] $path, [scriptblock] $transform, [string] $description) {
    $text = Read-TextFile $path
    $updated = & $transform $text
    if ($updated -ceq $text) {
        Write-Host "  [skip] $description (already up to date)"
    } else {
        Write-TextFile $path $updated
        Write-Host "  [done] $description"
    }
}

Write-Host "Updating website for Lucene.NET $Version ($Status, released $ReleaseDate)..."

# --- Download page ---

$TocPath = Join-Path -Path $DownloadFolder -ChildPath 'toc.yml'
$NewPageName = "version-$Version.md"
$NewPagePath = Join-Path -Path $DownloadFolder -ChildPath $NewPageName

if (Test-Path $NewPagePath) {
    Write-Host "  [skip] download/$NewPageName (already exists)"
} else {
    # The previous release is the first entry in the download TOC
    $tocMatch = [regex]::Match((Read-TextFile $TocPath), 'href:\s*version-(?<v>[^\s]+)\.md')
    if (-not $tocMatch.Success) {
        throw "Could not determine the previous release from $TocPath"
    }
    $PreviousVersion = $tocMatch.Groups['v'].Value
    $templatePath = Join-Path -Path $DownloadFolder -ChildPath "version-$PreviousVersion.md"
    $page = Read-TextFile $templatePath
    # Only whole versions are replaced, so (for example) the link to the Java Lucene 4.8.0 release (".../lucene-solr%2F4.8.0")
    # is left alone when the previous release is 4.8.0
    foreach ($pair in @(@((Get-ReleaseTag $PreviousVersion), (Get-ReleaseTag $Version)), @($PreviousVersion, $Version))) {
        $page = [regex]::Replace($page, '(?<![\w.])' + [regex]::Escape($pair[0]) + '(?![\w-]|\.\d)', $pair[1])
    }
    $page = [regex]::Replace($page, '(?m)^_Released:_ `[^`]*`', "_Released:_ ``$ReleaseDate``")
    $page = [regex]::Replace($page, '(?m)^_Status:_ \*\*`[^`]*`\*\*', "_Status:_ **``$Status``**")
    Write-TextFile $NewPagePath $page
    Write-Host "  [done] download/$NewPageName (copied from version-$PreviousVersion.md)"
    Write-Warning "Review the 'Supported Frameworks' and 'All Packages' sections and the Java Lucene release link of download/$NewPageName, they were copied from $PreviousVersion"
}

if ((Read-TextFile $TocPath).Contains("href: $NewPageName")) {
    Write-Host "  [skip] download/toc.yml (already contains $Version)"
} else {
    Add-BeforeFirstMatch $TocPath '^- name:' @("- name: Version $Version", "  href: $NewPageName")
    Write-Host "  [done] download/toc.yml"
}

$DownloadMdPath = Join-Path -Path $DownloadFolder -ChildPath 'download.md'
if ((Read-TextFile $DownloadMdPath).Contains("(xref:download/$Version)")) {
    Write-Host "  [skip] download/download.md (already contains $Version)"
} else {
    Add-BeforeFirstMatch $DownloadMdPath '^## \[Lucene ' @(
        "## [Lucene $Version](xref:download/$Version)",
        '',
        "_Status:_ **``$Status``**",
        '',
        "_Released:_ ``$ReleaseDate``",
        '')
    Write-Host "  [done] download/download.md"
}

# --- API docs links ---

# docs.md has a section per Lucene version (for example, "## Lucene.NET 4.8.0") listing the API docs of
# each of its releases, newest first
$DocsMdPath = Join-Path -Path $SiteFolder -ChildPath 'docs.md'
$DocsUrl = "https://lucenenet.apache.org/docs/$Version/"
$LuceneVersion = [regex]::Match($Version, '^\d+\.\d+\.\d+').Value
$DocsMd = Read-TextFile $DocsMdPath
if ($DocsMd.Contains("($DocsUrl)")) {
    Write-Host "  [skip] docs.md (already contains $Version)"
} else {
    $sectionHeading = "## Lucene.NET $LuceneVersion"
    $section = [regex]::Match($DocsMd, '(?m)^' + [regex]::Escape($sectionHeading) + '\s*$')
    if ($section.Success) {
        $nl = Get-NewLine $DocsMd
        $link = [regex]::Match($DocsMd.Substring($section.Index), '(?m)^- \[')
        if (-not $link.Success) {
            throw "Could not find the list of API docs links under '$sectionHeading' in $DocsMdPath"
        }
        Write-TextFile $DocsMdPath ($DocsMd.Insert($section.Index + $link.Index, "- [$Version]($DocsUrl)$nl"))
    } else {
        Add-BeforeFirstMatch $DocsMdPath '^## Lucene\.NET ' @(
            $sectionHeading,
            '',
            'The API docs are slightly different between versions, each one is listed below:',
            '',
            "- [$Version]($DocsUrl)",
            '')
    }
    Write-Host "  [done] docs.md"
}

# NOTE: Only lines that start with "# Redirect" or "RewriteRule" are changed so the explanatory comments stay intact
$HtaccessPath = Join-Path -Path $SiteFolder -ChildPath 'lucenetemplate/.htaccess'
Update-File $HtaccessPath {
    param($text)
    $text = [regex]::Replace($text, '(?m)^((?:# Redirect|RewriteRule) \^?/?docs/absolute.*?/docs/)[^/\s]+/', "`${1}$Version/")
    if (-not $IsPrerelease) {
        $text = [regex]::Replace($text, '(?m)^((?:# Redirect|RewriteRule) \^?/?docs/latest/.*?/docs/)[^/\s]+/', "`${1}$Version/")
    }
    return $text
} 'lucenetemplate/.htaccess'

# --- DOAP (stable releases only) ---

$DoapPath = Join-Path -Path $SiteFolder -ChildPath 'lucenetemplate/doap_Lucene_Net.rdf'
if ($IsPrerelease) {
    Write-Host "  [skip] lucenetemplate/doap_Lucene_Net.rdf (pre-release)"
} elseif ((Read-TextFile $DoapPath).Contains("<revision>$Version</revision>")) {
    Write-Host "  [skip] lucenetemplate/doap_Lucene_Net.rdf (already contains $Version)"
} else {
    Add-BeforeFirstMatch $DoapPath '^\s*<release>' @(
        '    <release>',
        '      <Version>',
        "        <name>Apache Lucene.Net $Version</name>",
        "        <created>$ReleaseDate</created>",
        "        <revision>$Version</revision>",
        '      </Version>',
        '    </release>')
    Write-Host "  [done] lucenetemplate/doap_Lucene_Net.rdf"
}

# --- Version references in getting started docs ---

Update-File (Join-Path -Path $SiteFolder -ChildPath 'quick-start/tutorial.md') {
    param($text)
    return [regex]::Replace($text, '(<PackageReference Include="Lucene\.Net[^"]*" Version=")[^"]+(")', "`${1}$Version`${2}")
} 'quick-start/tutorial.md'

foreach ($relativePath in @('README.md', 'src/dotnet/tools/lucene-cli/docs/index.md')) {
    Update-File (Join-Path -Path $RepoRoot -ChildPath $relativePath) {
        param($text)
        return [regex]::Replace($text, '(dotnet tool install lucene-cli -g --version )\S+', "`${1}$Version")
    } $relativePath
}

# --- Release notes (created before the release vote, so only check it exists) ---

$ReleaseNotesPath = Join-Path -Path $SiteFolder -ChildPath "release-notes/version-$Version.md"
if (-not (Test-Path $ReleaseNotesPath)) {
    Write-Warning "release-notes/version-$Version.md does not exist. It should have been added before the release vote."
}

Write-Host "Done."
