module SqlHydra.Fsproj
open System.IO
open System.Collections.Generic
open Microsoft.Build.Construction
open Domain

/// Adds the generated .fs file to the fsproj as Visible=False.
let addFileToProject (fsproj: FileInfo) (cfg: Config) = 
    let root = ProjectRootElement.Open(fsproj.FullName)

    let fileAlreadyAdded = 
        root.ItemGroups 
        |> Seq.collect (fun grp -> grp.Items)
        |> Seq.exists (fun item -> 
            item.Include = cfg.OutputFile || 
            item.Include = cfg.OutputFile.Replace(@"\", "/") || // Handle "Folder/File.fs"
            item.Include = cfg.OutputFile.Replace("/", @"\")    // Handle "Folder\Files.fs"
        )

    root.ItemGroups
    |> Seq.filter (fun g -> g.Items |> Seq.exists (fun item -> item.ItemType = "Compile"))
    |> Seq.tryHead
    |> Option.iter (fun fstCompileGrp ->
        if not fileAlreadyAdded then
            printfn $"Adding '{cfg.OutputFile}' to .fsproj."
            fstCompileGrp.AddItem("Compile", cfg.OutputFile, [ KeyValuePair("Visible", "False") ]) |> ignore
            root.Save()
    )

let getTargetFrameworks (fsProj: FileInfo) =
    let root = ProjectRootElement.Open(fsProj.FullName)
    let targetFrameworksValue = 
        root.PropertyGroups
        |> Seq.collect (fun pg -> pg.Properties)
        |> Seq.tryFind (fun p -> p.Name = "TargetFrameworks")
        |> Option.map (fun p -> p.Value)
        |> Option.defaultValue ""

    targetFrameworksValue.Split(';')
    |> Array.map _.Trim()
    
let targetsLegacyFramework (fsProj: FileInfo) = 
    getTargetFrameworks fsProj
    |> Array.exists (fun t -> 
        t.StartsWith("net4") || 
        t.StartsWith("netstandard")
    )