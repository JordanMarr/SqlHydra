module SqlUtils

open SqlHydra.Query
open System.Data.SqlClient
open SqlKata.Execution
open System.Collections.Generic
open SqlKata

let getConnection() = 
    new SqlConnection(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;")
    // Docker: "mssql"
    //new SqlConnection(@"Server=localhost,1433;Database=master;User=sa;Password=Password#123;")

let openConnection() = 
    let conn = getConnection()
    conn.Open()
    conn

let toSql<'T> (query: TypedQuery<'T>) = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    compiler.Compile(query.Query).Sql

let get<'T>(query: TypedQuery<'T>) = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    use connection = getConnection()
    use db = new QueryFactory(connection, compiler)
    db.Get<'T>(query.Query)

let getDict(query: Query) = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    use connection = getConnection()
    use db = new QueryFactory(connection, compiler)
    db.Get(query)
    |> Seq.cast<IDictionary<string,obj>>
    |> Seq.toArray
