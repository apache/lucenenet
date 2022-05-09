# Parses and validates the command arguments and bootstraps the Psake build script with the cleaned values

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

function Get-NextArg([string[]]$arguments, [int]$i, [string]$argName) {
    $i++
    if ($arguments.Length -gt $i -and -not $($arguments[$i]).StartsWith('-')) {
        return $arguments[$i]
    } else {
        throw $("'$argName' requires a value to be passed as the next argument.")
    }
}

# Default values, if not supplied as args
[string]$packageVersion = ''
[string]$fileVersion = ''
[string]$configuration = 'Release'
[bool]$runTests = $false
[int]$maximumParallelJobs = 8

# If the version.props file exists at the repository root, it is used to "lock" the version
# to the current release (this happens in the official release distribution at
# https://dist.apache.org/repos/dist/release/lucenenet/). In this case, we will not process
# any version values that are passed, and the user will get an error. Note we don't do any
# validation to ensure it has the values we need to produce a build (that part is automated
# as part of the release).
[string]$versionPropsFile = "$PSScriptRoot/version.props"
[bool]$versionPropsExists = Test-Path $versionPropsFile

# Analyze the args that were passed and process them
for ([int]$i = 0; $i -lt $args.Length; $i++) {
    $arg = $args[$i]
    $loweredArg =  "$arg".ToLowerInvariant()
    
    if ($loweredArg -eq '-t' -or $loweredArg -eq '--test') {
        $runTests = $true
    } elseif ($loweredArg -eq '-mp' -or $loweredArg -eq '--maximum-parallel-jobs' -or $loweredArg -eq '--maximumparalleljobs') {
        [string]$mpjStr = Get-NextArg $args $i $arg
        [int]$mpjInt = $null
        if (-not [int]::TryParse($mpjStr, [ref]$mpjInt)) { throw $("The '$arg' value must be a 32 bit integer. Got: $mpjStr.") }
        $maximumParallelJobs = $mpjInt
        $i++
    } elseif ($loweredArg -eq '-pv' -or $loweredArg -eq '--package-version' -or $loweredArg -eq '--packageversion') {
        if ($versionPropsExists) { throw $("'$arg' is not valid when $versionPropsFile exists.") }
        $packageVersion = Get-NextArg $args $i $arg
        $i++
    } elseif ($loweredArg -eq '-fv' -or $loweredArg -eq '--file-version' -or $loweredArg -eq '--fileversion') {
        if ($versionPropsExists) { throw $("'$arg' is not valid when $versionPropsFile exists.") }
        $fileVersion = Get-NextArg $args $i $arg
        $i++
    } elseif ($loweredArg -eq '-config' -or $loweredArg -eq '--configuration') {
        $configuration = Get-NextArg $args $i $arg
        $i++
    } else {
        throw $("Unrecognized argument: '$arg'")
    }
}

# Build the call to the Psake script using the captured/default args
[string[]]$task = 'Default'
if ($runTests) {
    $task = 'Default','Test'
}
$parameters = @{}
$properties = @{}

$properties.maximumParallelJobs = $maximumParallelJobs

# If version.props exists, we must not prepare for build or backup, because
# we assume we are a release distribution.
$properties.prepareForBuild = -not $versionPropsExists
$properties.backupFiles = -not $versionPropsExists

if (-not [string]::IsNullOrWhiteSpace($packageVersion)) {
    $properties.packageVersion=$packageVersion
}
if (-not [string]::IsNullOrWhiteSpace($fileVersion)) {
    $properties.version=$fileVersion
}
if (-not [string]::IsNullOrWhiteSpace($configuration)) {
    $properties.configuration=$configuration
}

Import-Module "$PSScriptRoot/.build/psake/psake.psm1"
Invoke-Psake "$PSScriptRoot/.build/runbuild.ps1" -task $task -properties $properties -parameters $parameters