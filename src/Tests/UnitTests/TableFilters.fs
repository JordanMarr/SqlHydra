module UnitTests.``Table Filters``

open NUnit.Framework
open SqlHydra.Domain
open SqlHydra.SchemaFilters

let tbl schema name = 
    { 
        Table.Schema = schema
        Table.Name = name
        Table.Catalog = ""
        Table.Columns = []
        Table.TotalColumns = 10
        Table.Type = TableType.Table
    }

[<Test>]
let ``Apply No Filters``() =
    let dboTbl1 = tbl "dbo" "tbl1"
    let dboTbl2 = tbl "dbo" "tbl2"
    let prodTbl1 = tbl "prod" "tbl1"
    let prodTbl2 = tbl "prod" "tbl2"
    let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

    let filters = { 
        Includes = [ ]
        Excludes = [ ] 
        Restrictions = Map.empty
    }

    let filteredTables = tables |> filterTables filters
    Assert.AreEqual(filteredTables, tables)

[<Test>]
let ``Apply Includes``() =
    let dboTbl1 = tbl "dbo" "tbl1"
    let dboTbl2 = tbl "dbo" "tbl2"
    let prodTbl1 = tbl "prod" "tbl1"
    let prodTbl2 = tbl "prod" "tbl2"
    let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

    let filters = { 
        Includes = [ "dbo/*" ]
        Excludes = [ ] 
        Restrictions = Map.empty
    }

    let filteredTables = tables |> filterTables filters
    Assert.AreEqual(filteredTables, [ dboTbl1; dboTbl2 ])

[<Test>]
let ``Apply Excludes``() =
    let dboTbl1 = tbl "dbo" "tbl1"
    let dboTbl2 = tbl "dbo" "tbl2"
    let prodTbl1 = tbl "prod" "tbl1"
    let prodTbl2 = tbl "prod" "tbl2"
    let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

    let filters = { 
        Includes = [ "*" ]
        Excludes = [ "dbo/*" ] 
        Restrictions = Map.empty
    }

    let filteredTables = tables |> filterTables filters
    Assert.AreEqual(filteredTables, [ prodTbl1; prodTbl2 ])

[<Test>]
let ``Apply Includes and Excludes``() =
    let dboTbl1 = tbl "dbo" "tbl1"
    let dboTbl2 = tbl "dbo" "tbl2"
    let prodTbl1 = tbl "prod" "tbl1"
    let prodTbl2 = tbl "prod" "tbl2"
    let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

    let filters = { 
        Includes = [ "dbo/*" ]
        Excludes = [ "*/*1" ] 
        Restrictions = Map.empty
    }

    let filteredTables = tables |> filterTables filters
    Assert.AreEqual(filteredTables, [ dboTbl2 ])

[<Test>]
let ``Apply Multiple Includes``() =
    let dboTbl1 = tbl "dbo" "tbl1"
    let dboTbl2 = tbl "dbo" "tbl2"
    let prodTbl1 = tbl "prod" "tbl1"
    let prodTbl2 = tbl "prod" "tbl2"
    let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

    let filters = { 
        Includes = [ "dbo/tbl1"; "prod/tbl2" ]
        Excludes = [ ] 
        Restrictions = Map.empty
    }

    let filteredTables = tables |> filterTables filters
    Assert.AreEqual(filteredTables, [ dboTbl1; prodTbl2 ])
