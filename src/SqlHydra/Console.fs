module SqlHydra.Console

open Spectre.Console
open SqlHydra.Domain
open System

type LoadConfigResult = 
    | Valid of Config
    | Invalid of error: string
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
let newConfigWizard(app: AppInfo) = 
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
                Config.ProviderDbTypeAttributes = true
                Config.Readers = Some { ReadersConfig.ReaderType = app.DefaultReaderType } 
                Config.Filters = FilterPatterns.Empty // User must manually configure filter in .toml file
            }
        | OtherDataLibrary -> 
            { 
                Config.ConnectionString = connection
                Config.OutputFile = outputFile
                Config.Namespace = ns
                Config.IsCLIMutable = true
                Config.ProviderDbTypeAttributes = false
                Config.Readers = None 
                Config.Filters = FilterPatterns.Empty // User must manually configure filter in .toml file
            }
        | Standalone -> 
            { 
                Config.ConnectionString = connection
                Config.OutputFile = outputFile
                Config.Namespace = ns
                Config.IsCLIMutable = true
                Config.ProviderDbTypeAttributes = false
                Config.Readers = Some { ReadersConfig.ReaderType = app.DefaultReaderType } 
                Config.Filters = FilterPatterns.Empty // User must manually configure filter in .toml file
            }

    AnsiConsole.MarkupLine($"[green]-[/] {app.Command}.toml has been created!")
    if config.Readers <> None then AnsiConsole.MarkupLine($"[green]-[/] Please install the `{app.DefaultProvider}` NuGet package in your project.")
    if useCase = SqlHydraQueryIntegration then AnsiConsole.MarkupLine($"[green]-[/] Please install the `SqlHydra.Query` NuGet package in your project.")
    config

/// Ex: "sqlhydra-mssql.toml"
let buildTomlFilename(app: AppInfo) =
    $"{app.Command}.toml"

/// Saves a config as toml.
let saveConfig (tomlFile: IO.FileInfo, cfg: Config) = 
    let toml = TomlConfigParser.save(cfg)
    IO.File.WriteAllText(tomlFile.FullName, toml)

/// Reads a config from toml.
let tryLoadConfig(tomlFile: IO.FileInfo) = 
    if tomlFile.Exists then
        try            
            let toml = IO.File.ReadAllText(tomlFile.FullName)
            let config = TomlConfigParser.read(toml)
            Valid config
        with ex -> 
            Invalid ex.Message
    else 
        NotFound

/// Creates a sqlhydra-*.toml file if necessary.
let getConfig(app: AppInfo, argv: string array) = 
    AnsiConsole.MarkupLine($"[blue]-[/] {app.Name}")
    AnsiConsole.MarkupLine($"[blue]-[/] v[yellow]{app.Version}[/]")

    let tomlFile = 
        match argv with 
        | [| |] -> IO.FileInfo(buildTomlFilename(app))
        | [| tomlFilePath |] -> IO.FileInfo(tomlFilePath)
        | _ ->
            AnsiConsole.MarkupLine($"[red]>[/] Invalid args: '{argv}'. Expected no args, or a .toml configuration file path.")
            failwith "Invalid args."

    match tryLoadConfig(tomlFile) with
    | Valid cfg -> 
        cfg
    | Invalid exMsg -> 
        Console.WriteLine($"> Unable to deserialize '{tomlFile.FullName}'. \n{exMsg}")
        failwith "Invalid toml config."
    | NotFound ->
        AnsiConsole.MarkupLine($"[blue]-[/] `{tomlFile.Name}` does not exist. Starting configuration wizard...")
        let cfg = newConfigWizard(app)
        saveConfig(tomlFile, cfg)
        cfg

/// Runs code generation for a given database provider.
let run (app: AppInfo, argv: string[], getSchema: Config -> Schema) = 
    let cfg = getConfig(app, argv)

    let formattedCode = 
        getSchema cfg
        |> SchemaGenerator.generateModule cfg app
        |> SchemaGenerator.toFormattedCode cfg app

    IO.File.WriteAllText(cfg.OutputFile, formattedCode)
    Fsproj.addFileToProject(cfg)
    AnsiConsole.MarkupLine($"[green]-[/] `{cfg.OutputFile}` has been generated!")
    0