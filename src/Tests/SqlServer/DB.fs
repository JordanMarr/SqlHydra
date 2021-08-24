module SqlServer.DB

open SqlHydra.Query
open Microsoft.Data.SqlClient

// Docker: "mssql"
let connectionString = @"Server=localhost,1433;Database=AdventureWorksLT2019;User=sa;Password=Password#123;"

let getConnection() = 
    new SqlConnection(connectionString)

let openConnection() = 
    let conn = getConnection()
    conn.Open()
    conn

let toSql<'T> (query: TypedQuery<'T>) = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    compiler.Compile(query.Query).Sql
