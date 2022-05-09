function Framework {
    <#
    .SYNOPSIS
    Sets the version of the .NET framework you want to use during build.

    .DESCRIPTION
    This function will accept a string containing version of the .NET framework to use during build.
    Possible values: '1.0', '1.1', '2.0', '2.0x86', '2.0x64', '3.0', '3.0x86', '3.0x64', '3.5', '3.5x86', '3.5x64', '4.0', '4.0x86', '4.0x64', '4.5', '4.5x86', '4.5x64', '4.5.1', '4.5.1x86', '4.5.1x64'.
    Default is '3.5*', where x86 or x64 will be detected based on the bitness of the PowerShell process.

    .PARAMETER framework
    Version of the .NET framework to use during build.

    .EXAMPLE
    Framework "4.0"

    Task default -depends Compile

    Task Compile -depends Clean {
        msbuild /version
    }

    -----------
    The script above will output detailed version of msbuid v4
    .LINK
    Assert
    .LINK
    Exec
    .LINK
    FormatTaskName
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
        [string]$framework
    )

    $psake.context.Peek().config.framework = $framework

    ConfigureBuildEnvironment
}
