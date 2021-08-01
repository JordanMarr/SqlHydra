module Console

open Spectre.Console
open SqlHydra.Schema
open System
open Newtonsoft.Json

type AppInfo = {
    Name: string
    Command: string
    DefaultReaderType: string
    Version: string
}

let yesNo(title: string) = 
    let selection = SelectionPrompt<string>()
    selection.Title <- title
    selection.AddChoices(["Yes"; "No"]) |> ignore
    AnsiConsole.Prompt(selection) = "Yes"

let newConfigWizard(app: AppInfo) = 
    let connStr = AnsiConsole.Ask<string>("Enter a database [green]Connection String[/]:")
    let outputFile = AnsiConsole.Ask<string>("Enter an [green]Output Filename[/] (Ex: [yellow]AdventureWorks.fs[/]):")
    let ns = AnsiConsole.Ask<string>("Enter a [green]Namespace[/] (Ex: [yellow]MyApp.AdventureWorks[/]):")
    let isCLIMutable = yesNo "Add CLIMutable attribute to generated records?"
    let enableReaders = yesNo "Generate HydraReader?"
    let useDefaultReaderType = 
        if enableReaders 
        then yesNo $"Use the default Data Reader Type? (Default = {app.DefaultReaderType}):"
        else false
    let readerType = 
        if not useDefaultReaderType
        then AnsiConsole.Ask<string>($"Enter [green]Data Reader Type[/]:")
        else app.DefaultReaderType

    { 
        Config.ConnectionString = connStr
        Config.OutputFile = outputFile
        Config.Namespace = ns
        Config.IsCLIMutable = isCLIMutable
        Config.Readers = 
            {
                ReadersConfig.IsEnabled = enableReaders
                ReadersConfig.ReaderType = readerType
            }
    }

/// Ex: "sqlhydra-mssql.json"
let buildJsonFileName(app: AppInfo) =
    $"{app.Command}.json"

let saveConfig (jsonFileName: string, cfg: Config) = 
    let json = JsonConvert.SerializeObject(cfg, Formatting.Indented)
    IO.File.WriteAllText(jsonFileName, json)

let tryLoadConfig(jsonFileName: string) = 
    if IO.File.Exists(jsonFileName) then
        try
            let json = IO.File.ReadAllText(jsonFileName)
            JsonConvert.DeserializeObject<SqlHydra.Schema.Config>(json) |> Some
        with ex -> 
            None
    else 
        None

/// Creates hydra.json if necessary and then runs.
let getConfig(app: AppInfo, argv: string array) = 

    AnsiConsole.MarkupLine($"{app.Name}")
    AnsiConsole.MarkupLine($"v[yellow]{app.Version}[/]")

    let jsonFileName = buildJsonFileName(app)

    match argv with 
    | [| |] ->
        match tryLoadConfig(jsonFileName) with
        | Some cfg -> cfg
        | None ->
            AnsiConsole.MarkupLine($"[yellow]\"{jsonFileName}\" not detected. Starting configuration wizard...[/]")
            let cfg = newConfigWizard(app)
            saveConfig(jsonFileName, cfg)
            cfg

    | [| "--new" |] -> 
        AnsiConsole.MarkupLine("[yellow]Creating a new configuration...[/]")
        let cfg = newConfigWizard(app)
        saveConfig(jsonFileName, cfg)
        cfg

    | _ ->
        AnsiConsole.MarkupLine($"Invalid args: '{argv}'. Expected no args, or \"--edit\".")
        failwith "Invalid args."
