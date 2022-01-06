<#
.SYNOPSIS
    Validate that the version of a module passed in via the $currentVersion
    parameter is valid based on the criteria specified by the following
    parameters.
.DESCRIPTION
    This function is used to determine whether or not a given module is within
    the version bounds specified by the parameters passed in. Psake will use
    this information to determine if the module it has found will contain the
    proper version of the shared task it has been asked to import.

    This function should allow bounds that are only on the lower limit, only on
    the upper, within a range, or if no bounds are supplied, the current module
    will be accepted without question.
.PARAMETER currentVersion
    The version of the module in the current session to be subjected to comparison
.PARAMETER minimumVersion
    The lower bound of the version that will be accepted. This comparison should
    be inclusive, meaning an input version greater than or equal to this version
    should be accepted.
.PARAMETER maximumVersion
    The upper bound of the version that will be accepted. This comparison should
    be inclusive, meaning an input version less than or equal to this version
    should be accepted.
.PARAMETER lessThanVersion
    The upper bound of the version that will be accepted. This comparison should
    be exlusive. Meaning an input version that is less than only, not equal to
    this version, will be accepted.
.INPUTS
    A $currentVersion of type [System.Version] or a convertable string.
    A set of version criteria, each of type [System.Version] or a convertable string.
.OUTPUTS
    boolean - Pass/Fail
#>
function Test-ModuleVersion {
    [CmdletBinding()]
    param (
        [string]$currentVersion,
        [string]$minimumVersion,
        [string]$maximumVersion,
        [string]$lessThanVersion
    )

    begin {
    }

    process {
        $result = $true

        # If no version is specified simply return true and allow the module to pass.
        if("$minimumVersion$maximumVersion$lessthanVersion" -eq ''){
            return $true
        }

        # Single integer values cannot be converted to type system.version.
        # We convert to a string, and if there is a single character we know that
        # we need to add a '.0' to the integer to make it convertable to a version.
        if(![string]::IsNullOrEmpty($currentVersion)) {
            if($currentVersion.ToString().Length -eq 1) {
                [version]$currentVersion = "$currentVersion.0"
            } else {
                [version]$currentVersion = $currentVersion
            }
        }

        if(![string]::IsNullOrEmpty($minimumVersion)) {
            if($minimumVersion.ToString().Length -eq 1){
                [version]$minimumVersion = "$minimumVersion.0"
            } else {
                [version]$minimumVersion = $minimumVersion
            }

            if($currentVersion.CompareTo($minimumVersion) -lt 0){
                $result = $false
            }
        }

        if(![string]::IsNullOrEmpty($maximumVersion)) {
            if($maximumVersion.ToString().Length -eq 1) {
                [version]$maximumVersion = "$maximumVersion.0"
            } else {
                [version]$maximumVersion = $maximumVersion
            }

            if ($currentVersion.CompareTo($maximumVersion) -gt 0) {
                $result = $false
            }
        }

        if(![string]::IsNullOrEmpty($lessThanVersion)) {
            if($lessThanVersion.ToString().Length -eq 1) {
                [version]$lessThanVersion = "$lessThanVersion.0"
            } else {
                [version]$lessThanVersion = $lessThanVersion
            }

            if($currentVersion.CompareTo($lessThanVersion) -ge 0) {
                $result = $false
            }
        }

        Write-Output $result
    }

    end {
    }
}
