module SqlServerTests
open NUnit.Framework

[<Test>]
let getSchema() = 
    let cs = "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
    let tables, columns = SqlHydra.SqlServerGenrator.Schema.getSchema cs
    ()