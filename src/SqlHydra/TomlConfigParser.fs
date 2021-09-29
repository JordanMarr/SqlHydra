module SqlHydra.TomlConfigParser

open Tomlyn
open Tomlyn.Model
open Tomlyn.Syntax
open Domain

type TomlTable with

    member this.Get<'T>(name: string) = 
        this.Item(name) :?> 'T

    member this.TryGet<'T>(name: string) =
        if this.ContainsKey(name)
        then Some (this.Item(name) :?> 'T)
        else None

/// Reads .toml file and returns a Config.
let read(toml: string) =
    (*
        NOTE: New configuration keys should be parsed gracefully so as to not break older versions!
    *)
    let doc = Toml.Parse toml
    let model = doc.ToModel()
    let generalTable = model.Get<TomlTable> "general"
    let filterMaybe = generalTable.TryGet "filter"
    let readersTable = model.TryGet<TomlTable> "readers"
    {
        Config.ConnectionString = generalTable.Get "connection"
        Config.OutputFile = generalTable.Get "output"
        Config.Namespace = generalTable.Get "namespace"
        Config.IsCLIMutable = generalTable.Get "cli_mutable"
        Config.Filter = filterMaybe
        Config.Readers = 
            readersTable
            |> Option.map (fun rdrsTbl -> 
                {
                    ReadersConfig.ReaderType = rdrsTbl.Get<string> "reader_type"
                }
            )
    }

/// Saves a Config to .toml file.
let save(cfg: Config) =
    let doc = DocumentSyntax()
    
    let general = TableSyntax("general")        
    general.Items.Add("connection", cfg.ConnectionString)
    general.Items.Add("output", cfg.OutputFile)
    general.Items.Add("namespace", cfg.Namespace)
    general.Items.Add("cli_mutable", cfg.IsCLIMutable)
    cfg.Filter |> Option.iter (fun filter -> 
        general.Items.Add("filter", filter)
    )

    doc.Tables.Add(general)
    
    cfg.Readers |> Option.iter (fun readersConfig ->
        let readers = TableSyntax("readers")
        readers.Items.Add("reader_type", readersConfig.ReaderType)
        doc.Tables.Add(readers)
    )
    
    let toml = doc.ToString()
    toml