module SqlHydra.Query.QuotationVisitor
open System
open FSharp.Quotations
open FSharp.Quotations.Patterns
open FSharp.Quotations.DerivedPatterns

let notImpl() = raise (NotImplementedException())
let notSupported msg = raise (NotSupportedException msg)

/// Returns the `for` table binding name.
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
        | Value (null, t) when t.Name = "Unit" -> Some "_" // Handles edge case `_` assignment. Ex: `for _ in Tables.Person do`
        | Var v -> Some v.Name
        | _ -> notImpl()
    visit f |> Option.get

/// Throws an "unsupported" exception if the underscore `_` is detected when it should not be allowed.
let allowUnderscore (isAllowed: bool) (forName: string) = 
    match isAllowed, forName with
    | false, "_" -> notSupported "The underscore `_` table assignment is not supported by this builder."
    | _ -> forName
