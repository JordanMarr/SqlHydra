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

/// Presents a series of user prompts to create a new config file.
let newConfigWizard(app: AppInfo) = 
    let connection = AnsiConsole.Ask<string>("Enter a database [green]Connection String[/]:")
    let outputFile = AnsiConsole.Ask<string>("Enter an [green]Output Filename[/] (Ex: [yellow]AdventureWorks.fs[/]):")
    let ns = AnsiConsole.Ask<string>("Enter a [green]Namespace[/] (Ex: [yellow]MyApp.AdventureWorks[/]):")
    let isCLIMutable = yesNo "Add CLIMutable attribute to generated records?"    
    let enableReaders = yesNo "Generate HydraReader?"
    let useDefaultReaderType = 
        if enableReaders 
        then yesNo $"Use the default Data Reader Type? (Default = {app.DefaultReaderType}):"
        else false
    let readerType = 
        if enableReaders && not useDefaultReaderType
        then AnsiConsole.Ask<string>($"Enter a [green]Data Reader Type[/]:")
        else app.DefaultReaderType

    { 
        Config.ConnectionString = connection.Replace(@"\\", @"\") // Fix if user copies an escaped backslash from an existing config
        Config.OutputFile = outputFile
        Config.Namespace = ns
        Config.IsCLIMutable = isCLIMutable
        Config.Readers = 
            if enableReaders 
            then Some { ReadersConfig.ReaderType = readerType }
            else None
    }

/// Ex: "sqlhydra-mssql.toml"
let buildTomlFilename(app: AppInfo) =
    $"{app.Command}.toml"

/// Saves a config as toml.
let saveConfig (tomlFilename: string, cfg: Config) = 
    let toml = TomlConfigParser.serialize(cfg)
    IO.File.WriteAllText(tomlFilename, toml)

/// Reads a config from toml.
let tryLoadConfig(tomlFileName: string) = 
    if IO.File.Exists(tomlFileName) then
        try            
            let toml = IO.File.ReadAllText(tomlFileName)
            let config = TomlConfigParser.deserialize(toml)
            Valid config
        with ex -> 
            Invalid ex.Message
    else 
        NotFound

/// Creates a sqlhydra-*.toml file if necessary and then runs.
let getConfig(app: AppInfo, argv: string array) = 

    AnsiConsole.MarkupLine($"{app.Name}")
    AnsiConsole.MarkupLine($"v[yellow]{app.Version}[/]")

    let tomlFilename = buildTomlFilename(app)

    match argv with 
    | [| |] ->
        match tryLoadConfig(tomlFilename) with
        | Valid cfg -> 
            cfg
        | Invalid exMsg -> 
            AnsiConsole.MarkupLine($"[red]ERROR: [/]Unable to deserialize '{tomlFilename}'. \n{exMsg}")
            failwith "Invalid toml config."
        | NotFound ->
            AnsiConsole.MarkupLine($"[yellow]\"{tomlFilename}\" not detected. Starting configuration wizard...[/]")
            let cfg = newConfigWizard(app)
            saveConfig(tomlFilename, cfg)
            cfg

    | _ ->
        AnsiConsole.MarkupLine($"Invalid args: '{argv}'. Expected no args, or \"--new\".")
        failwith "Invalid args."

