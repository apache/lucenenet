function WriteDocumentation($showDetailed) {

        $currentContext = $psake.context.Peek()

        if ($currentContext.tasks.default) {
            $defaultTaskDependencies = $currentContext.tasks.default.DependsOn
        } else {
            $defaultTaskDependencies = @()
        }

        $docs = GetTasksFromContext $currentContext |
                    Where-Object   {$_.Name -ne 'default'} |
                    ForEach-Object {
                        $isDefault = $null
                        if ($defaultTaskDependencies -contains $_.Name) {
                            $isDefault = $true
                        }
                        return Add-Member -InputObject $_ 'Default' $isDefault -PassThru
                    }

        if ($showDetailed) {
            $docs | Sort-Object 'Name' | format-list -property Name,Alias,Description,@{Label="Depends On";Expression={$_.DependsOn -join ', '}},Default
        } else {
            $docs | Sort-Object 'Name' | format-table -autoSize -wrap -property Name,Alias,@{Label="Depends On";Expression={$_.DependsOn -join ', '}},Default,Description
        }
    }
