module SqlServer.Migration

open Microsoft.SqlServer.Management.Common
open Microsoft.SqlServer.Management.Smo
open SqlServer.AdventureWorks
open FSharp.Control.Tasks.V2
open Expecto
open DB

let sqlFile = 
    let assembly = System.Reflection.Assembly.GetExecutingAssembly().Location |> System.IO.FileInfo
    let thisDir = assembly.Directory.Parent.Parent.Parent.FullName
    let relativePath = System.IO.Path.Combine(thisDir, "TestData", "AdventureWorksLT.sql")
    relativePath
   
let readSqlFile() = 
    System.IO.File.ReadAllText(sqlFile)

let migrate() = task {
    use conn = openConnection()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- "SELECT DB_ID('AdventureWorksLT2019')"
    match cmd.ExecuteScalar() with
    | :? System.DBNull -> 
        printfn "Creating AdventureWorksLT Database..."
        let sql = readSqlFile()
        let server = new Server(ServerConnection(conn))
        let result = server.ConnectionContext.ExecuteNonQuery(sql)
        printfn "Migration Result: %i" result
    | _ ->
        printfn "AdventureWorksLT Database already exists"
}

let migration = 
    testList "Migration" [
        testTask "AdventureWorksLT Migration" {
            do! migrate()
        }
    ]