module Oracle.DB

let connectionString = @"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521)) (CONNECT_DATA=(SERVICE_NAME=XE))); User Id=C##ADVWORKS;Password=Oracle1;"

let toSql (query: SqlKata.Query) = 
    let compiler = SqlKata.Compilers.OracleCompiler()
    compiler.Compile(query).Sql
