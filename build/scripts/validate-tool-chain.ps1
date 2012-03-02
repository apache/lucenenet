#
# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the "License"); you may not use this file except in compliance with
# the License.  You may obtain a copy of the License at
#  
# http://www.apache.org/licenses/LICENSE-2.0
#  
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
#
#
# NOTICE: This script could mess up your development box. Use with extreme 
# caution. Better yet, test this on a non-production env vm before attempting
# to use it on any box you consider important.
#
#
# This is essentially a devopts script for installing tools that are needed for
# the Lucene.NEt build scripts to run CI on windows. 
#
# The script could use some refactoring and has the need to 
# increase its functionality for version & better error checking.
# 
# However it is a good alternative to having to remember where to 
# certain download software packages from or what to execute after the install. 
#
# This could also be handy for setting up new dev machines on windows 8 previews. 
#
# When Co-App is finally released and is considered stable, we could potentially 
# replace this script with that. 
# http://coapp.org/

function Get-ScriptDirectory
{
	$script = (Get-Variable MyInvocation -Scope 1).Value
	Split-Path $script.MyCommand.Path
}

$cd = Get-ScriptDirectory;

$Folder64 = $Env:ProgramFiles;
$Folder32 = ${Env:ProgramFiles(x86)};

$RequireWin7_1 = $false;
$RequireFxCop10 = $false;
$RequireSHFB = $false;
$RequireNCover = $false;

echo $Folder32;

$FindWin7_1 = Test-Path ($Folder64 + "\Microsoft SDKs\Windows\v7.1");
if($FindWin7_1 -eq $false) {
	$FindWin7_1 = Test-Path ($Folder32 + "\Microsoft SDKs\Windows\v7.1");
}

if($FindWin7_1 -eq $false) {
	echo "Windows 7.1 SDK               ..Not installed in its expected location."; 
	$RequireWin7_1 = $true;
} else {
	echo "Windows 7.1 SDK               ..Found."; 
}

$FindFxCop10 = Test-Path($Folder32 + "\Microsoft Fxcop 10.0");

if($FindFxCop10 -eq $false) {
	echo "Fx Cop 10 is not installed in its expected location."; 
	$RequireFxCop10 = $true;
} else {
	echo "Fx Cop 10                     ..Found."; 
}

$FindNCover = Test-Path ($Folder32 + "\NCover\NCover.Console.exe");
if($FindNCover -eq $false) {
	echo "NCover is not installed in its expected location.";
	$RequireNCover = $true;
} else {
	echo "NCover                        ..Found."; 
}

$FindSHFB = Test-Path ($Folder32 + "\EWSoftware\Sandcastle Help File Builder");

if($FindSHFB -eq $false) {
	echo "Sandcastle Help File Builder is not installed in its expected location."; 
	$RequireSFHB = $true;
} else {
	echo "Sandcastle Help File Builder  ..Found.";     
}

Function PromptForSHFBInstall
{
	$process = read-host "Do you want to download and install SandCastle Help File Builder ? (Y) or (N)";
	if($process -eq "Y")
	{
		
		$license = Read-Host "Do you agree to reading and accepting the ms-pl license http://www.opensource.org/licenses/MS-PL ? (Y) or (N)";
		
		if($license -eq "Y")
		{
			[System.Reflection.Assembly]::LoadFrom((Join-Path ($cd) "..\..\lib\ICSharpCode\SharpZipLib\0.85\ICSharpCode.SharpZipLib.dll"));
			$zip = New-Object ICSharpCode.SharpZipLib.Zip.FastZip
			$client = new-object System.Net.WebClient;
			$SHFBUrl = "http://download.codeplex.com/Download?ProjectName=shfb&DownloadId=214182&FileTime=129456589216470000&Build=18101";
			$SHFBFileName = Join-Path $home Downloads\SHFBGuidedInstallation.zip;
			$SHFBFileNameExtract = Join-Path $home Downloads\SHFBGuidedInstallation;
			[System.Net.GlobalProxySelection]::Select = [System.Net.GlobalProxySelection]::GetEmptyWebProxy();
			trap { $error[0].Exception.ToString() } 
			
			$exists = Test-Path $SHFBFileName;
			if($exists -eq $false)
			{
				echo ("Downloading SHFB to " + $SHFBFileName);
				$client.DownloadFile($SHFBUrl,$SHFBFileName);
			}
			
			$exists = Test-Path $SHFBFileNameExtract;
			if($exists -eq $false)
			{
				echo ("Extracting SHFB to " + $SHFBFileNameExtract);
				$zip.ExtractZip($SHFBFileName, $SHFBFileNameExtract, $null);
			}

			
			echo ("Installing SHFB...");
			$installer = Join-Path $HOME Downloads\SHFBGuidedInstallation\SandCastleInstaller.exe
			
			
			trap [Exception] {
				echo $_.Exception.Message;
				return;
			}
		    & $installer | Out-Null
			
			if($LASTEXITCODE -eq 0) 
			{ 
				echo "SHFB was installed" ;
			} else {
				echo "SHFB installation failed.";
				return;
			}
			
			echo ("Deleting SHFB Zip");
			del $SHFBFileName;
			
			echo ("Deleteing Extracted Files...");
			del $SHFBFileNameExtract;
		} 
		else 
		{
			echo "SandCastle Help File Builder install aborted.";
		}
	}
}

