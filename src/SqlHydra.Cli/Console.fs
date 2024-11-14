module SqlHydra.Console

open System
open System.IO
open Spectre.Console
open SqlHydra.Domain

type Args = 
    {
        Provider: string
        AppInfo: AppInfo
        TomlFile: FileInfo
        Project: FileInfo
        GetSchema: Config -> IsLegacy -> Schema
        Version: string
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

type UseCase = 
    | SqlHydraQueryIntegration
    | OtherDataLibrary
    | Standalone
    override this.ToString() = // Selection prompts:
        match this with 
        | SqlHydraQueryIntegration -> "SqlHydra.Query integration (default)"
        | OtherDataLibrary -> "Other data library"
        | Standalone -> "Standalone"
        
let multiSelectDU<'T> (title: string) (options: 'T seq) = 
    let selection = SelectionPrompt<'T>()
    selection.Title <- title
    selection.UseConverter(fun item -> item.ToString()) |> ignore
    selection.AddChoices options |> ignore
    let answer = AnsiConsole.Prompt(selection)
    AnsiConsole.MarkupLine($"{title} [green]{answer}[/]")
    answer

/// Presents a series of user prompts to create a new config file.
let newConfigWizard (args: Args) = 
    let app = args.AppInfo
    let connection = 
        let cn = AnsiConsole.Ask<string>("[blue]-[/] Enter a database [green]Connection String[/]:")
        cn.Replace(@"\\", @"\") // Fix if user copies an escaped backslash from an existing config
    let outputFile = AnsiConsole.Ask<string>("[blue]-[/] Enter an [green]Output Filename[/] (Ex: [yellow]AdventureWorks.fs[/]):")
    let ns = AnsiConsole.Ask<string>("[blue]-[/] Enter a [green]Namespace[/] (Ex: [yellow]MyApp.AdventureWorks[/]):")
    let useCase = multiSelectDU<UseCase> "[blue]-[/] Select a use case:" [ SqlHydraQueryIntegration; OtherDataLibrary; Standalone ]
    let config = 
        match useCase with 
        | SqlHydraQueryIntegration -> 
            { 
                Config.ConnectionString = connection
                Config.OutputFile = outputFile
                Config.Namespace = ns
                Config.IsCLIMutable = true
                Config.IsMutableProperties = false
                Config.NullablePropertyType = NullablePropertyType.Option
                Config.ProviderDbTypeAttributes = true
                Config.TableDeclarations = true
                Config.Readers = Some { ReadersConfig.ReaderType = app.DefaultReaderType } 
                Config.Filters = Filters.Empty // User must manually configure filter in .toml file
            }
        | OtherDataLibrary -> 
            { 
                Config.ConnectionString = connection
                Config.OutputFile = outputFile
                Config.Namespace = ns
                Config.IsCLIMutable = true
                Config.IsMutableProperties = false
                Config.NullablePropertyType = NullablePropertyType.Option
                Config.ProviderDbTypeAttributes = false
                Config.TableDeclarations = false
                Config.Readers = None 
                Config.Filters = Filters.Empty // User must manually configure filter in .toml file
            }
        | Standalone -> 
            { 
                Config.ConnectionString = connection
                Config.OutputFile = outputFile
                Config.Namespace = ns
                Config.IsCLIMutable = true
                Config.IsMutableProperties = false
                Config.NullablePropertyType = NullablePropertyType.Option
                Config.ProviderDbTypeAttributes = false
                Config.TableDeclarations = false
                Config.Readers = Some { ReadersConfig.ReaderType = app.DefaultReaderType } 
                Config.Filters = Filters.Empty // User must manually configure filter in .toml file
            }

    AnsiConsole.MarkupLine($"[green]-[/] {args.TomlFile.Name} has been created!")
    if config.Readers <> None then AnsiConsole.MarkupLine($"[green]-[/] Please install the `{app.DefaultProvider}` NuGet package in your project.")
    if useCase = SqlHydraQueryIntegration then AnsiConsole.MarkupLine($"[green]-[/] Please install the `SqlHydra.Query` NuGet package in your project.")
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
    let readers = cfg.Readers |> Option.map (fun r -> r.ReaderType) |> Option.defaultValue "HydraReader Feature Disabled"
    AnsiConsole.MarkupLine($"[blue]-[/] Readers: [deepskyblue1]\"{readers}\"[/]")
    // Filters are printed in SchemaFilters.fs

let printLegacyStatus (isLegacy: bool) = 
    if isLegacy 
    then AnsiConsole.MarkupLine($"[blue]-[/] DateOnly/TimeOnly Support: [deepskyblue1]False[/]")
    else AnsiConsole.MarkupLine($"[blue]-[/] DateOnly/TimeOnly Support: [deepskyblue1]True[/]")        

/// Creates a sqlhydra-*.toml file if necessary.
let getOrCreateConfig (args: Args) = 
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine($"{args.AppInfo.Name} [gold1]v{args.Version}[/]")

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

    let generatedCode = 
        let isLegacy = Fsproj.targetsLegacyFramework args.Project
        printLegacyStatus isLegacy
        let schema = args.GetSchema cfg isLegacy
        SchemaTemplate.generate cfg args.AppInfo schema args.Version isLegacy

    File.WriteAllText(outputFile.FullName, generatedCode)
    Fsproj.addFileToProject args.Project cfg
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine($"[gray]https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration[/]")
    AnsiConsole.MarkupLine($"[green1]Generated: \"{outputFile.FullName}\"![/]")
