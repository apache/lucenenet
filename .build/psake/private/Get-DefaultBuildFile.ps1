# Attempt to find the default build file given the config_default of
# buildFileName and legacyBuildFileName.  If neither exist optionally
# return the buildFileName or $null
function Get-DefaultBuildFile {
    param(
        [boolean] $UseDefaultIfNoneExist = $true
    )

    if (test-path $psake.config_default.buildFileName -pathType Leaf) {
        Write-Output $psake.config_default.buildFileName
    } elseif (test-path $psake.config_default.legacyBuildFileName -pathType Leaf) {
        Write-Warning "The default configuration file of default.ps1 is deprecated.  Please use psakefile.ps1"
        Write-Output $psake.config_default.legacyBuildFileName
    } elseif ($UseDefaultIfNoneExist) {
        Write-Output $psake.config_default.buildFileName
    }
}
