module Sqlite.DB

let toSql (query: SqlKata.Query) = 
    let compiler = SqlKata.Compilers.SqliteCompiler()
    compiler.Compile(query).Sql
