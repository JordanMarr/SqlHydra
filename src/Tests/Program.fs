open Expecto
open Expecto.Logging

[<EntryPoint>]
let main argv =
    let testConfig =
        { defaultConfig with
            parallelWorkers = 1
            verbosity = LogLevel.Debug }

    let sqlServerTests = 
        [
            SqlServer.Migration.migration
            SqlServer.Queries.tests
            SqlServer.QueryTextOutput.tests
            SqlServer.Generation.tests
        ]
        |> testList "Sql Server Tests"
    
    let sqliteTests = 
        [
            Sqlite.Generation.tests
        ]
        |> testList "Sqlite Tests"

    let unitTests = 
        [
            UnitTests.TomlConfigParser.tests
        ]
        |> testList "Unit Tests"

    [
        sqlServerTests
        sqliteTests
        unitTests
    ]
    |> testList ""
    |> runTests testConfig