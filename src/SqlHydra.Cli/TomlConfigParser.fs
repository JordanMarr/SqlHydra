module SqlHydra.TomlConfigParser

open Tomlyn
open Tomlyn.Model
open Tomlyn.Syntax
open Domain

/// A TOML table that names itself in error messages, e.g. "[general]" for the table at `path` "general".
type private Section(path: string, table: TomlTable) =
    let name = if path = "" then "The config" else $"[{path}]"

    let child (key: string) (t: TomlTable) = Section((if path = "" then key else $"{path}.{key}"), t)

    member _.Keys = table.Keys

    member private _.Cast<'T>(key: string, value: obj) : 'T =
        let kind (t: System.Type) =
            if t = typeof<string> then "a String"
            elif t = typeof<bool> then "a Boolean"
            elif t = typeof<int64> then "an Integer"
            elif t = typeof<float> then "a Float"
            elif t = typeof<TomlDateTime> then "a Date-Time"
            elif t = typeof<TomlArray> || t = typeof<TomlTableArray> then "an Array"
            elif t = typeof<TomlTable> then "a Table"
            else $"a {t.Name}"
        match value with
        | :? 'T as typed -> typed
        | _ -> failwith $"{name} key '{key}' should be {kind typeof<'T>}, but is {kind (value.GetType())}."

    /// The value at `key`, or None when it is left out. Fails when the value has the wrong type.
    member this.TryGet<'T>(key: string) : 'T option =
        match table.TryGetValue key with
        | true, value -> Some (this.Cast<'T>(key, value))
        | _ -> None

    /// The value at `key`. Fails when it is left out or has the wrong type.
    member this.Get<'T>(key: string) : 'T =
        this.TryGet<'T> key |> Option.defaultWith (fun () -> failwith $"{name} is missing required key '{key}'.")

    /// The table at `key`. Fails when it is left out or is not a table.
    member this.Required(key: string) = child key (this.Get key)

    /// The table at `key`, or None when it is left out. Fails when it is not a table.
    member this.Optional(key: string) = this.TryGet key |> Option.map (child key)

    /// The list of strings at `key`, or [] when it is left out. Fails when it is not a list of strings.
    member this.Strings(key: string) =
        this.TryGet<TomlArray> key
        |> Option.map (Seq.map (fun value -> this.Cast<string>(key, value)) >> Seq.toList)
        |> Option.defaultValue []

/// Reads .toml file and returns a Config.
let read(toml: string) =

    // NOTE: New configuration keys should be parsed gracefully so as to not break older versions!
    let doc = Toml.Parse toml
    let model = Section("", doc.ToModel())
    let generalTable = model.Required "general"
    let readersTableMaybe = model.Optional "readers"
    let filtersTableMaybe = model.Optional "filters"
    let extensionsTableMaybe = model.Optional "extensions"
    let queryIntegrationTableMaybe = model.Optional "sqlhydra_query_integration"

    {
        Config.ConnectionString = generalTable.Get "connection"
        Config.OutputFile = generalTable.Get "output"
        Config.Namespace = generalTable.Get "namespace"
        Config.IsCLIMutable = generalTable.Get "cli_mutable"
        Config.IsMutableProperties = generalTable.TryGet "mutable_properties" |> Option.defaultValue false
        Config.NullablePropertyType = 
            generalTable.TryGet "nullable_property_type" 
            |> Option.map (fun (value: string) -> 
                match value.ToLower() with
                | "option" -> NullablePropertyType.Option
                | "nullable" -> NullablePropertyType.Nullable
                | _ -> NullablePropertyType.Option
            )
            |> Option.defaultValue NullablePropertyType.Option
        Config.ProviderDbTypeAttributes = 
            match queryIntegrationTableMaybe with
            | Some queryIntegrationTable -> queryIntegrationTable.TryGet "provider_db_type_attributes" |> Option.defaultValue true
            | None -> true // Default to true if missing
        Config.TableDeclarations =
            match queryIntegrationTableMaybe with
            | Some queryIntegrationTable ->
                match queryIntegrationTable.TryGet "table_declarations" with
                | Some tblDecl -> tblDecl
                | None -> true // Default to true [sqlhydra_query_integration] table already exists
            | None -> false // Default to false if [sqlhydra_query_integration] table is missing
        Config.LeftJoinedViews =
            // Absent means false so existing codebases see no change; the init wizard writes
            // `left_joined_views = true` into new configs.
            queryIntegrationTableMaybe
            |> Option.bind (fun queryIntegrationTable -> queryIntegrationTable.TryGet "left_joined_views")
            |> Option.defaultValue false
        Config.Readers = 
            readersTableMaybe
            |> Option.map (fun rdrsTbl -> 
                {
                    ReadersConfig.ReaderType = rdrsTbl.Get<string> "reader_type"
                }
            )
        Config.TypeMappingExtensions =
            match extensionsTableMaybe with
            | Some extTable ->
                extTable.Strings "type_mappings"
            | None -> []
        Config.Filters =
            match filtersTableMaybe with
            | Some filtersTable -> 
                {
                    Filters.Includes = filtersTable.Strings "include"
                    Filters.Excludes = filtersTable.Strings "exclude"
                    Filters.Restrictions = 
                        match filtersTable.Optional "restrictions" with
                        | Some restrictions -> 
                            restrictions.Keys
                            |> Seq.map (fun key -> 
                                key, 
                                    restrictions.Strings key
                                    |> Seq.toArray 
                                    |> Array.map (fun s -> if s = "" then null else s) // GetSchema expects nulls for missing values, not empty strings.
                            )
                            |> Map.ofSeq
                        | None ->
                            Map.empty
                }
            | None ->
                Filters.Empty
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
    queryInt.Items.Add("left_joined_views", cfg.LeftJoinedViews)
    doc.Tables.Add(queryInt)

    cfg.Readers |> Option.iter (fun readersConfig ->
        let readers = TableSyntax("readers")
        readers.Items.Add("reader_type", readersConfig.ReaderType)
        doc.Tables.Add(readers))

    if cfg.TypeMappingExtensions <> [] then
        let extensions = TableSyntax("extensions")
        extensions.Items.Add("type_mappings", cfg.TypeMappingExtensions |> List.toArray)
        doc.Tables.Add(extensions)

    let filters = TableSyntax("filters")
    filters.Items.Add("include", cfg.Filters.Includes |> List.toArray)
    filters.Items.Add("exclude", cfg.Filters.Excludes |> List.toArray)

    doc.Tables.Add(filters)
    
    let toml = doc.ToString()
    toml