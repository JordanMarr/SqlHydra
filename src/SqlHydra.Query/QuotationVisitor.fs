module SqlHydra.Query.QuotationVisitor
open System
open FSharp.Quotations
open FSharp.Quotations.Patterns
open FSharp.Quotations.DerivedPatterns

let visitFor<'T> (f: Expr<'T -> QuerySource<'T>>) =
    let rec visit expr =
        match expr with
        | Lambdas (args, body) -> visit body
        | Let (_, _, cont) -> visit cont
        | NewTuple items ->
            match items[0] with
            | Var x -> Some x.Name
            | _ -> None
        | TupleGet (items, i) -> visit items
        | Application (_, e) -> visit e
        | Call (_, _, args) -> args |> Seq.head |> visit
        | Var v -> Some v.Name
        | _ -> failwith "bang"
    visit f |> Option.get
    