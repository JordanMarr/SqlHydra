module SqlHydra.Console

open System
open System.IO
open Spectre.Console
open SqlHydra.Domain

type Args =
    {
        Provider: ISqlHydraDbProvider
        TomlFile: FileInfo
        Project: FileInfo
        Version: Version.InformationalVersion
        ConnectionString: string option
    }

and IsLegacy = bool

type LoadConfigResult = 
    | Valid of Config
    | Invalid of Exception
    | NotFound

/// Creates a yes/no prmompt.
let yesNo(title: string) = 
    let selection = SelectionPrompt<string>()
    selection.Title <- title
    selection.AddChoices(["Yes"; "No"]) |> ignore    
    let answer = AnsiConsole.Prompt(selection)
    if answer = "Yes"
    then AnsiConsole.MarkupLine($"{title} [green]{answer}[/]")
    else AnsiConsole.MarkupLine($"{title} [red]{answer}[/]")
    answer = "Yes"

/// Presents a series of user prompts to create a new config file.
let newConfigWizard (args: Args) =
    let connection =
        let cn = AnsiConsole.Ask<string>("[blue]-[/] Enter a database [green]Connection String[/]:")
        cn.Replace(@"\\", @"\") // Fix if user copies an escaped backslash from an existing config
    let outputFile = AnsiConsole.Ask<string>("[blue]-[/] Enter an [green]Output Filename[/] (Ex: [yellow]AdventureWorks.fs[/]):")
    let ns = AnsiConsole.Ask<string>("[blue]-[/] Enter a [green]Namespace[/] (Ex: [yellow]MyApp.AdventureWorks[/]):")
    let config =
        {
            Config.ConnectionString = connection
            Config.OutputFile = outputFile
            Config.Namespace = ns
            Config.IsCLIMutable = true
            Config.IsMutableProperties = false
            Config.NullablePropertyType = NullablePropertyType.Option
            Config.ProviderDbTypeAttributes = true
            Config.TableDeclarations = true
            Config.Readers = None
            Config.Filters = Filters.Empty // User must manually configure filter in .toml file
            Config.TypeMappingExtensions = []
        }

    AnsiConsole.MarkupLine($"[green]-[/] {args.TomlFile.Name} has been created!")
    AnsiConsole.MarkupLine($"[green]-[/] Please install the `SqlHydra.Query` NuGet package in your project.")
    config

/// Saves a config as toml.
let saveConfig (tomlFile: FileInfo, cfg: Config) = 
    let toml = TomlConfigParser.save(cfg)
    File.WriteAllText(tomlFile.FullName, toml)

/// Reads a config from toml.
let tryLoadConfig(tomlFile: FileInfo) =     
    if tomlFile.Exists then
        try
            let toml = File.ReadAllText(tomlFile.FullName)
            let config = TomlConfigParser.read(toml)
            Valid config
        with ex -> 
            Invalid ex
    else 
        NotFound

let printConfig (cfg: Config) = 
    // Create connection string object 
    let connString = new System.Data.Common.DbConnectionStringBuilder(ConnectionString = cfg.ConnectionString)
    connString.Remove("password") |> ignore
    AnsiConsole.MarkupLine($"[blue]-[/] Connection String: [deepskyblue1]\"{connString}\"[/]")
    AnsiConsole.MarkupLine($"[blue]-[/] Output File: [deepskyblue1]\"{cfg.OutputFile}\"[/]")
    AnsiConsole.MarkupLine($"[blue]-[/] Namespace: [deepskyblue1]\"{cfg.Namespace}\"[/]")
    AnsiConsole.MarkupLine($"[blue]-[/] CLI Mutable: [deepskyblue1]{cfg.IsCLIMutable}[/]")
    AnsiConsole.MarkupLine($"[blue]-[/] Mutable Properties: [deepskyblue1]{cfg.IsMutableProperties}[/]")
    AnsiConsole.MarkupLine($"[blue]-[/] Nullable Property Type: [deepskyblue1]\"{cfg.NullablePropertyType}\"[/]")
    AnsiConsole.MarkupLine($"[blue]-[/] Provider DB Type Attributes: [deepskyblue1]{cfg.ProviderDbTypeAttributes}[/]")
    AnsiConsole.MarkupLine($"[blue]-[/] Table Declarations: [deepskyblue1]{cfg.TableDeclarations}[/]")
    // Filters are printed in SchemaFilters.fs

let printLegacyStatus (isLegacy: bool) = 
    if isLegacy 
    then AnsiConsole.MarkupLine($"[blue]-[/] DateOnly/TimeOnly Support: [deepskyblue1]False[/]")
    else AnsiConsole.MarkupLine($"[blue]-[/] DateOnly/TimeOnly Support: [deepskyblue1]True[/]")        

/// Creates a sqlhydra-*.toml file if necessary.
let getOrCreateConfig (args: Args) = 
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine($"{args.Provider.Name} [gold1]v%s{args.Version.InformationalVersion}[/]")

    match tryLoadConfig(args.TomlFile) with
    | Valid cfg -> 
        printConfig cfg
        cfg
    | Invalid ex -> 
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything)
        failwith "Invalid toml config."
    | NotFound ->
        AnsiConsole.MarkupLine($"[blue]-[/] `{args.TomlFile.Name}` does not exist. Starting configuration wizard...")
        let cfg = newConfigWizard(args)
        saveConfig(args.TomlFile, cfg)
        cfg

open Fantomas.Core

let formatCodeWithFantomas (code: string) =
    // Generated code is read, not hand-edited: a wide line beats a stacked one. Records split
    // when wider than MaxRecordWidth (default 40!), so every WriteColumn became a three-line
    // stanza and the write-column lists dominated the file. At 200/200 each column is one line.
    let cfg = { FormatConfig.Default with MaxLineLength = 200; MaxRecordWidth = 200 }

    CodeFormatter.FormatDocumentAsync(false, code, cfg) 
    |> Async.RunSynchronously
    |> _.Code

/// Runs code generation for a given database provider.
let run (args: Args) = 
    let cfg = 
        getOrCreateConfig(args)
        |> fun cfg -> 
            // CLI connection string overrides toml file connection string.
            match args.ConnectionString with
            | Some cs -> { cfg with ConnectionString = cs }
            | None -> cfg
    
    // The generated file should be created relative to the .fsproj directory.
    let outputFile = Path.Combine(args.Project.Directory.FullName, cfg.OutputFile) |> FileInfo

    // Ensure the output directory exists (`cfg.OutputFile` may contain subdirectories).
    outputFile.Directory.Create()

    // Load extensions from TOML-configured assemblies (explicit opt-in only)
    let extensions = Extensions.loadNamed args.Project cfg.TypeMappingExtensions

    let generatedCode =
        let isLegacy = Fsproj.targetsLegacyFramework args.Project
        printLegacyStatus isLegacy
        let typeMappingExts = extensions |> Extensions.ofType<IExtendTypeMapping>
        //let namingExts = extensions |> Extensions.ofType<IExtendNaming> // TODO: enable once IExtendNaming is stable
        let namingExts : IExtendNaming list = []
        let schema = args.Provider.GetSchema(cfg, isLegacy, typeMappingExts)
        SchemaTemplate.generate cfg args.Provider schema args.Version namingExts
        |> formatCodeWithFantomas
        
    File.WriteAllText(outputFile.FullName, generatedCode)
    Fsproj.addFileToProject args.Project cfg
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine($"[gray]https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration[/]")
    AnsiConsole.MarkupLine($"[green1]Generated: \"{outputFile.FullName}\"![/]")
