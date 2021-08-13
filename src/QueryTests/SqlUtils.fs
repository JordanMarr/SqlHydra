module SqlUtils

open SqlHydra.Query
open System.Data
open System.Data.SqlClient
open SqlKata.Execution
open System.Collections.Generic
open SqlKata
open System.Data.Common
open FSharp.Control.Tasks.V2

let toSql<'T> (query: Query<'T>) = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    compiler.Compile(query.Query).Sql

let getConnection() = new SqlConnection(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;")

let openConnection() = 
    let conn = getConnection()
    conn.Open()
    conn

let get<'T>(query: Query<'T>) = 
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

//let read (reader: System.Data.SqlClient.SqlDataReader -> 'T list) (query: Query<'T>) = 
//    ()

//let private executeReader<'T> (conn: IDbConnection) (mapRow: IDataReader -> 'T) (query: Query) =
//    let compiler = SqlKata.Compilers.SqlServerCompiler()    
//    use cmd = conn.CreateCommand()
//    let compiledQuery = compiler.Compile(query)
    
//    cmd.CommandText <- compiledQuery.Sql
//    for b in compiledQuery.NamedBindings do
//        let p = cmd.CreateParameter()
//        p.ParameterName <- b.Key
//        p.Value <- b.Value
//        cmd.Parameters.Add(p) |> ignore

//    use reader = cmd.ExecuteReader()
//    [ while reader.Read() do
//        mapRow reader ]

//// Allows results 'R to vary from initial query type 'T.
//let executeQuery<'T, 'R> (conn: SqlConnection) (mapRow: SqlDataReader -> 'R) (query: Query) =
//    let compiler = SqlKata.Compilers.SqlServerCompiler()    
//    use cmd = conn.CreateCommand()
//    let compiledQuery = compiler.Compile(query)
    
//    cmd.CommandText <- compiledQuery.Sql
//    for b in compiledQuery.NamedBindings do
//        let p = cmd.CreateParameter()
//        p.ParameterName <- b.Key
//        p.Value <- b.Value
//        cmd.Parameters.Add(p) |> ignore

//    use reader = cmd.ExecuteReader()
//    [ while reader.Read() do
//        mapRow reader ]

//let read2 fn1 fn2 = fun () -> fn1(), fn2()
//let read3 fn1 fn2 fn3 = fun () -> fn1(), fn2(), fn3()
//let read4 fn1 fn2 fn3 fn4 = fun () -> fn1(), fn2(), fn3(), fn4()
//let read5 fn1 fn2 fn3 fn4 fn5 = fun () -> fn1(), fn2(), fn3(), fn4(), fn5()



//let inline (>=>) fn1 fn2= fun () -> fn1(), fn2()

//let (>&>) fn1and2 fn3 = 
//    fun () -> 
//        fn1and2(), fn3()
