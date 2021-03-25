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

# TODO: Convert this script to use the https://github.com/NightOwl888/lucenenet-javadoc2markdown CLI
# See https://github.com/apache/lucenenet/issues/396#issuecomment-734417702

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

Remove-Item  -Recurse -Force "$ToolsFolder\tmp"

# TODO: Execute the JavaDocToMarkdownConverter dotnet tool
# TODO: Validate that this only executes within a git branch: `docs/markdown-converted/*`
#