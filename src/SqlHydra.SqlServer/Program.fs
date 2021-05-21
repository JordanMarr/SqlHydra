open System
open SqlHydra
open SqlHydra.SqlServer
open System.Text.Json

[<EntryPoint>]
let main argv =
    match argv with
    | [| connectionString; schemaOutputPath |] -> 
        let schema = SqlServerSchemaProvider.getSchema connectionString
        SqlHydra.Utils.serializeSchema schemaOutputPath schema
        0
    | _ ->
        1