Function PromptForWinSdk7_1Install 
{
	$process = read-host "Do you want to download and install Windows Sdk 7.1 ? (Y) or (N)";
	if($process -eq "Y")
	{	
		$client = new-object System.Net.WebClient;
		$WinSdk7_1Url = "http://download.microsoft.com/download/A/6/A/A6AC035D-DA3F-4F0C-ADA4-37C8E5D34E3D/winsdk_web.exe";
		$WinSdk7_1FileName = Join-Path $home Downloads\winsdk_web.exe;
		[System.Net.GlobalProxySelection]::Select = [System.Net.GlobalProxySelection]::GetEmptyWebProxy();
		trap { $error[0].Exception.ToString() } 
		
		$exists = Test-Path $WinSdk7_1FileName;
		if($exists -eq $false)
		{
			echo ("Downloading Win Sdk 7.1 to " + $WinSdk7_1FileName);
			$client.DownloadFile($WinSdk7_1Url,$WinSdk7_1FileName);
		}
		
		
		echo ("Installing Win Sdk 7.1  ...");
		$installer = $WinSdk7_1FileName;
		trap [Exception] {
			echo $_.Exception.Message;
			return;
		}
		
	    & $installer
		
		echo "Attempting to setup Win Sdk Version...";
		$verExe = "C:\Program Files\Microsoft SDKs\Windows\v7.1\Setup\WindowsSdkVer.exe";
		$verExeExists = Test-Path $verExe;
		
		
		echo "Say yes to the next next two prompts if you wish to set WindowsSdkVer to -version:v7.1 ...";
		if($verExeExists)
		{
			$p = [diagnostics.process]::Start($verExe, " -version:v7.1");
			
			trap [Exception] {
				echo ("Most likely this action was cancelled by you.: " + $_.Exception.Message);
				return;
			}
			
			$p.WaitForExit()  | out-null
			if($LASTEXITCODE -eq 0) 
			{ 
				echo "Win Sdk 7.1 was installed" ;
			} else {
				echo "Win Sdk 7.1 failed.";
				return;
			}
			
			
		} else {
			echo ($verExe + "was not found.")
		}
		
		echo ("Deleteing installer...");
		del $WinSdk7_1FileName;
		$RequireWin7_1 = $false;
			
	} 
	else 
	{
		echo "Win Sdk 7.1 install aborted.";
	}
	
}

Function PromptForFxCop10Install()
{
	$process = read-host "Do you want to install FxCop 10.0 (WinSdk 7.1 is required)? (Y) or (N)";
	if($process -eq "Y")
	{
		$fxCopExe = "C:\Program Files\Microsoft SDKs\Windows\v7.1\Bin\FXCop\FxCopSetup.exe";
		$fxCopExeExists = Test-Path $fxCopExe;
		
		if($fxCopExeExists -eq $true)
		{
			trap [Exception] {
				echo $_.Exception.Message;
				return;
			}
			& $fxCopExe;
		
		} else {
			echo ("The installer for fxcop 10 was not found at its expected location: " + $fxCopExe);
			return;
		}
	}
}

Function PromptForNCoverInstall()
{
	$process = read-host "NCover is not free, you are responsible for obtaining your own license. Do you want to install NCover ? (Y) or (N)";
	
	
	if($process -eq "Y")
	{
		$client = new-object System.Net.WebClient;
		$download = "http://downloads.ncover.com/NCover-x64-3.4.18.6937.msi";
		$downloadFileName = Join-Path ($home + "Downloads\NCover-x64-3.4.18.6937.msi");
		[System.Net.GlobalProxySelection]::Select = [System.Net.GlobalProxySelection]::GetEmptyWebProxy();
		trap { $error[0].Exception.ToString() } 
			
		
		$exists = Test-Path $downloadFileName;
		if($exists -eq $false)
		{
			echo ("Downloading NCover to " + $downloadFileName);
			$client.DownloadFile($download,$downloadFileName);
		}
	
		echo "Installing NCover...";
		trap [Exception] {
				echo $_.Exception.Message;
				return;
			}
		& $fxCopExe;
		
		echo "Deleting installer....";
		del $downloadFileName;
	}
}


if($RequireSFHB -eq $true)
{
	PromptForSHFBInstall;
}

if($RequireWin7_1 -eq $true)
{
	PromptForWinSdk7_1Install
}

if($RequireWin7_1 -eq $false -and $RequireFxCop10 -eq $true)
{
	PromptForFxCop10Install
}

if($RequireNCover -eq $true)
{
	PromptForNCoverInstall
}