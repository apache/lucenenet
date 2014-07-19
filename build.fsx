#r @"packages/FAKE/tools/FakeLib.dll"
open System
open Fake

let buildDir = "./build"

let exe = 
    if isMono then "/bin/bash"
        else "cmd"

let ci =
    System.Environment.GetEnvironmentVariable("CI")

let isCi =
    if ci <> null then true
    else false

Target "Clean" (fun _ -> 
    trace "Clean"
    CleanDir buildDir
)

Target "Restore" (fun _ ->
    
    let exe = 
        if isMono then "/bin/bash"
        else "cmd"
    
    let command = 
        if isMono then "-c 'kpm restore -s https://www.myget.org/F/aspnetvnext'"
        else "/c kpm restore -s https://www.myget.org/F/aspnetvnext"

    let out = (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- exe
                info.Arguments <- command
            ) (TimeSpan.FromMinutes 5.0))
    
    let exitCode = out.ExitCode
    
    if not (exitCode = 0 || exitCode = 100 || exitCode = 255) then 
        failwithf "Unable to Restore Packages: %i" exitCode
)

Target "Build:Core" (fun _ ->

    let command = 
        if isMono then "-c 'cd ./src/Lucene.Net.Core/ && k build'"
        else "/c k build ./src/Lucene.Net.Core/"

    // Shell.Exec(exe, )
    let out =  (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- exe
                info.Arguments <- command.ToString()
            ) (TimeSpan.FromMinutes 5.0))

    let exitCode = out.ExitCode
    
    if not (exitCode = 0 || exitCode = 100 || exitCode = 255) then 
        failwithf "Unable to Build Core: %i" exitCode
)

Target "Build:TestFramework" (fun _ ->

    
    let command = 
        if isMono then "-c 'cd ./test/Lucene.Net.TestFramework/ && k build'"
        else "/c k build ./test/Lucene.Net.TestFramework/"

    // Shell.Exec(exe, )
    let out =  (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- exe
                info.Arguments <- command.ToString()
            ) (TimeSpan.FromMinutes 5.0))
   
    let exitCode = out.ExitCode
    
    if not (exitCode = 0 || exitCode = 100 || exitCode = 255) then 
        failwithf "Unable to Build TestFramework: %i" exitCode
)

Target "Build:Core:Tests" (fun _ ->

        
    let command = 
        if isMono then "-c 'cd ./test/Lucene.Net.Core.Tests/ && k build'"
        else "/c k build ./test/Lucene.Net.Core.Tests/"

    // Shell.Exec(exe, )
    let out =  (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- exe
                info.Arguments <- command.ToString()
            ) (TimeSpan.FromMinutes 5.0))

    let exitCode = out.ExitCode
    
    if not (exitCode = 0 || exitCode = 100 || exitCode = 255) then 
        failwithf "Unable to Build Core Tests: %i" exitCode
)

Target "Test:Core" (fun _ ->

    let command = 
        if isMono then "-c 'cd ./test/Lucene.Net.Core.Tests/ && k test'"
        elif isCi then "/c cd test\\Lucene.Net.Core.Tests\\ & ..\\..\\k.cmd test"
        else "/c cd ./test/Lucene.Net.Core.Tests/ & k test"

    let out =  (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- exe
                info.Arguments <- command.ToString()
            ) (TimeSpan.FromMinutes 5.0))

    let exitCode = out.ExitCode
    
    if not (exitCode = 0 || exitCode = 100 || exitCode = 255) then 
        failwithf "Tests Failed: %i" exitCode
)

Target "Default" (fun _ ->
    trace "Done!"
)

"Clean"
    ==> "Restore"
    ==> "Build:Core"
    ==> "Build:TestFramework"
    ==> "Build:Core:Tests"
    ==> "Test:Core"
    ==> "Default"

RunTargetOrDefault "Default"