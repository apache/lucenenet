function BuildTearDown {
    <#
        .SYNOPSIS
        Adds a scriptblock that will be executed once at the end of the build
        .DESCRIPTION
        This function will accept a scriptblock that will be executed once at the end of the build, regardless of success or failure
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
        BuildTearDown {
            "Running 'BuildTearDown'"
        }
        The script above produces the following output:
        Executing task, Clean...
        Executing task, Compile...
        Executing task, Test...
        Running 'BuildTearDown'
        Build Succeeded
        .EXAMPLE
        A failing build script is shown below:
        Task default -depends Test
        Task Test -depends Compile, Clean {
            throw "forced error"
        }
        Task Compile -depends Clean {
        }
        Task Clean {
        }
        BuildTearDown {
            "Running 'BuildTearDown'"
        }
        The script above produces the following output:
        Executing task, Clean...
        Executing task, Compile...
        Executing task, Test...
        Running 'BuildTearDown'
        forced error
        At line:x char:x ...
        .LINK
        Assert
        .LINK
        Exec
        .LINK
        FormatTaskName
        .LINK
        Framework
        .LINK
        Invoke-psake
        .LINK
        Properties
        .LINK
        Task
        .LINK
        BuildSetup
        .LINK
        TaskSetup
        .LINK
        TaskTearDown
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$setup
    )

    $psake.context.Peek().buildTearDownScriptBlock = $setup
}