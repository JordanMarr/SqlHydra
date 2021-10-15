module UnitTests.TableFilters

open Expecto
open SqlHydra.Domain

[<Tests>]
let tests = 

    let tbl schema name = 
        { 
            Table.Schema = schema
            Table.Name = name
            Table.Catalog = ""
            Table.Columns = []
            Table.TotalColumns = 10
            Table.Type = TableType.Table
        }

    categoryList "Unit Tests" "Table Filters" [
        
        test "Apply No Filters" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let filters = { 
                Includes = [ ]
                Excludes = [ ] 
            }

            let filteredTables = tables |> filterTables filters
            Expect.equal filteredTables tables ""
        }

        test "Apply Includes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let filters = { 
                Includes = [ "dbo/*" ]
                Excludes = [ ] 
            }

            let filteredTables = tables |> filterTables filters
            Expect.equal filteredTables [ dboTbl1; dboTbl2 ] ""
        }

        test "Apply Excludes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let filters = { 
                Includes = [ "*" ]
                Excludes = [ "dbo/*" ] 
            }

            let filteredTables = tables |> filterTables filters
            Expect.equal filteredTables [ prodTbl1; prodTbl2 ] ""
        }

        test "Apply Includes and Excludes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let filters = { 
                Includes = [ "dbo/*" ]
                Excludes = [ "*/*1" ] 
            }

            let filteredTables = tables |> filterTables filters
            Expect.equal filteredTables [ dboTbl2 ] ""
        }

        test "Apply Multiple Includes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let filters = { 
                Includes = [ "dbo/tbl1"; "prod/tbl2" ]
                Excludes = [ ] 
            }

            let filteredTables = tables |> filterTables filters
            Expect.equal filteredTables [ dboTbl1; prodTbl2 ] ""
        }
    ]
