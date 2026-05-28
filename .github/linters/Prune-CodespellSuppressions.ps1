#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Reports (and optionally removes) entries in codespell.txt that codespell no longer flags.

.DESCRIPTION
    Codespell has no built-in way to detect unused entries in an ignore-words file.
    This script runs codespell against the repo with NO ignore-words file, collects the
    set of words it flags, and reports any suppression in codespell.txt that is not in
    that set (i.e., the suppression is no longer needed).

    The pre-commit hook invocation in .pre-commit-config.yaml is mirrored here so the
    scan covers the same files codespell would normally see.

.PARAMETER Apply
    Rewrite codespell.txt with the unused entries removed. Without this switch the
    script only reports.

.PARAMETER CodespellCommand
    Override the codespell executable (default: "codespell").
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$CodespellCommand = 'codespell'
)

$ErrorActionPreference = 'Stop'

$repoRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$suppressionsFile = Join-Path $repoRoot '.github/linters/codespell.txt'

if (-not (Test-Path $suppressionsFile)) {
    throw "Suppressions file not found: $suppressionsFile"
}

# Must match the `exclude:` regex for the codespell hook in .pre-commit-config.yaml.
$excludeRegex = '^src/Lucene\.Net\.Analysis\.Common/Analysis/../.*\.rslp$|^.*Lucene\.Net\.Tests.*$'

$suppressions = Get-Content -LiteralPath $suppressionsFile |
    Where-Object { $_ -ne '' }

Write-Host "Loaded $($suppressions.Count) suppression(s) from $suppressionsFile"
Write-Host "Enumerating git-tracked files (to match pre-commit's view of the repo)..."

# pre-commit only feeds tracked files to codespell; running codespell directly would
# also walk bin/, obj/, and other gitignored output. Drive it with `git ls-files`
# so the two scans cover the same file set.
Push-Location $repoRoot
try {
    $trackedFiles = & git ls-files
    Write-Host "Running codespell on $($trackedFiles.Count) tracked file(s) with NO ignore list..."

    # Write the file list to a temp argsfile and pass it with codespell's "@PATH"
    # syntax. This avoids OS argv length limits on large file lists.
    $argsFile = New-TemporaryFile
    try {
        Set-Content -LiteralPath $argsFile -Value $trackedFiles -Encoding utf8
        # We don't care about exit code: a non-zero exit just means it found
        # misspellings, which is exactly what we want.
        $rawOutput = & $CodespellCommand "@$argsFile" 2>&1
    }
    finally {
        Remove-Item -LiteralPath $argsFile -ErrorAction SilentlyContinue
    }
}
finally {
    Pop-Location
}

# Codespell output format: "<path>:<line>: <flagged> ==> <suggestion>"
# Filter out paths that match the pre-commit exclude regex, then extract the flagged word.
$flagged = New-Object System.Collections.Generic.HashSet[string]
foreach ($line in $rawOutput) {
    if ($line -notmatch '^(?<path>[^:]+):\d+:\s+(?<word>\S+)\s+==>') { continue }
    $path = $Matches['path']
    if ($path -match $excludeRegex) { continue }
    # Suppressions are case-sensitive and matched against the dictionary entry's case.
    [void]$flagged.Add($Matches['word'].ToLowerInvariant())
}

Write-Host "Codespell flagged $($flagged.Count) distinct word(s) (after applying exclude regex)."

$unused = $suppressions | Where-Object { -not $flagged.Contains($_.ToLowerInvariant()) }

if (-not $unused) {
    Write-Host "No unused suppressions found. codespell.txt is clean." -ForegroundColor Green
    return
}

Write-Host ""
Write-Host "Unused suppressions ($($unused.Count)):" -ForegroundColor Yellow
$unused | ForEach-Object { Write-Host "  $_" }

if ($Apply) {
    $keep = $suppressions | Where-Object { $flagged.Contains($_.ToLowerInvariant()) }
    # Preserve trailing newline that codespell.txt currently has.
    Set-Content -LiteralPath $suppressionsFile -Value $keep -Encoding utf8
    Write-Host ""
    Write-Host "Removed $($unused.Count) unused entr$(if ($unused.Count -eq 1) { 'y' } else { 'ies' }) from codespell.txt." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Re-run with -Apply to remove these from codespell.txt."
    exit 1
}
