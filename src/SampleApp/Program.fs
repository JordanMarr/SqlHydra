
[<EntryPoint>]
let main argv = 
    
    SampleApp.DapperFSharpExample.runQueries() |> Async.AwaitTask |> Async.RunSynchronously
    0
