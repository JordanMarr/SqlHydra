open System
open SqlHydra
open System.Text.Json

[<EntryPoint>]
let main argv =
    match argv with
    | [| connectionString; schemaOutputPath |] -> 
        let schema = SqlServerSchemaProvider.getSchema connectionString
        Schema.serialize schemaOutputPath schema
        0
    | _ ->
        1
