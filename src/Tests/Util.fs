[<AutoOpen>]
module Util

open Expecto

let sequencedTestList nm = testList nm >> testSequenced
let fsequencedTestList nm = ftestList nm >> testSequenced
