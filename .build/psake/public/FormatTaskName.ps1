function FormatTaskName {
    <#
        .SYNOPSIS
        This function allows you to change how psake renders the task name during a build.

        .DESCRIPTION
        This function takes either a string which represents a format string (formats using the -f format operator see "help about_operators") or it can accept a script block that has a single parameter that is the name of the task that will be executed.

        .PARAMETER format
        A format string or a scriptblock to execute

        .EXAMPLE
        A sample build script that uses a format string is shown below:

        Task default -depends TaskA, TaskB, TaskC

        FormatTaskName "-------- {0} --------"

        Task TaskA {
        "TaskA is executing"
        }

        Task TaskB {
        "TaskB is executing"
        }

        Task TaskC {
        "TaskC is executing"

        -----------
        The script above produces the following output:

        -------- TaskA --------
        TaskA is executing
        -------- TaskB --------
        TaskB is executing
        -------- TaskC --------
        TaskC is executing

        Build Succeeded!
        .EXAMPLE
        A sample build script that uses a ScriptBlock is shown below:

        Task default -depends TaskA, TaskB, TaskC

        FormatTaskName {
            param($taskName)
            write-host "Executing Task: $taskName" -foregroundcolor blue
        }

        Task TaskA {
        "TaskA is executing"
        }

        Task TaskB {
        "TaskB is executing"
        }

        Task TaskC {
        "TaskC is executing"
        }

        -----------
        The above example uses the scriptblock parameter to the FormatTaskName function to render each task name in the color blue.

        Note: the $taskName parameter is arbitrary, it could be named anything.
        .LINK
        Assert
        .LINK
        Exec
        .LINK
        Framework
        .LINK
        Get-PSakeScriptTasks
        .LINK
        Include
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
        $format
    )

    $psake.context.Peek().config.taskNameFormat = $format
}
