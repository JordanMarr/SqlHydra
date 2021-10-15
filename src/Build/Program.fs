module Program

open System.IO
open Fake.IO
open Fake.Core
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators

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

Target.create "Test" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "run --configuration Release", tests)
    if exitCode <> 0 then failwith "Failed while running server tests"

Target.create "Pack" <| fun _ ->
    packages
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "pack --configuration Release -o nupkg/Release", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}' package.'")

let version = "0.540.0-beta1"

Target.create "Publish" <| fun _ ->
    let nugetKey =
        match Environment.environVarOrNone "SQLHYDRA_NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a SQLHYDRA_NUGET_KEY environmental variable"
    
    let existingPackages = !! (slnRoot </> "**" </> "nupkg" </> "Release" </> $"*.{version}.nupkg")

    if existingPackages |> Seq.length = 0
    then printfn $"No packages were found for version '{version}'."

    existingPackages
    |> Seq.map (fun nupkg -> Shell.Exec(Tools.dotnet, $"nuget push {nupkg} -s nuget.org -k {nugetKey}"), nupkg)
    |> Seq.iter (fun (code, pkg) -> if code = 0 then printfn $"Published '{pkg}'." else failwith $"Could not publish '{pkg}' package. Error: {code}")

let dependencies = [
    "Restore" ==> "Build" ==> "Test" ==> "Pack" ==> "Publish"
]

Target.runOrDefaultWithArguments "Publish"
