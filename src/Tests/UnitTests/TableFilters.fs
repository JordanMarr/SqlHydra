module UnitTests.TableFilters

open Expecto
open System
open SqlHydra
open SqlHydra.Domain
open System.Globalization

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

    let cfg filters = 
        {
            Config.ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
            Config.OutputFile = "AdventureWorks.fs"
            Config.Namespace = "SampleApp.AdventureWorks"
            Config.IsCLIMutable = true
            Config.Readers = None
            Config.Filters = filters
        }

    categoryList "Unit Tests" "Table Filters" [
        
        test "Apply No Filters" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let cfg = cfg { 
                Includes = [ ]
                Excludes = [ ] 
            }

            let filteredTables = tables |> applyFilters cfg.Filters
            Expect.equal filteredTables tables ""
        }

        test "Apply Includes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let cfg = cfg { 
                Includes = [ "dbo/*" ]
                Excludes = [ ] 
            }

            let filteredTables = tables |> applyFilters cfg.Filters
            Expect.equal filteredTables [ dboTbl1; dboTbl2 ] ""
        }

        test "Apply Excludes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let cfg = cfg { 
                Includes = [ "*" ]
                Excludes = [ "dbo/*" ] 
            }

            let filteredTables = tables |> applyFilters cfg.Filters
            Expect.equal filteredTables [ prodTbl1; prodTbl2 ] ""
        }

        test "Apply Includes and Excludes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let cfg = cfg { 
                Includes = [ "dbo/*" ]
                Excludes = [ "*/*1" ] 
            }

            let filteredTables = tables |> applyFilters cfg.Filters
            Expect.equal filteredTables [ dboTbl2 ] ""
        }

        test "Apply Multiple Includes" {
            let dboTbl1 = tbl "dbo" "tbl1"
            let dboTbl2 = tbl "dbo" "tbl2"
            let prodTbl1 = tbl "prod" "tbl1"
            let prodTbl2 = tbl "prod" "tbl2"
            let tables = [ dboTbl1; dboTbl2; prodTbl1; prodTbl2 ]

            let cfg = cfg { 
                Includes = [ "dbo/tbl1"; "prod/tbl2" ]
                Excludes = [ ] 
            }

            let filteredTables = tables |> applyFilters cfg.Filters
            Expect.equal filteredTables [ dboTbl1; prodTbl2 ] ""
        }
    ]

