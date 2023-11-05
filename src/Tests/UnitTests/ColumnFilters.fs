module UnitTests.``Column Filters``

open Expecto
open NUnit.Framework
open SqlHydra.Domain
open SqlHydra.SchemaFilters

let col nm = 
    {
        Name = nm
        TypeMapping = { ClrType = ""; ColumnTypeAlias = ""; DbType = System.Data.DbType.String; ReaderMethod = ""; ProviderDbType = None }
        IsNullable = false
        IsPK = false
    }

let equalLists lst1 lst2 = 
    Expect.equal (Set lst1) (Set lst2) "Lists are not equal"

[<Test>]
let ``Include All and No Excludes``() = 
    let idCol = col "ID"
    let fnameCol = col "FName"
    let lnameCol = col "LName"
    let ageCol = col "Age"
    let columns = [ idCol; fnameCol; lnameCol; ageCol ]
    
    let filters = { 
        Includes = [ "*.*" ]
        Excludes = [ ] 
        Restrictions = Map.empty
    }

    let filteredColumns = columns |> filterColumns filters "dbo" "Person"
    equalLists columns filteredColumns

[<Test>]
let ``Include All and Exclude FName and LName``() = 
    let idCol = col "ID"
    let fnameCol = col "FName"
    let lnameCol = col "LName"
    let ageCol = col "Age"
    let columns = [ idCol; fnameCol; lnameCol; ageCol ]
    
    let filters = { 
        Includes = [ "*.*" ]
        Excludes = [ "dbo/Person.FName"; "*/Person.LName" ] 
        Restrictions = Map.empty
    }

    let filteredColumns = columns |> filterColumns filters "dbo" "Person"
    equalLists filteredColumns [ idCol; ageCol ]

[<Test>]
let ``Ignore filter if no table match``() = 
    let idCol = col "ID"
    let fnameCol = col "FName"
    let lnameCol = col "LName"
    let ageCol = col "Age"
    let columns = [ idCol; fnameCol; lnameCol; ageCol ]
    
    let filters = { 
        Includes = [ "*.*" ]
        Excludes = [ "Instrument.Age" ] 
        Restrictions = Map.empty
    }

    let filteredColumns = columns |> filterColumns filters "dbo" "Person"
    equalLists filteredColumns columns

[<Test>]
let ``Exclude Only Underscore Columns``() = 
    let idCol = col "ID"
    let fnameCol = col "_FName"
    let lnameCol = col "_LName"
    let ageCol = col "Age"
    let columns = [ idCol; fnameCol; lnameCol; ageCol ]
    
    let filters = { 
        Includes = [ "*.*" ]
        Excludes = [ "*._*" ] 
        Restrictions = Map.empty
    }

    let filteredColumns = columns |> filterColumns filters "dbo" "Person"
    equalLists filteredColumns [ idCol; ageCol ]
