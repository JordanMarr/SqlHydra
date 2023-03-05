module SqlServer.SelectTaskTests

open SqlHydra.Query
open Expecto
open DB

#if NET5_0
open SqlServer.AdventureWorksNet5
#endif
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

[<Tests>]
let selectTests = 
    categoryList "SqlServer" "selectTask" [
        
        testTask "selectTask" {
            let! results = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 10
                }
        
            gt0 results
        }
        
        testTask "selectTask - select" {
            let! results = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 10
                    select p
                }
        
            gt0 results
        }

        testTask "selectTask - toArray" {
            let! results = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 10
                    toArray
                }
        
            gt0 results
        }
        
        testTask "selectTask - mapList column" {
            let! results = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 10
                    mapList p.FirstName
                }
                
            gt0 results
        }

        testTask "selectTask - select entity - mapSeq column" {
            let! results = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 10
                    select p
                    mapSeq $"{p.FirstName} {p.LastName}"
                }
        
            gt0 results
        }
        
        testTask "selectTask - select columns into - mapList column" {
            let! results = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 10
                    select (p.FirstName, p.LastName) into (fname, lname)
                    mapList $"{fname} {lname}"
                }
                
            gt0 results
        }
        
        testTask "selectTask - count" {
            let! results = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    count
                }
        
            Expect.isTrue (results > 0) ""
        }

        testTask "selectTask - tryHead - Selected" {
            let! result = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 1
                    tryHead
                }
        
            Expect.isSome result ""
        }

        testTask "selectTask - tryHead - Mapped" {
            let! result = 
                selectTask HydraReader.Read (Create openContext) {
                    for p in Person.Person do
                    take 1
                    mapSeq $"{p.FirstName} {p.LastName}"
                    tryHead
                }
        
            Expect.isSome result ""
        }
        
    ]
    