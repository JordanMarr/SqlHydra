module SqlServer.``selectAsync Tests``

open SqlHydra.Query
open Expecto
open NUnit.Framework
open DB

#if NET6_0
open SqlServer.AdventureWorksNet6
#endif
#if NET7_0
open SqlServer.AdventureWorksNet7
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

[<Test>]
let ``selectAsync - no select``() = async {
    let! results = 
        selectAsync HydraReader.Read (Create openContext) {
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            take 10
            mapArray $"{o.SalesOrderNumber} - {d.LineTotal} - {d.ModifiedDate.ToShortDateString()}"
        }
        
    gt0 results
}

[<Test>]
let ``selectAsync - select p``() = async {
    let! results = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            select p
        }
        
    gt0 results
}

[<Test>]
let ``selectAsync - toArray``() = async {
    let! results = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            select p
            toArray
        }
        
    gt0 results
}

[<Test>]
let ``selectAsync - mapList column``() = async {
    let! results = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            mapList p.FirstName
        }
        
    gt0 results
}

[<Test>]
let ``selectAsync - select entity - mapSeq column``() = async {
    let! results = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            select p
            mapSeq $"{p.FirstName} {p.LastName}"
        }
        
    gt0 results
}

[<Test>]
let ``selectAsync - select columns into - mapList column``() = async {
    let! results = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            select (p.FirstName, p.LastName) into (fname, lname)
            mapList $"{fname} {lname}"
        }
        
    gt0 results
}

[<Test>]
let ``selectAsync - count``() = async {
    let! results = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            count
        }
        
    Expect.isTrue (results > 0) ""
}

[<Test>]
let ``selectAsync - tryHead - Selected``() = async {
    let! result = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 1
            tryHead
        }
        
    Expect.isSome result ""
}

[<Test>]
let ``selectAsync - tryHead - Mapped``() = async {
    let! result = 
        selectAsync HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 1
            mapSeq $"{p.FirstName} {p.LastName}"
            tryHead
        }
        
    Expect.isSome result ""
}

