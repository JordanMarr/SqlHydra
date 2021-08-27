module SqlHydra.Fsproj
open System.IO
open System.Collections.Generic
open Microsoft.Build.Construction
open Domain

/// Adds the generated .fs file to the fsproj as Hidden=True.
let addFileToProject (cfg: Config) = 
    match Directory.EnumerateFiles(".", "*.fsproj") |> Seq.tryHead with
    | Some fsprojPath ->
        let root = ProjectRootElement.Open(fsprojPath)

        let fileAlreadyAdded = 
            root.ItemGroups 
            |> Seq.collect (fun grp -> grp.Items)
            |> Seq.exists (fun item -> 
                item.Include = cfg.OutputFile || 
                item.Include = cfg.OutputFile.Replace(@"\", "/") || // Handle "Folder/File.fs"
                item.Include = cfg.OutputFile.Replace("/", @"\")    // Handle "Folder\Files.fs"
            )

        let firstGroupWithFiles =
            root.ItemGroups
            |> Seq.filter (fun g -> g.Items |> Seq.exists (fun item -> item.ItemType = "Compile"))
            |> Seq.tryHead

        match firstGroupWithFiles, fileAlreadyAdded with
        | Some grp, false -> 
            printfn $"Adding '{cfg.OutputFile}' to .fsproj."
            grp.AddItem("Compile", cfg.OutputFile, [ KeyValuePair("Visible", "False") ]) |> ignore
            root.Save()
        | _ -> 
            ()
    
    | None -> 
        ()
