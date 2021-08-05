
[<EntryPoint>]
let main argv = 
    
    //SampleApp.DapperFSharpExample.runQueries() |> Async.AwaitTask |> Async.RunSynchronously
    SampleApp.ReaderExample.runQueries() |> Async.AwaitTask |> Async.RunSynchronously
    //SampleApp.DonaldExample.runQueries() |> Async.AwaitTask |> Async.RunSynchronously
    0
