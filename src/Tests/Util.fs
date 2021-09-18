[<AutoOpen>]
module Util

open Expecto

let sequencedTestList nm = testList nm >> testSequenced
let fsequencedTestList nm = ftestList nm >> testSequenced

let categoryList cat subCat tests =
    testList cat [
        sequencedTestList subCat tests
    ]