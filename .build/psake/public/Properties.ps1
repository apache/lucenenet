function Properties {
    <#
        .SYNOPSIS
        Define a scriptblock that contains assignments to variables that will be available to all tasks in the build script

        .DESCRIPTION
        A build script may declare a "Properies" function which allows you to define variables that will be available to all the "Task" functions in the build script.

        .PARAMETER properties
        The script block containing all the variable assignment statements

        .EXAMPLE
        A sample build script is shown below:

        Properties {
            $build_dir = "c:\build"
            $connection_string = "datasource=localhost;initial catalog=northwind;integrated security=sspi"
        }

        Task default -depends Test

        Task Test -depends Compile, Clean {
        }

        Task Compile -depends Clean {
        }

        Task Clean {
        }

        Note: You can have more than one "Properties" function defined in the build script.

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
        Invoke-psake
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
        [scriptblock]$properties
    )

    $psake.context.Peek().properties.Push($properties)
}
