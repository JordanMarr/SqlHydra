module UnitTests.``Infix Operators``

open System
open System.Diagnostics
open System.Reflection
open NUnit.Framework
open Swensen.Unquote
open SqlHydra.Query

[<Test>]
let ``a DLL that loads while the startup scan is running still gets scanned``() =
    let extension = typeof<SqlHydraInfixOperatorAttribute>.Assembly
    let scanned = ResizeArray()
    let mutable raiseLoad = ignore
    InfixOperators.watch
        (fun handler -> raiseLoad <- handler)
        // The DLL loads during the scan: the event fires, but the DLL isn't in the list.
        (fun () -> raiseLoad extension; [||])
        scanned.Add
    List.ofSeq scanned =! [ extension ]

/// The first scan runs once per process, so InfixRace.App runs it in a fresh one.
[<Test>]
let ``on the real runtime, a DLL that loads during the first scan registers its operator``() =
    let appPath =
        Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
        |> Seq.find (fun a -> a.Key = "InfixRaceApp")
    let dotnet = Environment.GetEnvironmentVariable "DOTNET_HOST_PATH" |> Option.ofObj |> Option.defaultValue "dotnet"
    use app = Process.Start(ProcessStartInfo(dotnet, $"\"{appPath.Value}\"", RedirectStandardOutput = true))
    if not (app.WaitForExit 60_000) then
        app.Kill()
        Assert.Fail "InfixRace.App did not exit within 60 seconds."
    app.StandardOutput.ReadToEnd().Trim() =! "SELECT (\"p\".\"x\" <~> \"p\".\"y\") FROM \"Program\".\"Point\" AS \"p\""
