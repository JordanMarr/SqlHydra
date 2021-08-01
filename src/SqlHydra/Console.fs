module Console

open Spectre.Console
open SqlHydra.Schema
open System
open Newtonsoft.Json

let newConfigWizard() = 
    { 
        Config.ConnectionString = ""
        Config.OutputFile = ""
        Config.Namespace = ""
        Config.IsCLIMutable = false
        Config.Readers = 
            {
                ReadersConfig.IsEnabled = false
                ReadersConfig.ReaderType = ""
            }
    }

let getConfig() = 
    let fileName = "hydra.json"
    let configExists = IO.File.Exists(fileName)
    if configExists then
        let json = IO.File.ReadAllText(fileName)
        JsonConvert.DeserializeObject<SqlHydra.Schema.Config>(json)
    else
        newConfigWizard()

