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

# Runs both directions of the Lucene 4.8.1 <-> Lucene.NET index compatibility
# check (issue #270). Cross-platform (Windows / macOS / Linux PowerShell). Requires
# a JDK and the .NET SDK. All generated indexes go under the gitignored work/
# folder; nothing is committed.
#
# By default the test shard builds with its own default target framework. Set
# $env:COMPAT_TFM (e.g. net10.0, net8.0) to force a specific one.

$ErrorActionPreference = 'Stop'

# Guarantee a non-zero exit code on any failure so CI fails the job. A bare throw
# is not always reflected in the process exit code across PowerShell hosts/versions.
trap {
    Write-Host "==> Compatibility check FAILED: $_" -ForegroundColor Red
    exit 1
}

$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $Here '..' '..' '..')
$Work = Join-Path $Here 'work'
$Shard = Join-Path $RepoRoot 'src' 'Lucene.Net.Tests._J-S' 'Lucene.Net.Tests._J-S.csproj'

$JavaIndex = Join-Path $Work 'java'
$DotNetIndex = Join-Path $Work 'dotnet'

# Pick the correct Maven wrapper for the OS (mvnw.cmd is the Windows batch file).
$Mvnw = if ($IsWindows) { Join-Path $Here 'mvnw.cmd' } else { Join-Path $Here 'mvnw' }

# Only pass -f when the caller explicitly forces a target framework.
$TfmArgs = if ($env:COMPAT_TFM) { @('-f', $env:COMPAT_TFM) } else { @() }

function Invoke-DotNetTest([string]$Filter) {
    # dotnet test exits 0 when a --filter matches nothing, which would hide a
    # misconfiguration. Capture output, surface it, and fail on "matched 0".
    $output = & dotnet test $Shard @TfmArgs -c Release --no-build --filter $Filter 2>&1
    $exit = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    if ($exit -ne 0) { throw "dotnet test failed for filter '$Filter'" }
    if ($output -match 'no test is available|matches the given testcase filter|total:\s*0\b') {
        throw "No tests ran for filter '$Filter'. Is the shard built for this target framework?"
    }
}

function Invoke-Maven([string[]]$MvnArgs) {
    Push-Location $Here
    try {
        & $Mvnw @MvnArgs
        if ($LASTEXITCODE -ne 0) { throw "Maven failed: $($MvnArgs -join ' ')" }
    } finally {
        Pop-Location
    }
}

Write-Host "==> Building the .NET test shard$(if ($env:COMPAT_TFM) { " ($env:COMPAT_TFM)" })"
& dotnet build $Shard @TfmArgs -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

Write-Host ""
Write-Host "==> Direction 1: .NET writes, Java reads"
Write-Host "    .NET writing index into $DotNetIndex"
[Environment]::SetEnvironmentVariable('lucenenet.compat.write.dir', $DotNetIndex, 'Process')
try {
    Invoke-DotNetTest 'FullyQualifiedName~TestJavaCompatibility.TestWriteIndexForJava'
} finally {
    [Environment]::SetEnvironmentVariable('lucenenet.compat.write.dir', $null, 'Process')
}

foreach ($variant in @('index.481.nocfs', 'index.481.cfs')) {
    $indexDir = Join-Path $DotNetIndex $variant
    Write-Host "    Java reading $indexDir"
    Invoke-Maven @('-q', 'test', "-Dlucenenet.index.dir=$indexDir")
}

Write-Host ""
Write-Host "==> Direction 2: Java writes, .NET reads"
Write-Host "    Java writing index into $JavaIndex"
Invoke-Maven @('-q', 'compile', 'exec:java', "-Dexec.args=$JavaIndex")

Write-Host "    .NET reading from $JavaIndex"
[Environment]::SetEnvironmentVariable('lucenenet.compat.read.dir', $JavaIndex, 'Process')
try {
    Invoke-DotNetTest 'FullyQualifiedName~TestJavaCompatibility.TestReadJavaIndex'
} finally {
    [Environment]::SetEnvironmentVariable('lucenenet.compat.read.dir', $null, 'Process')
}

Write-Host ""
Write-Host "==> Both directions passed."
exit 0
