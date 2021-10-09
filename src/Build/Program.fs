module Program

open System.IO
open Fake.IO
open Fake.Core
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.Core.TargetOperators

// Initialize FAKE context
Setup.context()

let path xs = Path.Combine(Array.ofList xs)
let slnRoot = Files.findParent __SOURCE_DIRECTORY__ "SqlHydra.sln";

let query = path [ slnRoot; "SqlHydra.Query" ]
let mssql = path [ slnRoot; "SqlHydra.SqlServer" ]
let npgsql = path [ slnRoot; "SqlHydra.Npgsql" ]
let sqlite = path [ slnRoot; "SqlHydra.Sqlite" ]
let tests = path [ slnRoot; "Tests" ]

let packages = [ query; mssql; npgsql; sqlite ]

Target.create "Restore" <| fun _ ->
    packages @ [ tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "restore", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not restore '{pkg}' package.")

Target.create "Build" <| fun _ ->
    packages @ [ tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "Tests" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "run --configuration Release", tests)
    if exitCode <> 0 then failwith "Failed while running server tests"

Target.create "Pack" <| fun _ ->
    packages
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "pack --configuration Release -o nupkg/Release", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}' package.'")

Target.create "Publish" <| fun _ ->
    let nugetKey =
        match Environment.environVarOrNone "NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a NUGET_KEY environmental variable"
    
    packages
    |> List.map (fun pkg -> pkg </> "nupkg")
    |> List.map (fun nupkg -> Shell.Exec(Tools.dotnet, $"nuget push {nupkg} -s nuget.org -k {nugetKey}"), nupkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not publish '{pkg}' package.'")

let dependencies = [
    "Restore" ==> "Build" ==> "Tests" ==> "Pack"
    "Restore" ==> "Build" ==> "Tests" ==> "Pack" ==> "Publish"
]

Target.runOrDefaultWithArguments "Pack"
