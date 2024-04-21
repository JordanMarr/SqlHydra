module SqlServer.``selectTask Tests``

open SqlHydra.Query
open Swensen.Unquote
open NUnit.Framework
open DB

#if NET6_0
open SqlServer.AdventureWorksNet6
#endif
#if NET8_0
open SqlServer.AdventureWorksNet8
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

[<Test>]
let ``selectTask - no select``() = task {
    let! results = 
        selectTask HydraReader.Read openContext {
            for p in Person.Person do
            take 10
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - select p``() = task {
    let! results = 
        selectTask HydraReader.Read openContext {
            for p in Person.Person do
            take 10
            select p
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - toArray``() = task {
    let! results = 
        selectTask HydraReader.Read openContext {
            for p in Person.Person do
            take 10
            toArray
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - mapList column``() = task {
    let! results = 
        selectTask HydraReader.Read openContext {
            for p in Person.Person do
            take 10
            mapList p.FirstName
        }
        
    gt0 results
}

[<Test>]
let ``selectTask - select entity - mapSeq column``() = task {
    let! results = 
        selectTask HydraReader.Read openContext {
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
        selectTask HydraReader.Read openContext {
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
        selectTask HydraReader.Read openContext {
            for p in Person.Person do
            count
        }
        
    results >! 0
}

[<Test>]
let ``selectTask - tryHead - Selected``() = task {
    let! result = 
        selectTask HydraReader.Read openContext {
            for p in Person.Person do
            take 1
            tryHead
        }
        
    result |> Option.isSome =! true
}

[<Test>]
let ``selectTask - tryHead - Mapped``() = task {
    let! result = 
        selectTask HydraReader.Read openContext {
            for p in Person.Person do
            take 1
            mapSeq $"{p.FirstName} {p.LastName}"
            tryHead
        }
        
    result |> Option.isSome =! true
}
