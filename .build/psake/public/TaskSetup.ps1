function TaskSetup {
    <#
        .SYNOPSIS
        Adds a scriptblock that will be executed before each task

        .DESCRIPTION
        This function will accept a scriptblock that will be executed before each task in the build script.

        The scriptblock accepts an optional parameter which describes the Task being setup.

        .PARAMETER setup
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

        TaskSetup {
            "Running 'TaskSetup' for task $context.Peek().currentTaskName"
        }

        The script above produces the following output:

        Running 'TaskSetup' for task Clean
        Executing task, Clean...
        Running 'TaskSetup' for task Compile
        Executing task, Compile...
        Running 'TaskSetup' for task Test
        Executing task, Test...

        Build Succeeded

        .EXAMPLE
        A sample build script showing access to the Task context is shown below:

        Task default -depends Test

        Task Test -depends Compile, Clean {
        }

        Task Compile -depends Clean {
        }

        Task Clean {
        }

        TaskSetup {
            param($task)

            "Running 'TaskSetup' for task $($task.Name)"
        }

        The script above produces the following output:

        Running 'TaskSetup' for task Clean
        Executing task, Clean...
        Running 'TaskSetup' for task Compile
        Executing task, Compile...
        Running 'TaskSetup' for task Test
        Executing task, Test...

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
        TaskTearDown
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$setup
    )

    $psake.context.Peek().taskSetupScriptBlock = $setup
}
