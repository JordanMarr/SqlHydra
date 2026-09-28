module UnitTests.``Column Filters``

open Swensen.Unquote
open NUnit.Framework
open SqlHydra.Domain
open SqlHydra.SchemaFilters

let col nm = 
    {
        Name = nm
        TypeMapping = { ClrType = ""; ColumnTypeAlias = ""; DbType = System.Data.DbType.String; ProviderDbType = None }
        IsNullable = false
        IsPK = false
        IsReadOnly = false
    }

let equalLists lst1 lst2 = 
    Assert.AreEqual(Set lst1, Set lst2, "Lists are not equal")

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

[<Test>]
let ``Table-level Include with Column Exclude keeps the other columns``() = 
    // #128: `include = [ "*" ]` has no column-level includes, which used to mean "no columns",
    // so a column exclude dropped every column (and so every table).
    let idCol = col "ID"
    let fnameCol = col "FName"
    let lnameCol = col "LName"
    let ageCol = col "Age"
    let columns = [ idCol; fnameCol; lnameCol; ageCol ]
    
    let filters = { 
        Includes = [ "*" ]
        Excludes = [ "dbo/Person.FName" ] 
        Restrictions = Map.empty
    }

    let filteredColumns = columns |> filterColumns filters "dbo" "Person" |> Seq.toList
    test <@ filteredColumns = [ idCol; lnameCol; ageCol ] @>

[<Test>]
let ``Column Exclude Without Includes keeps the other columns``() = 
    let idCol = col "ID"
    let fnameCol = col "FName"
    let columns = [ idCol; fnameCol ]
    
    let filters = { 
        Includes = [ ]
        Excludes = [ "*/Person.FName" ] 
        Restrictions = Map.empty
    }

    let filteredColumns = columns |> filterColumns filters "dbo" "Person" |> Seq.toList
    test <@ filteredColumns = [ idCol ] @>

[<Test>]
let ``Column Filtering Preserves Column Order``() = 
    // Filtering used to rebuild the list from a Set, sorting the generated fields by name.
    let columns = [ col "ID"; col "Zip"; col "rowguid"; col "Age"; col "City" ]
    
    let filters = { 
        Includes = [ "*.*" ]
        Excludes = [ "dbo/Person.City" ] 
        Restrictions = Map.empty
    }

    let filteredColumns = columns |> filterColumns filters "dbo" "Person" |> Seq.toList
    test <@ filteredColumns = [ col "ID"; col "Zip"; col "rowguid"; col "Age" ] @>
