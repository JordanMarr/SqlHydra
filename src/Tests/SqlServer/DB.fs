module SqlServer.DB

open SqlHydra.Query
open Microsoft.Data.SqlClient

// devcontainer: "mssql"
let connectionString = @"Server=mssql;Database=AdventureWorksLT2019;User=sa;Password=Password#123;"

// localhost
//let connectionString = @"Server=localhost,12019;Database=AdventureWorksLT2019;User=sa;Password=Password#123;"
//let connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"

let getConnection() = 
    new SqlConnection(connectionString)

let openConnection() = 
    let conn = getConnection()
    conn.Open()
    conn

let openMaster() = 
    let conn = new SqlConnection(@"Server=mssql;Database=master;User=sa;Password=Password#123;")
    conn.Open()
    conn

let toSql<'T> (query: QuerySource<'T, SqlKata.Query>) = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    compiler.Compile(query.Query).Sql

let toSql'<'T> (query: SqlKata.Query) =
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    compiler.Compile(query).Sql