open SqlHydra
open SqlHydra.Sqlite

[<EntryPoint>]
let main argv =
    match argv with
    | [| connectionString; schemaOutputPath |] -> 
        let schema = SqliteSchemaProvider.getSchema connectionString
        Schema.serialize schemaOutputPath schema
        0
    | _ ->
        1
