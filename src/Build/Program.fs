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

Target.create "BuildCliNet8" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net8.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "BuildCliNet9" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net9.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "BuildCliNet10" <| fun _ ->
    [ cli; tests ]
    |> List.map (fun pkg -> Shell.Exec(Tools.dotnet, "build --configuration Release --framework net10.0", pkg), pkg)
    |> List.iter (fun (code, pkg) -> if code <> 0 then failwith $"Could not build '{pkg}'package.'")

Target.create "Build" <| fun _ ->
    printfn "Building all supported frameworks."

Target.create "TestNet8" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "test --configuration Release --framework net8.0", tests)
    if exitCode <> 0 then failwith "Failed while running net8.0 tests"

Target.create "TestNet9" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "test --configuration Release --framework net9.0", tests)
    if exitCode <> 0 then failwith "Failed while running net9.0 tests"

Target.create "TestNet10" <| fun _ ->
    let exitCode = Shell.Exec(Tools.dotnet, "test --configuration Release --framework net10.0", tests)
    if exitCode <> 0 then failwith "Failed while running net10.0 tests"

Target.create "Test" <| fun _ ->
    printfn "Testing on all supported frameworks."

Target.create "Regen" <| fun _ ->
    // Regenerates every AdventureWorks*.fs in Tests from its sqlhydra-*.toml, against the
    // running database containers. CI reruns this and fails if a PR left the generated
    // files stale, so the generated diff always travels with the generator change.
    let tomlPattern = System.Text.RegularExpressions.Regex(@"^sqlhydra-([a-z]+)-(?:nullable-)?net(\d+)\.toml$")
    let tomls =
        DirectoryInfo(tests).GetFiles("sqlhydra-*.toml")
        |> Array.choose (fun f ->
            match tomlPattern.Match(f.Name) with
            | m when m.Success -> Some(f.Name, m.Groups.[1].Value, $"net{m.Groups.[2].Value}.0")
            | _ -> None)
        |> Array.sortBy (fun (name, _, _) -> name)
    if tomls.Length = 0 then failwith $"No sqlhydra-*.toml files found in '{tests}'."
    tomls
    |> Array.iter (fun (toml, provider, framework) ->
        printfn $"Regenerating {toml} ({provider}, {framework})"
        let code = Shell.Exec(Tools.dotnet, $"run --project {cli} --framework {framework} -- {provider} -t {toml}", tests)
        if code <> 0 then failwith $"Regeneration failed for '{toml}'.")

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
    "Restore" ==> "BuildQuery" ==> "BuildCliNet8" ==> "BuildCliNet9" ==> "BuildCliNet10" ==> "Build"
    "Build" ==> "TestNet8" ==> "TestNet9" ==> "TestNet10" ==> "Test"
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
