open Expecto
open Expecto.Logging

[<EntryPoint>]
let main argv =
    let testConfig =
        { defaultConfig with
            parallelWorkers = 1
            verbosity = LogLevel.Debug }

    Tests.runTestsInAssembly testConfig argv