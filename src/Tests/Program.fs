open Expecto
open Expecto.Logging

[<EntryPoint>]
let main argv =
    let testConfig =
        { defaultConfig with
            parallelWorkers = 1
            verbosity = LogLevel.Debug }

    let sqlHydraQueryTests = 
        [
            SqlServerQueries.tests
            QueryTextOutput.tests
        ]
        |> testList "SqlHydra.Query Tests"
    
    let sqlHydraGenerators = 
        [
            SqlServerTests.tests
            SqliteTests.tests
            TomlConfigParser.tests
        ]
        |> testList "SqlHydra.Generators Tests"

    [
        sqlHydraQueryTests
        sqlHydraGenerators
    ]
    |> testList ""
    |> runTests testConfig