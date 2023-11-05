module SqlServer.``selectTask Tests``

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
let ``select Task``() = task {
    let! results = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - select p``() = task {
    let! results = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            select p
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - toArray``() = task {
    let! results = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            toArray
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - mapList column``() = task {
    let! results = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            mapList p.FirstName
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - select entity - mapSeq column``() = task {
    let! results = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            select p
            mapSeq $"{p.FirstName} {p.LastName}"
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - select columns into - mapList column``() = task {
    let! results = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 10
            select (p.FirstName, p.LastName) into (fname, lname)
            mapList $"{fname} {lname}"
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - count``() = task {
    let! results = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            count
        }
        
    Expect.isTrue (results > 0) ""
}

[<Test>]
let ``selectTask - tryHead - Selected``() = task {
    let! result = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 1
            tryHead
        }
        
    Expect.isSome result ""
}

[<Test>]
let ``selectTask - tryHead - Mapped``() = task {
    let! result = 
        selectTask HydraReader.Read (Create openContext) {
            for p in Person.Person do
            take 1
            mapSeq $"{p.FirstName} {p.LastName}"
            tryHead
        }
        
    Expect.isSome result ""
}
