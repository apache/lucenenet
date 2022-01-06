function Invoke-psake {
    <#
        .SYNOPSIS
        Runs a psake build script.

        .DESCRIPTION
        This function runs a psake build script

        .PARAMETER buildFile
        The path to the psake build script to execute

        .PARAMETER taskList
        A comma-separated list of task names to execute

        .PARAMETER framework
        The version of the .NET framework you want to use during build. You can append x86 or x64 to force a specific framework.
        If not specified, x86 or x64 will be detected based on the bitness of the PowerShell process.
        Possible values: '1.0', '1.1', '2.0', '2.0x86', '2.0x64', '3.0', '3.0x86', '3.0x64', '3.5', '3.5x86', '3.5x64', '4.0', '4.0x86', '4.0x64', '4.5', '4.5x86', '4.5x64', '4.5.1', '4.5.1x86', '4.5.1x64'

        .PARAMETER docs
        Prints a list of tasks and their descriptions

        .PARAMETER parameters
        A hashtable containing parameters to be passed into the current build script.
        These parameters will be processed before the 'Properties' function of the script is processed.
        This means you can access parameters from within the 'Properties' function!

        .PARAMETER properties
        A hashtable containing properties to be passed into the current build script.
        These properties will override matching properties that are found in the 'Properties' function of the script.

        .PARAMETER initialization
        Parameter description

        .PARAMETER nologo
        Do not display the startup banner and copyright message.

        .PARAMETER detailedDocs
        Prints a more descriptive list of tasks and their descriptions.

        .PARAMETER notr
        Do not display the time report.

        .EXAMPLE
        Invoke-psake

        Runs the 'default' task in the '.build.ps1' build script

        .EXAMPLE
        Invoke-psake '.\build.ps1' Tests,Package

        Runs the 'Tests' and 'Package' tasks in the '.build.ps1' build script

        .EXAMPLE
        Invoke-psake Tests

        This example will run the 'Tests' tasks in the 'psakefile.ps1' build script. The 'psakefile.ps1' is assumed to be in the current directory.

        .EXAMPLE
        Invoke-psake 'Tests, Package'

        This example will run the 'Tests' and 'Package' tasks in the 'psakefile.ps1' build script. The 'psakefile.ps1' is assumed to be in the current directory.

        .EXAMPLE
        Invoke-psake .\build.ps1 -docs

        Prints a report of all the tasks and their dependencies and descriptions and then exits

        .EXAMPLE
        Invoke-psake .\parameters.ps1 -parameters @{"p1"="v1";"p2"="v2"}

        Runs the build script called 'parameters.ps1' and passes in parameters 'p1' and 'p2' with values 'v1' and 'v2'

        Here's the .\parameters.ps1 build script:

        properties {
            $my_property = $p1 + $p2
        }

        task default -depends TestParams

        task TestParams {
            Assert ($my_property -ne $null) '$my_property should not be null'
        }

        Notice how you can refer to the parameters that were passed into the script from within the "properties" function.
        The value of the $p1 variable should be the string "v1" and the value of the $p2 variable should be "v2".

        .EXAMPLE
        Invoke-psake .\properties.ps1 -properties @{"x"="1";"y"="2"}

        Runs the build script called 'properties.ps1' and passes in parameters 'x' and 'y' with values '1' and '2'

        This feature allows you to override existing properties in your build script.

        Here's the .\properties.ps1 build script:

        properties {
            $x = $null
            $y = $null
            $z = $null
        }

        task default -depends TestProperties

        task TestProperties {
            Assert ($x -ne $null) "x should not be null"
            Assert ($y -ne $null) "y should not be null"
            Assert ($z -eq $null) "z should be null"
        }

        .NOTES
        ---- Exceptions ----

        If there is an exception thrown during the running of a build script psake will set the '$psake.build_success' variable to $false.
        To detect failue outside PowerShell (for example by build server), finish PowerShell process with non-zero exit code when '$psake.build_success' is $false.
        Calling psake from 'cmd.exe' with 'psake.cmd' will give you that behaviour.

        ---- $psake variable ----

        When the psake module is loaded a variable called $psake is created which is a hashtable
        containing some variables:

        $psake.version                      # contains the current version of psake
        $psake.context                      # holds onto the current state of all variables
        $psake.run_by_psake_build_tester    # indicates that build is being run by psake-BuildTester
        $psake.config_default               # contains default configuration
                                            # can be overriden in psake-config.ps1 in directory with psake.psm1 or in directory with current build script
        $psake.build_success                # indicates that the current build was successful
        $psake.build_script_file            # contains a System.IO.FileInfo for the current build script
        $psake.build_script_dir             # contains the fully qualified path to the current build script
        $psake.error_message                # contains the error message which caused the script to fail

        You should see the following when you display the contents of the $psake variable right after importing psake

        PS projects:\psake\> Import-Module .\psake.psm1
        PS projects:\psake\> $psake

        Name                           Value
        ----                           -----
        run_by_psake_build_tester      False
        version                        4.2
        build_success                  False
        build_script_file
        build_script_dir
        config_default                 @{framework=3.5; ...
        context                        {}
        error_message

        After a build is executed the following $psake values are updated: build_script_file, build_script_dir, build_success

        PS projects:\psake\> Invoke-psake .\examples\psakefile.ps1
        Executing task: Clean
        Executed Clean!
        Executing task: Compile
        Executed Compile!
        Executing task: Test
        Executed Test!

        Build Succeeded!

        ----------------------------------------------------------------------
        Build Time Report
        ----------------------------------------------------------------------
        Name    Duration
        ----    --------
        Clean   00:00:00.0798486
        Compile 00:00:00.0869948
        Test    00:00:00.0958225
        Total:  00:00:00.2712414

        PS projects:\psake\> $psake

        Name                           Value
        ----                           -----
        build_script_file              YOUR_PATH\examples\psakefile.ps1
        run_by_psake_build_tester      False
        build_script_dir               YOUR_PATH\examples
        context                        {}
        version                        4.2
        build_success                  True
        config_default                 @{framework=3.5; ...
        error_message

        .LINK
        Assert
        .LINK
        Exec
        .LINK
        FormatTaskName
        .LINK
        Framework
        .LINK
        Get-PSakeScriptTasks
        .LINK
        Include
        .LINK
        Properties
        .LINK
        Task
        .LINK
        TaskSetup
        .LINK
        TaskTearDown
        .LINK
        Properties
    #>
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = $false)]
        [string]$buildFile,

        [Parameter(Position = 1, Mandatory = $false)]
        [string[]]$taskList = @(),

        [Parameter(Position = 2, Mandatory = $false)]
        [string]$framework,

        [Parameter(Position = 3, Mandatory = $false)]
        [switch]$docs = $false,

        [Parameter(Position = 4, Mandatory = $false)]
        [hashtable]$parameters = @{},

        [Parameter(Position = 5, Mandatory = $false)]
        [hashtable]$properties = @{},

        [Parameter(Position = 6, Mandatory = $false)]
        [alias("init")]
        [scriptblock]$initialization = {},

        [Parameter(Position = 7, Mandatory = $false)]
        [switch]$nologo,

        [Parameter(Position = 8, Mandatory = $false)]
        [switch]$detailedDocs,

        [Parameter(Position = 9, Mandatory = $false)]
        [switch]$notr # disable time report
    )

    try {
        if (-not $nologo) {
            "psake version {0}$($script:nl)Copyright (c) 2010-2018 James Kovacs & Contributors$($script:nl)" -f $psake.version
        }
        if (!$buildFile) {
           $buildFile = Get-DefaultBuildFile
        }
        elseif (!(Test-Path $buildFile -PathType Leaf) -and ($null -ne (Get-DefaultBuildFile -UseDefaultIfNoneExist $false))) {
            # If the default file exists and the given "buildfile" isn't found assume that the given
            # $buildFile is actually the target Tasks to execute in the $config.buildFileName script.
            $taskList = $buildFile.Split(', ')
            $buildFile = Get-DefaultBuildFile
        }

        $psake.error_message = $null

        ExecuteInBuildFileScope $buildFile $MyInvocation.MyCommand.Module {
            param($currentContext, $module)

            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

            if ($docs -or $detailedDocs) {
                WriteDocumentation($detailedDocs)
                return
            }

            try {
                foreach ($key in $parameters.keys) {
                    if (test-path "variable:\$key") {
                        set-item -path "variable:\$key" -value $parameters.$key -WhatIf:$false -Confirm:$false | out-null
                    } else {
                        new-item -path "variable:\$key" -value $parameters.$key -WhatIf:$false -Confirm:$false | out-null
                    }
                }
            } catch {
                WriteColoredOutput "Parameter '$key' is null" -foregroundcolor Red
                throw
            }

            # The initial dot (.) indicates that variables initialized/modified in the propertyBlock are available in the parent scope.
            while ($currentContext.properties.Count -gt 0) {
                $propertyBlock = $currentContext.properties.Pop()
                . $propertyBlock
            }

            foreach ($key in $properties.keys) {
                if (test-path "variable:\$key") {
                    set-item -path "variable:\$key" -value $properties.$key -WhatIf:$false -Confirm:$false | out-null
                }
            }

            # Simple dot sourcing will not work. We have to force the script block into our
            # module's scope in order to initialize variables properly.
            . $module $initialization

            & $currentContext.buildSetupScriptBlock

            # Execute the list of tasks or the default task
            try {
                if ($taskList) {
                    foreach ($task in $taskList) {
                        invoke-task $task
                    }
                } elseif ($currentContext.tasks.default) {
                    invoke-task default
                } else {
                    throw $msgs.error_no_default_task
                }
            }
            finally {
                & $currentContext.buildTearDownScriptBlock
            }

            $successMsg = $msgs.psake_success -f $buildFile
            WriteColoredOutput ("$($script:nl)${successMsg}$($script:nl)") -foregroundcolor Green

            $stopwatch.Stop()
            if (-not $notr) {
                WriteTaskTimeSummary $stopwatch.Elapsed
            }
        }

        $psake.build_success = $true

    } catch {
        $psake.build_success = $false
        $psake.error_message = FormatErrorMessage $_

        # if we are running in a nested scope (i.e. running a psake script from a psake script) then we need to re-throw the exception
        # so that the parent script will fail otherwise the parent script will report a successful build
        $inNestedScope = ($psake.context.count -gt 1)
        if ( $inNestedScope ) {
            throw $_
        } else {
            if (!$psake.run_by_psake_build_tester) {
                WriteColoredOutput $psake.error_message -foregroundcolor Red
            }
        }
    } finally {
        CleanupEnvironment
    }
}
