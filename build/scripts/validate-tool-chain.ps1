@'
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at
  
 http://www.apache.org/licenses/LICENSE-2.0
  
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
 
'@

function DirExists($path) { 
 
    if ([IO.Directory]::Exists($path)) 
    { 
        return $true; 
    } 
    else 
    { 
        return $false; 
    } 
} 

$Folder64 = $Env:ProgramFiles;
$Folder32 = ${Env:ProgramFiles(x86)};

$RequireWin7_1 = $false;
$RequireFxCop10 = $false;
$RequireSHFB = $false;

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


$FindSHFB = Test-Path ($Folder32 + "\EWSoftware\Sandcastle Help File Builder");

if($FindSHFB -eq $false) {
	echo "Sandcastle Help File Builder is not installed in its expected location."; 
	$RequireSFHB = $true;
} else {
	echo "Sandcastle Help File Builder  ..Found."; 
     
}
