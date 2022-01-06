function WriteColoredOutput {
    param(
        [string] $message,
        [System.ConsoleColor] $foregroundcolor
    )

    $currentConfig = GetCurrentConfigurationOrDefault
    if ($currentConfig.coloredOutput -eq $true) {
        if (($null -ne $Host.UI) -and ($null -ne $Host.UI.RawUI) -and ($null -ne $Host.UI.RawUI.ForegroundColor)) {
            $previousColor = $Host.UI.RawUI.ForegroundColor
            $Host.UI.RawUI.ForegroundColor = $foregroundcolor
        }
    }

    $message

    if ($null -ne $previousColor) {
        $Host.UI.RawUI.ForegroundColor = $previousColor
    }
}
