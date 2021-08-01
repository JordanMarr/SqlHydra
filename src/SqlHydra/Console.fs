module Console

open Spectre.Console
open SqlHydra.Schema
open System
open Newtonsoft.Json

let newConfigWizard() = 
    let connStr = AnsiConsole.Ask<string>("Connection string:")
    let outputFile = AnsiConsole.Ask<string>("Generated filename (Ex: AdventureWorks.fs):")
    let ns = AnsiConsole.Ask<string>("Namespace:")
    let isCLIMutable = 
        let selection = SelectionPrompt<string>()
        selection.Title <- "Add CLIMutable attribute to generated records?"
        selection.AddChoices(["Yes"; "No"]) |> ignore
        AnsiConsole.Prompt(selection)
    
    { 
        Config.ConnectionString = connStr
        Config.OutputFile = outputFile
        Config.Namespace = ns
        Config.IsCLIMutable = isCLIMutable = "Yes"
        Config.Readers = 
            {
                ReadersConfig.IsEnabled = false
                ReadersConfig.ReaderType = ""
            }
    }

let fileName = "hydra.json"

let saveConfig (cfg: Config) = 
    let json = JsonConvert.SerializeObject(cfg)
    IO.File.WriteAllText(fileName, json)

let tryLoadConfig() = 
    if IO.File.Exists(fileName) then
        try
            let json = IO.File.ReadAllText(fileName)
            JsonConvert.DeserializeObject<SqlHydra.Schema.Config>(json) |> Some
        with ex -> None
    else 
        None

let getConfig() = 
    match tryLoadConfig() with
    | Some cfg -> cfg
    | None ->
        let cfg = newConfigWizard()
        saveConfig cfg
        cfg

