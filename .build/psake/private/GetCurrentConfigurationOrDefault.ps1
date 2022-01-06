function GetCurrentConfigurationOrDefault() {
    if ($psake.context.count -gt 0) {
        return $psake.context.peek().config
    } else {
        return $psake.config_default
    }
}
