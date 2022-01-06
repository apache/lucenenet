function Assert {
    <#
        .SYNOPSIS
        Helper function for "Design by Contract" assertion checking.

        .DESCRIPTION
        This is a helper function that makes the code less noisy by eliminating many of the "if" statements that are normally required to verify assumptions in the code.

        .PARAMETER conditionToCheck
        The boolean condition to evaluate

        .PARAMETER failureMessage
        The error message used for the exception if the conditionToCheck parameter is false

        .EXAMPLE
        C:\PS>Assert $false "This always throws an exception"

        Example of an assertion that will always fail.

        .EXAMPLE
        C:\PS>Assert ( ($i % 2) -eq 0 ) "$i is not an even number"

        This exmaple may throw an exception if $i is not an even number

        Note:
        It might be necessary to wrap the condition with paranthesis to force PS to evaluate the condition
        so that a boolean value is calculated and passed into the 'conditionToCheck' parameter.

        Example:
            Assert 1 -eq 2 "1 doesn't equal 2"

        PS will pass 1 into the condtionToCheck variable and PS will look for a parameter called "eq" and
        throw an exception with the following message "A parameter cannot be found that matches parameter name 'eq'"

        The solution is to wrap the condition in () so that PS will evaluate it first.

        Assert (1 -eq 2) "1 doesn't equal 2"
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
        .LINK
        TaskTearDown
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $conditionToCheck,

        [Parameter(Mandatory = $true)]
        [string]$failureMessage
    )

    if (-not $conditionToCheck) {
        throw ('Assert: {0}' -f $failureMessage)
    }
}
