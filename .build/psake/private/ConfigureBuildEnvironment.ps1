
function ConfigureBuildEnvironment {
    if (!(Test-Path Variable:\IsWindows) -or $IsWindows) {
        $framework = $psake.context.peek().config.framework
        if ($framework -cmatch '^((?:\d+\.\d+)(?:\.\d+){0,1})(x86|x64){0,1}$') {
            $versionPart = $matches[1]
            $bitnessPart = $matches[2]
        }
        else {
            throw ($msgs.error_invalid_framework -f $framework)
        }
        $versions = $null
        $buildToolsVersions = $null
        switch ($versionPart) {
            '1.0' {
                $versions = @('v1.0.3705')
            }
            '1.1' {
                $versions = @('v1.1.4322')
            }
            '1.1.0' {
                $versions = @()
            }
            '2.0' {
                $versions = @('v2.0.50727')
            }
            '2.0.0' {
                $versions = @()
            }
            '3.0' {
                $versions = @('v2.0.50727')
            }
            '3.5' {
                $versions = @('v3.5', 'v2.0.50727')
            }
            '4.0' {
                $versions = @('v4.0.30319')
            }
            {($_ -eq '4.5') -or ($_ -eq '4.5.1') -or ($_ -eq '4.5.2')} {
                $versions = @('v4.0.30319')
                $buildToolsVersions = @('16.0', '15.0', '14.0', '12.0')
            }
            {($_ -eq '4.6') -or ($_ -eq '4.6.1') -or ($_ -eq '4.6.2')} {
                $versions = @('v4.0.30319')
                $buildToolsVersions = @('16.0', '15.0', '14.0')
            }
            {($_ -eq '4.7') -or ($_ -eq '4.7.1') -or ($_ -eq '4.7.2')} {
                $versions = @('v4.0.30319')
                $buildToolsVersions = @('16.0', '15.0')
            }
            '4.8' {
                $versions = @('v4.0.30319')
                $buildToolsVersions = @('16.0', '15.0')
            }

            default {
                throw ($msgs.error_unknown_framework -f $versionPart, $framework)
            }
        }

        $bitness = 'Framework'
        if ($versionPart -ne '1.0' -and $versionPart -ne '1.1') {
            switch ($bitnessPart) {
                'x86' {
                    $bitness = 'Framework'
                    $buildToolsKey = 'MSBuildToolsPath32'
                }
                'x64' {
                    $bitness = 'Framework64'
                    $buildToolsKey = 'MSBuildToolsPath'
                }
                { [string]::IsNullOrEmpty($_) } {
                    $ptrSize = [System.IntPtr]::Size
                    switch ($ptrSize) {
                        4 {
                            $bitness = 'Framework'
                            $buildToolsKey = 'MSBuildToolsPath32'
                        }
                        8 {
                            $bitness = 'Framework64'
                            $buildToolsKey = 'MSBuildToolsPath'
                        }
                        default {
                            throw ($msgs.error_unknown_pointersize -f $ptrSize)
                        }
                    }
                }
                default {
                    throw ($msgs.error_unknown_bitnesspart -f $bitnessPart, $framework)
                }
            }
        }

        $frameworkDirs = @()
        if ($null -ne $buildToolsVersions) {
            foreach($ver in $buildToolsVersions) {
                if ($ver -eq "15.0") {
                    if ($null -eq (Get-Module -Name VSSetup)) {
                        if ($null -eq (Get-Module -Name VSSetup -ListAvailable)) {
                            WriteColoredOutput ($msgs.warning_missing_vsssetup_module -f $ver) -foregroundcolor Yellow
                            continue
                        }

                        Import-Module VSSetup
                    }

                    # borrowed from nightroman https://github.com/nightroman/Invoke-Build
                    if ($vsInstances = Get-VSSetupInstance) {
                        $vs = @($vsInstances | Select-VSSetupInstance -Version '[15.0, 16.0)' -Require Microsoft.Component.MSBuild)
                        if ($vs) {
                            if ($buildToolsKey -eq 'MSBuildToolsPath32') {
                                $frameworkDirs += Join-Path ($vs[0].InstallationPath) MSBuild\15.0\Bin
                            }
                            else {
                                $frameworkDirs += Join-Path ($vs[0].InstallationPath) MSBuild\15.0\Bin\amd64
                            }
                        }

                        $vs = @($vsInstances | Select-VSSetupInstance -Version '[15.0, 16.0)' -Product Microsoft.VisualStudio.Product.BuildTools)
                        if ($vs) {
                            if ($buildToolsKey -eq 'MSBuildToolsPath32') {
                                $frameworkDirs += Join-Path ($vs[0].InstallationPath) MSBuild\15.0\Bin
                            }
                            else {
                                $frameworkDirs += Join-Path ($vs[0].InstallationPath) MSBuild\15.0\Bin\amd64
                            }
                        }
                    }
                    else {
                        if (!($root = ${env:ProgramFiles(x86)})) {$root = $env:ProgramFiles}
                        if (Test-Path -LiteralPath "$root\Microsoft Visual Studio\2017") {
                            if ($buildToolsKey -eq 'MSBuildToolsPath32') {
                                $rp = @(Resolve-Path "$root\Microsoft Visual Studio\2017\*\MSBuild\15.0\Bin" -ErrorAction SilentlyContinue)
                            }
                            else {
                                $rp = @(Resolve-Path "$root\Microsoft Visual Studio\2017\*\MSBuild\15.0\Bin\amd64" -ErrorAction SilentlyContinue)
                            }

                            if ($rp) {
                                $frameworkDirs += $rp[-1].ProviderPath
                            }
                        }
                    }
                }
                elseif ($ver -eq "16.0") {
                    if ($null -eq (Get-Module -Name VSSetup)) {
                        if ($null -eq (Get-Module -Name VSSetup -ListAvailable)) {
                            WriteColoredOutput ($msgs.warning_missing_vsssetup_module -f $ver) -foregroundcolor Yellow
                            continue
                        }

                        Import-Module VSSetup
                    }

                    # borrowed from nightroman https://github.com/nightroman/Invoke-Build
                    if ($vsInstances = Get-VSSetupInstance) {
                        $vs = @($vsInstances | Select-VSSetupInstance -Version '[16.0,)' -Require Microsoft.Component.MSBuild)
                        if ($vs) {
                            $frameworkDirs += Join-Path ($vs[0].InstallationPath) MSBuild\Current\Bin
                        }

                        $vs = @($vsInstances | Select-VSSetupInstance -Version '[16.0,)' -Product Microsoft.VisualStudio.Product.BuildTools)
                        if ($vs) {
                            $frameworkDirs += Join-Path ($vs[0].InstallationPath) MSBuild\Current\Bin
                        }
                    }
                    else {
                        if (!($root = ${env:ProgramFiles(x86)})) {$root = $env:ProgramFiles}
                        if (Test-Path -LiteralPath "$root\Microsoft Visual Studio\2019") {
                            $rp = @(Resolve-Path "$root\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin" -ErrorAction SilentlyContinue)
                            if ($rp) {
                                $frameworkDirs += $rp[-1].ProviderPath
                            }
                        }
                    }
                }
                elseif (Test-Path "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$ver") {
                    $frameworkDirs += (Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$ver" -Name $buildToolsKey).$buildToolsKey
                }
            }
        }

        $frameworkDirs = $frameworkDirs + @($versions | ForEach-Object { "$env:windir\Microsoft.NET\$bitness\$_\" })
        for ($i = 0; $i -lt $frameworkDirs.Count; $i++) {
            $dir = $frameworkDirs[$i]
            if ($dir -Match "\$\(Registry:HKEY_LOCAL_MACHINE(.*?)@(.*)\)") {
                $key = "HKLM:" + $matches[1]
                $name = $matches[2]
                $dir = (Get-ItemProperty -Path $key -Name $name).$name
                $frameworkDirs[$i] = $dir
            }
        }

        $frameworkDirs | ForEach-Object { Assert (test-path $_ -pathType Container) ($msgs.error_no_framework_install_dir_found -f $_)}

        $env:PATH = ($frameworkDirs -join ";") + ";$env:PATH"
    }

    # if any error occurs in a PS function then "stop" processing immediately
    # this does not effect any external programs that return a non-zero exit code
    $global:ErrorActionPreference = "Stop"
}
