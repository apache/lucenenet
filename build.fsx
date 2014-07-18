#r @"packages/FAKE/tools/FakeLib.dll"

open Fake

let buildDir = "./build"

// from SO http://stackoverflow.com/questions/3065409/starting-a-process-synchronously-and-streaming-the-output
type ProcessResult = { exitCode : int; stdout : string; stderr : string }

let executeProcess (exe, cmdline, wd) =
    let psi = new System.Diagnostics.ProcessStartInfo(exe,cmdline) 
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.CreateNoWindow <- true
    if wd <> null then psi.WorkingDirectory <- wd    
    let p = System.Diagnostics.Process.Start(psi) 
    let output = new System.Text.StringBuilder()
    let error = new System.Text.StringBuilder()
    p.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
    p.ErrorDataReceived.Add(fun args -> error.Append(args.Data) |> ignore)
    p.BeginErrorReadLine()
    p.BeginOutputReadLine()
    p.WaitForExit()
    { exitCode = p.ExitCode; stdout = output.ToString(); stderr = error.ToString() }

Target "Clean" (fun _ -> 
    trace "Clean"
    CleanDir buildDir
)

Target "Build:Core" (fun _ ->
    //let errorCode = Shell.Exec "cmd","/c k build ./src/Lucene.Net.Core"
    //do something with the error code
    //()
    let result = executeProcess("cmd", "/c k build ./src/Lucene.Net.Core/", null)
    trace result.stdout;
)

Target "Build:TestFramework" (fun _ ->
    let result = executeProcess("cmd", "/c k build ./test/Lucene.Net.TestFramework/", null)
    trace result.stdout;
)

Target "Build:Core:Tests" (fun _ ->
    let result = executeProcess("cmd", "/c k build ./test/Lucene.Net.Core.Tests/", null)
    trace result.stdout;
)

Target "Test:Core" (fun _ ->
    let result = executeProcess("cmd", "/c k test", "./test/Lucene.Net.Core.Tests/")
    trace ""
    trace result.stdout
    trace result.stderr
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
