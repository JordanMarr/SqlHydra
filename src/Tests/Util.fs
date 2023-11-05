[<AutoOpen>]
module Util

open NUnit.Framework

/// Sequence length is > 0.
let gt0 (items: 'Item seq) =
    Assert.IsTrue(items |> Seq.length > 0, "Expected more than 0.")
