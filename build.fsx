#r @"packages/FAKE/tools/FakeLib.dll"
open System
open Fake

let buildDir = "./build"

// from SO http://stackoverflow.com/questions/3065409/starting-a-process-synchronously-and-streaming-the-output
type ProcessResult = { exitCode : int; stdout : string; stderr : string }


    

Target "Clean" (fun _ -> 
    trace "Clean"
    CleanDir buildDir
)

Target "Build:Core" (fun _ ->
    //let errorCode = Shell.Exec "cmd","/c k build ./src/Lucene.Net.Core"
    //do something with the error code
    //()
    let out = (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- "cmd"
                info.Arguments <- "/c k build ./src/Lucene.Net.Core/"
            ) (TimeSpan.FromMinutes 5.0))
    let code = sprintf "%i" out.ExitCode
    trace code
)

Target "Build:TestFramework" (fun _ ->

    let out =  (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- "cmd"
                info.Arguments <- "/c k build ./test/Lucene.Net.TestFramework/"
            ) (TimeSpan.FromMinutes 5.0))

   
    let code = sprintf "%i" out.ExitCode
    trace code
)

Target "Build:Core:Tests" (fun _ ->

    let out = (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- "cmd"
                info.Arguments <- "/c k build ./test/Lucene.Net.Core.Tests/"
            ) (TimeSpan.FromMinutes 5.0))
    let code = sprintf "%i" out.ExitCode
    trace code
)

Target "Test:Core" (fun _ ->
    let out = (ExecProcessAndReturnMessages(fun info ->
                info.FileName <- "cmd"
                info.Arguments <- "/c k test"
                info.WorkingDirectory <- "./test/Lucene.Net.Core.Tests/"
            ) (TimeSpan.FromMinutes 5.0))
   
    let code = sprintf "%i" out.ExitCode
    trace code
    //trace result.stdout
    //trace result.stderr
)

Target "Default" (fun _ ->
    trace "Hello Word"
)

"Clean"
    ==> "Build:Core"
    ==> "Build:TestFramework"
    ==> "Build:Core:Tests"
    ==> "Test:Core"
    ==> "Default"

RunTargetOrDefault "Default"
