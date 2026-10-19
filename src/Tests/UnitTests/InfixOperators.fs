module UnitTests.``Infix Operators``

open NUnit.Framework
open Swensen.Unquote
open SqlHydra.Query

[<Test>]
let ``a DLL that loads while the startup scan is running still gets scanned``() =
    let extension = typeof<SqlHydraInfixOperatorAttribute>.Assembly
    let scanned = ResizeArray()
    let mutable raiseLoad = ignore
    InfixOperators.watch
        (fun handler -> raiseLoad <- handler)
        // The DLL loads during the scan: the event fires, but the DLL isn't in the list.
        (fun () -> raiseLoad extension; [||])
        scanned.Add
    List.ofSeq scanned =! [ extension ]
