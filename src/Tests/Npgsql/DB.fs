module Npgsql.DB

#if LOCALHOST // localhost
let connectionString = @"Server=localhost;Port=54320;Database=Adventureworks;User Id=postgres;Password=postgres;Timeout=3"
#else // devcontainer: "npgsql"
let connectionString = @"Server=npgsql;Port=5432;Database=Adventureworks;User Id=postgres;Password=postgres;Timeout=3"
#endif

let toSql (query: SqlKata.Query) = 
    let compiler = SqlKata.Compilers.PostgresCompiler()
    compiler.Compile(query).Sql
