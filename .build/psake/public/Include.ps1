function Include {
    <#
        .SYNOPSIS
        Include the functions or code of another powershell script file into the current build script's scope

        .DESCRIPTION
        A build script may declare an "includes" function which allows you to define a file containing powershell code to be included
        and added to the scope of the currently running build script. Code from such file will be executed after code from build script.

        .PARAMETER fileNamePathToInclude
        A string containing the path and name of the powershell file to include

        .EXAMPLE
        A sample build script is shown below:

        Include ".\build_utils.ps1"

        Task default -depends Test

        Task Test -depends Compile, Clean {
        }

        Task Compile -depends Clean {
        }

        Task Clean {
        }

        -----------
        The script above includes all the functions and variables defined in the ".\build_utils.ps1" script into the current build script's scope

        Note: You can have more than 1 "Include" function defined in the build script.

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
        Invoke-psake
        .LINK
        Properties
        .LINK
        Task
        .LINK
        TaskSetup
        .LINK
        TaskTearDown
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$fileNamePathToInclude
    )

    Assert (test-path $fileNamePathToInclude -pathType Leaf) ($msgs.error_invalid_include_path -f $fileNamePathToInclude)

    $psake.context.Peek().includes.Enqueue((Resolve-Path $fileNamePathToInclude));
}
