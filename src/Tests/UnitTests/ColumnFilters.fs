module UnitTests.ColumnFilters

open Expecto
open SqlHydra.Domain
open SqlHydra.SchemaFilters

[<Tests>]
let tests =     

    let col nm = 
        {
            Name = nm
            TypeMapping = { ClrType = ""; ColumnTypeAlias = ""; DbType = System.Data.DbType.String; ReaderMethod = ""; ProviderDbType = None }
            IsNullable = false
            IsPK = false
        }

    let equalLists lst1 lst2 = 
        Expect.equal (Set lst1) (Set lst2) "Lists are not equal"

    categoryList "Unit Tests" "Column Filters" [
        
        test "Include All and No Excludes" {
            let idCol = col "ID"
            let fnameCol = col "FName"
            let lnameCol = col "LName"
            let ageCol = col "Age"
            let columns = [ idCol; fnameCol; lnameCol; ageCol ]
            
            let filters = { 
                Includes = [ "*.*" ]
                Excludes = [ ] 
            }

            let filteredColumns = columns |> filterColumns filters "dbo" "Person"
            equalLists columns filteredColumns
        }

        test "Include All and Exclude FName and LName" {
            let idCol = col "ID"
            let fnameCol = col "FName"
            let lnameCol = col "LName"
            let ageCol = col "Age"
            let columns = [ idCol; fnameCol; lnameCol; ageCol ]
            
            let filters = { 
                Includes = [ "*.*" ]
                Excludes = [ "dbo/Person.FName"; "*/Person.LName" ] 
            }

            let filteredColumns = columns |> filterColumns filters "dbo" "Person"
            equalLists filteredColumns [ idCol; ageCol ]
        }

        test "Ignore filter if no table match" {
            let idCol = col "ID"
            let fnameCol = col "FName"
            let lnameCol = col "LName"
            let ageCol = col "Age"
            let columns = [ idCol; fnameCol; lnameCol; ageCol ]
            
            let filters = { 
                Includes = [ "*.*" ]
                Excludes = [ "Instrument.Age" ] 
            }

            let filteredColumns = columns |> filterColumns filters "dbo" "Person"
            equalLists filteredColumns columns
        }

        test "Exclude Only Underscore Columns" {
            let idCol = col "ID"
            let fnameCol = col "_FName"
            let lnameCol = col "_LName"
            let ageCol = col "Age"
            let columns = [ idCol; fnameCol; lnameCol; ageCol ]
            
            let filters = { 
                Includes = [ "*.*" ]
                Excludes = [ "*._*" ] 
            }

            let filteredColumns = columns |> filterColumns filters "dbo" "Person"
            equalLists filteredColumns [ idCol; ageCol ]
        }
    ]