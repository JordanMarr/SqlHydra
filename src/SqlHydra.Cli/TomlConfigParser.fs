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

    // NOTE: New configuration keys should be parsed gracefully so as to not break older versions!
    let doc = Toml.Parse toml
    let model = doc.ToModel()
    let generalTable = model.Get<TomlTable> "general"
    let readersTableMaybe = model.TryGet<TomlTable> "readers"
    let filtersTableMaybe = model.TryGet<TomlTable> "filters"
    let queryIntegrationTableMaybe = model.TryGet<TomlTable> "sqlhydra_query_integration"

    {
        Config.ConnectionString = generalTable.Get "connection"
        Config.OutputFile = generalTable.Get "output"
        Config.Namespace = generalTable.Get "namespace"
        Config.IsCLIMutable = generalTable.Get "cli_mutable"
        Config.ProviderDbTypeAttributes = 
            match queryIntegrationTableMaybe with
            | Some queryIntegrationTable -> queryIntegrationTable.Get "provider_db_type_attributes"
            | None -> true // Default to true if missing
        Config.TableDeclarations = 
            match queryIntegrationTableMaybe with
            | Some queryIntegrationTable -> 
                match queryIntegrationTable.TryGet "table_declarations" with
                | Some tblDecl -> tblDecl
                | None -> true // Default to true [sqlhydra_query_integration] table already exists
            | None -> false // Default to false if [sqlhydra_query_integration] table is missing
        Config.Readers = 
            readersTableMaybe
            |> Option.map (fun rdrsTbl -> 
                {
                    ReadersConfig.ReaderType = rdrsTbl.Get<string> "reader_type"
                }
            )
        Config.Filters = 
            match filtersTableMaybe with
            | Some filtersTable -> 
                {
                    FilterPatterns.Includes = filtersTable.Get "include" |> Seq.cast<string> |> Seq.toList
                    FilterPatterns.Excludes = filtersTable.Get "exclude" |> Seq.cast<string> |> Seq.toList
                    FilterPatterns.Restrictions = 
                        match filtersTable.TryGet<TomlTable> "restrictions" with
                        | Some restrictions -> 
                            restrictions 
                            |> Seq.map (fun kvp -> 
                                kvp.Key, 
                                    kvp.Value :?> TomlArray 
                                    |> Seq.cast<string> 
                                    |> Seq.toArray 
                                    |> Array.map (fun s -> if s = "" then null else s) // GetSchema expects nulls for missing values, not empty strings.
                            )
                            |> Map.ofSeq
                        | None ->
                            Map.empty
                }
            | None ->
                FilterPatterns.Empty
    }

/// Saves a Config to .toml file.
let save(cfg: Config) =
    let doc = DocumentSyntax()
    
    let general = TableSyntax("general")        
    general.Items.Add("connection", cfg.ConnectionString)
    general.Items.Add("output", cfg.OutputFile)
    general.Items.Add("namespace", cfg.Namespace)
    general.Items.Add("cli_mutable", cfg.IsCLIMutable)
    doc.Tables.Add(general)
    
    let queryInt = TableSyntax("sqlhydra_query_integration")
    queryInt.Items.Add("provider_db_type_attributes", cfg.ProviderDbTypeAttributes)
    queryInt.Items.Add("table_declarations", cfg.TableDeclarations)
    doc.Tables.Add(queryInt)

    cfg.Readers |> Option.iter (fun readersConfig ->
        let readers = TableSyntax("readers")
        readers.Items.Add("reader_type", readersConfig.ReaderType)
        doc.Tables.Add(readers))

    let filters = TableSyntax("filters")
    filters.Items.Add("include", cfg.Filters.Includes |> List.toArray)
    filters.Items.Add("exclude", cfg.Filters.Excludes |> List.toArray)

    doc.Tables.Add(filters)
    
    let toml = doc.ToString()
    toml