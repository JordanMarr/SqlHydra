module Oracle.QueryIntegrationTests

open Expecto
open SqlHydra.Query
open DB
open Oracle.ManagedDataAccess.Client
#if NET5_0
open Oracle.AdventureWorksNet5
#endif
#if NET6_0
open Oracle.AdventureWorksNet6
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.OracleCompiler()
    let conn = new OracleConnection(connectionString)
    conn.Open()
    new QueryContext(conn, compiler)

// Tables
let regionsTable =              table<``C##ADVWORKS``.REGIONS>              |> inSchema (nameof ``C##ADVWORKS``)

[<Tests>]
let tests = 
    categoryList "Oracle" "Query Integration Tests" [

        
        //testTask "Insert and Get Id" {
        //    use ctx = openContext()
        //    ctx.BeginTransaction()

        //    let! regionId = 
        //        insert {
        //            for r in regionsTable do
        //            entity 
        //                {
        //                    ``C##ADVWORKS``.REGIONS.REGION_ID = 0 // PK
        //                    ``C##ADVWORKS``.REGIONS.REGION_NAME = "Test Region"
        //                }
        //            getId r.REGION_ID
        //        }
        //        |> ctx.InsertAsync

        //    let! region = 
        //        select {
        //            for r in regionsTable do
        //            where (r.REGION_ID = regionId)
        //        }
        //        |> ctx.ReadOneAsync HydraReader.Read
            
        //    match region with
        //    | Some (r: ``C##ADVWORKS``.REGIONS) -> 
        //        Expect.isTrue (r.REGION_ID > 0) "Expected REGION_ID to be greater than 0"
        //    | None -> 
        //        failwith "Expected to query a region row."
        //}

    ]