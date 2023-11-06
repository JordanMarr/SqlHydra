module Oracle.DB

let connectionString = @"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521)) (CONNECT_DATA=(SERVICE_NAME=XEPDB1))); User Id=OT;Password=Oracle1;"

let toSql (query: SqlHydra.Query.SelectQuery) = 
    let compiler = SqlKata.Compilers.OracleCompiler()
    let sql = compiler.Compile(query.ToKataQuery()).Sql
    #if DEBUG
    printfn "toSql: %s" sql
    #endif
    sql
