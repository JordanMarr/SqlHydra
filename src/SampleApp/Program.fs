module SampleApp.Program

[<EntryPoint>]
let main argv = 
    task {
        do! DapperFSharpExample.runQueries()
        do! DapperExample.runQueries()
        do! ReaderExample.runQueries()
        do! DonaldExample.runQueries() 
        return 0
    }
    |> Async.AwaitTask 
    |> Async.RunSynchronously
