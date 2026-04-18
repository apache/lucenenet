# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.

<#
.SYNOPSIS
    Helper script to download and run the Apache Release Audit Tool (RAT).

.DESCRIPTION
    This script automates use of the Apache RAT tool during release preparation.
    It ensures that the RAT JAR is available locally (downloading it if needed)
    and then runs it against the source tree to check for missing license headers
    and other compliance issues.

    By default, the tool is downloaded into a `.tools/rat` directory located next
    to this script. The script always runs RAT with the repository root (same
    directory as this script) as the target directory.

.PARAMETER Version
    The version of Apache RAT to use (default: 0.18).

.PARAMETER ExcludeFileName
    Name of an exclude file containing path patterns that RAT should ignore.
    This file should be located next to the script. Default: rat-exclude.txt

.REQUIREMENTS
    - Java 8+ must be installed and available on the PATH.
    - Internet connection (for first run, to download RAT).

.EXAMPLE
    pwsh ./rat.ps1

    Runs Apache RAT (default version 0.18) with exclusions from `rat-exclude.txt`.

.EXAMPLE
    pwsh ./rat.ps1 -Version 0.18 -ExcludeFileName custom-exclude.txt

    Runs Apache RAT version 0.18 using the specified exclude file.

.NOTES
    This script is intended for use by release managers when preparing official
    ASF releases. It is not normally required for day-to-day development.
#>
param(
    [string]$Version = "0.18",
    [string]$ExcludeFileName = ".rat-excludes"
)

# Script directory (works in PowerShell Core and Windows PowerShell)
$scriptDir = $PSScriptRoot

# Tool paths (kept under the script dir)
$ratDir = Join-Path $scriptDir ".tools\rat"
$ratJar = Join-Path $ratDir "apache-rat-$Version.jar"
$ratUrl = "https://repo1.maven.org/maven2/org/apache/rat/apache-rat/$Version/apache-rat-$Version.jar"

# Exclude file path (resolved relative to script dir)
$ratExcludeFile = Join-Path $scriptDir $ExcludeFileName

# Ensure tool folder exists and jar is present (download if missing)
if (-not (Test-Path $ratDir)) {
    New-Item -ItemType Directory -Path $ratDir | Out-Null
}

if (-not (Test-Path $ratJar)) {
    Write-Host "Downloading Apache RAT $Version to $ratJar ..."
    Invoke-WebRequest -Uri $ratUrl -OutFile $ratJar
}

# If exclude file is optional, optionally warn if missing:
if (-not (Test-Path $ratExcludeFile)) {
    Write-Host "Warning: exclude file '$ratExcludeFile' not found. Continuing without --input-exclude-file."
    $useExclude = $false
} else {
    $useExclude = $true
}

$argsList = @(
    "-jar", $ratJar,
    "--edit-license",
    "--edit-overwrite"
)

if ($useExclude) {
    $argsList += @("--input-exclude-file", "$ratExcludeFile")
}

$argsList += @("--", "$scriptDir")

# Call java with argument list. Use & to invoke program.
& java @argsList

if ($LASTEXITCODE -ne 0) {
    throw "RAT exited with code $LASTEXITCODE"
}

# Remove trailing whitespace from files modified by RAT
Write-Host "Removing trailing whitespace from modified files..."

# Get list of modified files from git
$modifiedFiles = git diff --name-only --diff-filter=M
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Could not get modified files list from git."
} else {
    foreach ($file in $modifiedFiles) {
        if ([string]::IsNullOrWhiteSpace($file)) { continue }

        $filePath = Join-Path $scriptDir $file
        if (-not (Test-Path $filePath)) { continue }

        try {
            # Read all lines, trim trailing whitespace, and write back
            $lines = Get-Content -Path $filePath
            if ($null -ne $lines) {
                $trimmedLines = $lines | ForEach-Object { $_.TrimEnd() }
                $trimmedLines | Set-Content -Path $filePath -NoNewline:$false
                Write-Host "  Cleaned: $file"
            }
        } catch {
            Write-Host "  Warning: Could not process $file : $_"
        }
    }
}
