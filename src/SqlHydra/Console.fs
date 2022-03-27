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
    let connection = AnsiConsole.Ask<string>("[blue]>[/] Enter a database [green]Connection String[/]:")
    let outputFile = AnsiConsole.Ask<string>("[blue]>[/] Enter an [green]Output Filename[/] (Ex: [yellow]AdventureWorks.fs[/]):")
    let ns = AnsiConsole.Ask<string>("[blue]>[/] Enter a [green]Namespace[/] (Ex: [yellow]MyApp.AdventureWorks[/]):")
    let isCLIMutable = yesNo "[blue]>[/] Add CLIMutable attribute to generated records?"    
    let enableReaders = yesNo "[blue]>[/] Generate HydraReader? (Recommended. This generates strongly typed data readers and is also required when using SqlHydra.Query.)"

    AnsiConsole.MarkupLine($"[green]>[/] {app.Command}.toml has been created!")
    AnsiConsole.MarkupLine($"[green]>[/] Please install the `{app.DefaultProvider}` NuGet package in your project.");

    { 
        Config.ConnectionString = connection.Replace(@"\\", @"\") // Fix if user copies an escaped backslash from an existing config
        Config.OutputFile = outputFile
        Config.Namespace = ns
        Config.IsCLIMutable = isCLIMutable
        Config.Filters = FilterPatterns.Empty // User must manually configure filter in .toml file
        Config.Readers = 
            if enableReaders 
            then Some { ReadersConfig.ReaderType = app.DefaultReaderType }
            else None
    }

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

/// Creates a sqlhydra-*.toml file if necessary and then runs.
let getConfig(app: AppInfo, argv: string array) = 
    AnsiConsole.MarkupLine($"[blue]>[/] {app.Name}")
    AnsiConsole.MarkupLine($"[blue]>[/] v[yellow]{app.Version}[/]")

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
        AnsiConsole.MarkupLine($"[blue]>[/] `{tomlFile.Name}` does not exist. Starting configuration wizard...")
        let cfg = newConfigWizard(app)
        saveConfig(tomlFile, cfg)
        cfg