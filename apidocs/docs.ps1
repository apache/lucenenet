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
	[Parameter(Mandatory=$false)]
	[int]
	$ServeDocs = 0,
	[Parameter(Mandatory=$false)]
	[int]
	$Clean = 1,
	# LogLevel can be: Diagnostic, Verbose, Info, Warning, Error
	[Parameter(Mandatory=$false)]
	[string]
	$LogLevel = 'Info'
)

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (get-item $PSScriptFilePath).Directory.Parent.FullName;
$ApiDocsFolder = Join-Path -Path $RepoRoot -ChildPath "apidocs";
$ToolsFolder = Join-Path -Path $ApiDocsFolder -ChildPath "tools";
#ensure the /build/tools folder
New-Item $ToolsFolder -type directory -force

# Go get docfx.exe if we don't have it
$DocFxExe = "$ToolsFolder\docfx\docfx.exe"
$FileExists = Test-Path $DocFxExe 
If ($FileExists -eq $False) {
	Write-Host "Retrieving docfx..."
	$DocFxZip = "$ToolsFolder\docfx.zip"
	$SourceDocFx = "https://github.com/dotnet/docfx/releases/download/v2.19.2/docfx.zip"
	Invoke-WebRequest $SourceDocFx -OutFile $DocFxZip
	#unzip
	Expand-Archive $DocFxZip -DestinationPath (Join-Path -Path $ToolsFolder -ChildPath "docfx")
}

# delete anything that already exists
if ($Clean -eq 1) {
	Write-Host "Cleaning..."
	Remove-Item (Join-Path -Path $ApiDocsFolder "_site\*") -recurse
	Remove-Item (Join-Path -Path $ApiDocsFolder "obj\*") -recurse
	Remove-Item (Join-Path -Path $ApiDocsFolder "obj") -force 
	Remove-Item (Join-Path -Path $ApiDocsFolder "api\*") -exclude "*.md" -recurse -force
	# Remove-Item (Join-Path -Path $ApiDocsFolder "api") -force -ErrorAction SilentlyContinue
}

# NOTE: There's a ton of Lucene docs that we want to copy and re-format. I'm not sure if we can really automate this 
# in a great way since the docs seem to be in many places, for example:
# Home page - 	https://github.com/apache/lucene-solr/blob/branch_4x/lucene/site/xsl/index.xsl
# Wiki docs - 	https://wiki.apache.org/lucene-java/FrontPage?action=show&redirect=FrontPageEN - not sure where the source is for this
# Html pages - 	Example: https://github.com/apache/lucene-solr/blob/releases/lucene-solr/4.8.0/lucene/highlighter/src/java/org/apache/lucene/search/highlight/package.html - these seem to be throughout the source
#				For these ones, could we go fetch them and download all *.html files from Git?

$DocFxJson = Join-Path -Path $RepoRoot "apidocs\docfx.json"

Write-Host "Building metadata..."
& $DocFxExe metadata $DocFxJson -l "obj\docfx.log" --loglevel $LogLevel
if($?) { 
	if ($ServeDocs -eq 0){
		# build the output		
		Write-Host "Building docs..."
		& $DocFxExe build $DocFxJson -l "obj\docfx.log" --loglevel $LogLevel
	}
	else {
		# build + serve (for testing)
		Write-Host "starting website..."
		& $DocFxExe $DocFxJson --serve -l "obj\docfx.log" --loglevel $LogLevel
	}
}