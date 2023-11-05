module SqlServer.DB

open Microsoft.Data.SqlClient

#if DOCKERHOST // devcontainer
let server = "mssql"
#else
let server = "localhost,12019"
#endif

let connectionString = $@"Server={server};Database=AdventureWorks;User=sa;Password=Password#123;Connect Timeout=3;TrustServerCertificate=True"

let getConnection() = 
    new SqlConnection(connectionString)

let openConnection() = 
    let conn = getConnection()
    conn.Open()
    conn

let toSql (query: SqlKata.Query) = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    compiler.Compile(query).Sql
