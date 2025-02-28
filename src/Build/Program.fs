module Program

open System.IO
open Fake.IO
open Fake.Core
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.Core.TargetOperators
open System.Xml.Linq

// Initialize FAKE context
Setup.context()

let slnRoot = Files.findParent __SOURCE_DIRECTORY__ "SqlHydra.sln";

let query = slnRoot </> "SqlHydra.Query"
let cli = slnRoot </> "SqlHydra.Cli"
let tests = slnRoot </> "Tests"

let allPackages = [ query; cli ]

Target.create "Restore" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "restore", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not restore '{pkg}' package.")

Target.create "BuildQuery" <| fun _ ->
    // SqlHydra.Query has to built separately since it is netstandard2.0
    query
    |> (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release", pkg), pkg)
    |> (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "BuildCliNet7" <| fun _ ->
    [ cli; ] 
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net7.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "BuildCliNet8" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net8.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "BuildCliNet9" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net9.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "Build" <| fun _ ->
    printfn "Building all supported frameworks."

Target.create "TestNet8" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "test --configuration Release --framework net8.0", tests)
    if exitCode <> 0 then failwith "Failed while running net8.0 tests"

Target.create "TestNet9" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "test --configuration Release --framework net9.0", tests)
    if exitCode <> 0 then failwith "Failed while running net9.0 tests"

Target.create "Test" <| fun _ ->
    printfn "Testing on all supported frameworks."

Target.create "Pack" <| fun _ ->
    allPackages
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "pack --configuration Release -o nupkg/Release", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}' package.'")

Target.create "Publish" <| fun _ ->
    let nugetKey =
        match Environment.environVarOrNone "SQLHYDRA_NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a SQLHYDRA_NUGET_KEY environmental variable"
    
    let getProjectVersion (projDir: string) = 
        let projName = DirectoryInfo(projDir).Name
        let doc = XDocument.Load($"{projDir}/{projName}.fsproj")
        doc.Descendants("Version") |> Seq.tryHead
        |> Option.map (fun versionElement -> versionElement.Value)
        |> Option.defaultWith (fun () -> failwith $"Could not find a <Version> element in '{projName}.fsproj'.")

    allPackages
    |> List.map (fun projDir ->
        let projName = DirectoryInfo(projDir).Name
        let version = getProjectVersion projDir
        let nupkgFilename = $"%s{projName}.%s{version}.nupkg"
        projDir </> "nupkg" </> "Release" </> nupkgFilename
    )
    |> List.map (fun nupkgFilepath -> Shell.Exec(Tools.dotnet, $"nuget push {nupkgFilepath} -s nuget.org -k {nugetKey} --skip-duplicate"), nupkgFilepath)
    |> List.iter (fun (code, pkg) -> if code <> 0 then printfn $"ERROR: Could not publish '{pkg}' package. Error: {code}") // Display error and continue

let dependencies = [
    "Restore" ==> "BuildQuery" ==> "BuildCliNet7" ==> "BuildCliNet8" ==> "BuildCliNet9" ==> "Build"
    "Build" ==> "TestNet8" ==> "TestNet9" ==> "Test"
    //"BuildCliNet9" ==> "TestNet9" ==> "Test"
    "Test" ==> "Pack" ==> "Publish"
]

[<EntryPoint>]
let main (args: string[]) =
    try
        match args with
        | [| singleArg |] -> Target.runOrDefault singleArg
        | _ -> Target.runOrDefault "Publish"
        0
    with ex ->
        printfn "%A" ex
        1
