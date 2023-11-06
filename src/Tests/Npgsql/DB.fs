module Npgsql.DB

#if DOCKERHOST // devcontainer
let connectionString = @"Server=npgsql;Port=5432;Database=Adventureworks;User Id=postgres;Password=postgres;Timeout=3"
#else
let connectionString = @"Server=localhost;Port=54320;Database=Adventureworks;User Id=postgres;Password=postgres;Timeout=3"
#endif

let toSql (query: SqlHydra.Query.SelectQuery) = 
    let compiler = SqlKata.Compilers.PostgresCompiler()
    let sql = compiler.Compile(query.ToKataQuery()).Sql
    #if DEBUG
    printfn "toSql: %s" sql
    #endif
    sql
