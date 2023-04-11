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
let cli = path [ slnRoot; "SqlHydra.Cli" ]
let tests = path [ slnRoot; "Tests" ]

let allPackages = [ query; cli ]
let toPublish = allPackages

Target.create "Restore" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "restore", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not restore '{pkg}' package.")

Target.create "BuildQuery" <| fun _ ->
    // SqlHydra.Query has to built separately since it is netstandard2.0
    query
    |> (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release", pkg), pkg)
    |> (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "BuildNet6" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net6.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "BuildNet7" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net7.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "Build" <| fun _ ->
    printfn "Building all supported frameworks."

Target.create "TestNet6" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "run --configuration Release --framework net6.0", tests)
    if exitCode <> 0 then failwith "Failed while running net6.0 tests"

Target.create "TestNet7" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "run --configuration Release --framework net7.0", tests)
    if exitCode <> 0 then failwith "Failed while running net7.0 tests"

Target.create "Test" <| fun _ ->
    printfn "Testing on all supported frameworks."

Target.create "Pack" <| fun _ ->
    toPublish
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "pack --configuration Release -o nupkg/Release", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}' package.'")

Target.create "Publish" <| fun _ ->
    let nugetKey =
        match Environment.environVarOrNone "SQLHYDRA_NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a SQLHYDRA_NUGET_KEY environmental variable"
    
    let getProjectVersion (projDir: string) = 
        let projName = DirectoryInfo(projDir).Name
        let dllPath = projDir </> "bin" </> "Release" </> "net6.0" </> $"{projName}.dll"
        System.Reflection.AssemblyName.GetAssemblyName(dllPath).Version

    toPublish
    |> List.map (fun projDir ->
        let version = getProjectVersion projDir
        let projName = DirectoryInfo(projDir).Name
        let nupkgFilename = $"{projName}.{version.Major}.{version.Minor}.{version.Build}.nupkg"
        projDir </> "nupkg" </> "Release" </> nupkgFilename
    )
    |> List.map (fun nupkgFilepath -> Shell.Exec(Tools.dotnet, $"nuget push {nupkgFilepath} -s nuget.org -k {nugetKey} --skip-duplicate"), nupkgFilepath)
    |> List.iter (fun (code, pkg) -> if code <> 0 then printfn $"ERROR: Could not publish '{pkg}' package. Error: {code}") // Display error and continue

let dependencies = [
    "Restore" ==> "BuildQuery" ==> "BuildNet6" ==> "BuildNet7" ==> "Build"
    "Build" ==> "TestNet6" ==> "TestNet7" ==> "Test"
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
