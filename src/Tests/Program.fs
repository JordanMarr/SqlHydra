open Expecto
open Expecto.Logging

[<EntryPoint>]
let main argv =
    let testConfig =
        { defaultConfig with
            parallelWorkers = 1
            verbosity = LogLevel.Debug }

    let sequencedTestList nm = testList nm >> testSequenced

    let sqlServerTests = 
        [
            SqlServer.QueryUnitTests.tests
            SqlServer.QueryIntegrationTests.tests
            SqlServer.Generation.tests
        ]
        |> sequencedTestList "Sql Server Tests"
    
    let sqliteTests = 
        [
            Sqlite.Generation.tests
        ]
        |> sequencedTestList "Sqlite Tests"

    let unitTests = 
        [
            UnitTests.TomlConfigParser.tests
        ]
        |> sequencedTestList "Unit Tests"

    [
        sqlServerTests
        sqliteTests
        unitTests
    ]
    |> sequencedTestList ""
    |> runTests testConfig