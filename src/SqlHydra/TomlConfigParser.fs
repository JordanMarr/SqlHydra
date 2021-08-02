module SqlHydra.TomlConfigParser

open Tomlyn
open Tomlyn.Model
open Tomlyn.Syntax
open Schema

type TomlTable with
    member this.TryGetTable(name: string) =
        if this.ContainsKey(name)
        then Some (this.Item(name) :?> TomlTable)
        else None

/// Toml to Config.
let deserialize(toml: string) =
    (*
        NOTE: New configuration keys should be parsed gracefully so as to not break older versions!
    *)
    let doc = Toml.Parse(toml)
    let table = doc.ToModel()
    let general = table.Item("general") :?> TomlTable
    let readersMaybe = table.TryGetTable("readers")

    {
        Config.ConnectionString = general.["connection"] :?> string
        Config.OutputFile = general.["output"] :?> string
        Config.Namespace = general.["namespace"] :?> string
        Config.IsCLIMutable = general.["cli_mutable"] :?> bool
        Config.Readers = 
            readersMaybe
            |> Option.map (fun tbl -> 
                {
                    ReadersConfig.ReaderType = tbl.["reader_type"] :?> string
                }
            )
    }

/// Config to toml.
let serialize(cfg: Config) =
    let doc = DocumentSyntax()
    
    let general = TableSyntax("general")        
    general.Items.Add("connection", cfg.ConnectionString)
    general.Items.Add("output", cfg.OutputFile)
    general.Items.Add("namespace", cfg.Namespace)
    general.Items.Add("cli_mutable", cfg.IsCLIMutable)
    doc.Tables.Add(general)
    
    if cfg.Readers.IsSome then
        let readers = TableSyntax("readers")
        readers.Items.Add("reader_type", cfg.Readers.Value.ReaderType)
        doc.Tables.Add(readers)
    
    let toml = doc.ToString()
    toml