[<AutoOpen>]
module Util

open Expecto

let sequencedTestList nm = testList nm >> testSequenced
let fsequencedTestList nm = ftestList nm >> testSequenced

let categoryList cat subCat tests =
    testList cat [
        sequencedTestList subCat tests
    ]

/// Sequence length is > 0.
let gt0 (items: 'Item seq) =
    Expect.isTrue (items |> Seq.length > 0) "Expected more than 0."
