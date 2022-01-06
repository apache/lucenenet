function GetTasksFromContext($currentContext) {

    $docs = $currentContext.tasks.Keys | foreach-object {

        $task = $currentContext.tasks.$_
        new-object PSObject -property @{
            Name = $task.Name;
            Alias = $task.Alias;
            Description = $task.Description;
            DependsOn = $task.DependsOn;
        }
    }

    return $docs
}
