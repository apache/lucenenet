
function TaskTearDown {
    <#
        .SYNOPSIS
        Adds a scriptblock to the build that will be executed after each task

        .DESCRIPTION
        This function will accept a scriptblock that will be executed after each task in the build script.

        The scriptblock accepts an optional parameter which describes the Task being torn down.

        .PARAMETER teardown
        A scriptblock to execute

        .EXAMPLE
        A sample build script is shown below:

        Task default -depends Test

        Task Test -depends Compile, Clean {
        }

        Task Compile -depends Clean {
        }

        Task Clean {
        }

        TaskTearDown {
            "Running 'TaskTearDown' for task $context.Peek().currentTaskName"
        }

        The script above produces the following output:

        Executing task, Clean...
        Running 'TaskTearDown' for task Clean
        Executing task, Compile...
        Running 'TaskTearDown' for task Compile
        Executing task, Test...
        Running 'TaskTearDown' for task Test

        Build Succeeded

        .EXAMPLE
        A sample build script demonstrating access to the task context is shown below:

        Task default -depends Test

        Task Test -depends Compile, Clean {
        }

        Task Compile -depends Clean {
        }

        Task Clean {
        }

        TaskTearDown {
            param($task)

            if ($task.Success) {
                "Running 'TaskTearDown' for task $($task.Name) - success!"
            } else {
                "Running 'TaskTearDown' for task $($task.Name) - failed: $($task.ErrorMessage)"
            }
        }

        The script above produces the following output:

        Executing task, Clean...
        Running 'TaskTearDown' for task Clean - success!
        Executing task, Compile...
        Running 'TaskTearDown' for task Compile - success!
        Executing task, Test...
        Running 'TaskTearDown' for task Test - success!

        Build Succeeded

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
        Properties
        .LINK
        Task
        .LINK
        TaskSetup
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$teardown
    )

    $psake.context.Peek().taskTearDownScriptBlock = $teardown
}
