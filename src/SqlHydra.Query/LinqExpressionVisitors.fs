module internal SqlHydra.Query.LinqExpressionVisitors

open System
open System.Linq.Expressions
open System.Reflection
open FastExpressionCompiler

let notImpl() = raise (NotImplementedException())
let notImplMsg msg = raise (NotImplementedException msg)

/// True when a method call is a SqlHydra query function (`isIn`, `like`, `inlineValue`,
/// `rawExpr`, `sqlFn` wrappers, ...) rather than an ordinary .NET call.
/// A SqlHydra function has a stub body (`Unchecked.defaultof<_>`), so evaluating one yields
/// null/0 rather than anything meaningful: it must be rendered as SQL, never compiled and run.
let isSqlHydraFunction (mi: MethodInfo) =
    mi.Module.Name = "SqlHydra.Query.dll"

/// Aggregate method names recognized by the visitor. Used by visitSqlFn / pattern matchers.
/// Keep in sync with QueryFunctions.Aggregates.
let aggregateMethodNames =
    System.Collections.Generic.HashSet<string>([
        nameof minBy; nameof maxBy; nameof sumBy; nameof avgBy
        nameof countBy; nameof avgByAs; nameof countDistinct
    ])

/// Renders an aggregate function call. Special-cases COUNTDISTINCT → COUNT(DISTINCT col).
let renderAggregate (aggType: string) (col: string) =
    if aggType = "COUNTDISTINCT" then $"COUNT(DISTINCT {col})"
    else $"{aggType}({col})"

/// Derives the SQL aggregate type name from an aggregate method name (e.g. `countBy` → `COUNT`, `avgByAs` → `AVG`).
let aggTypeOf (name: string) = name.Replace("By", "").Replace("As", "").ToUpper()

/// Maps an F# CLR type to its SQL CAST target type name.
let sqlTypeForClrType (t: System.Type) =
    let t =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>
        then t.GetGenericArguments().[0]
        elif t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<System.Nullable<_>>
        then System.Nullable.GetUnderlyingType(t)
        else t
    if t = typeof<float> || t = typeof<double> then "FLOAT"
    elif t = typeof<float32> || t = typeof<single> then "REAL"
    elif t = typeof<int> || t = typeof<int32> then "INTEGER"
    elif t = typeof<int64> then "BIGINT"
    elif t = typeof<int16> then "SMALLINT"
    elif t = typeof<decimal> then "NUMERIC"
    elif t = typeof<string> then "TEXT"
    elif t = typeof<bool> then "BOOLEAN"
    else t.Name


[<AutoOpen>]
module VisitorPatterns =

    let (|Lambda|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Lambda -> Some (exp :?> LambdaExpression)
        | _ -> None

    let (|Unary|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.ArrayLength
        | ExpressionType.Convert
        | ExpressionType.ConvertChecked
        | ExpressionType.Negate
        | ExpressionType.UnaryPlus
        | ExpressionType.NegateChecked
        | ExpressionType.Not
        | ExpressionType.Quote
        | ExpressionType.TypeAs -> Some (exp :?> UnaryExpression)
        | _ -> None

    let (|Binary|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Add
        | ExpressionType.AddChecked
        | ExpressionType.And
        | ExpressionType.AndAlso
        | ExpressionType.ArrayIndex
        | ExpressionType.Coalesce
        | ExpressionType.Divide
        | ExpressionType.Equal
        | ExpressionType.ExclusiveOr
        | ExpressionType.GreaterThan
        | ExpressionType.GreaterThanOrEqual
        | ExpressionType.LeftShift
        | ExpressionType.LessThan
        | ExpressionType.LessThanOrEqual
        | ExpressionType.Modulo
        | ExpressionType.Multiply
        | ExpressionType.MultiplyChecked
        | ExpressionType.NotEqual
        | ExpressionType.Or
        | ExpressionType.OrElse
        | ExpressionType.Power
        | ExpressionType.RightShift
        | ExpressionType.Subtract
        | ExpressionType.SubtractChecked -> Some (exp :?> BinaryExpression)
        | _ -> None

    let (|MethodCall|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Call -> Some (exp :?> MethodCallExpression)    
        | _ -> None
    let (|New|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.New -> Some (exp :?> NewExpression)
        | _ -> None

    let (|Constant|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Constant -> Some (exp :?> ConstantExpression)
        | _ -> None
    
    let (|ImplConvertConstant|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Convert ->
            // Handles implicit conversion. Ex: upcasting int to an int64
            let unary = exp :?> UnaryExpression
            match unary.Operand with
            | Constant c when unary.Type.IsPrimitive -> Some c
            | _ -> None
            //Some (unary.Operand, unary.Type)
        | ExpressionType.Call -> 
            // Handles implicit conversion. Ex: casting an int to a decimal
            let mc = exp :?> MethodCallExpression
            match mc.Method.Name, mc.Arguments |> Seq.toList with
            | "op_Implicit", [ Constant c ] -> Some c
            | _ -> None
        | _ -> None
    
    let (|ArrayInit|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.NewArrayInit -> 
            let arrayExp = exp :?> NewArrayExpression
            Some (arrayExp.Expressions |> Seq.map (function | Constant c -> c.Value | _ -> notImplMsg "Unable to unwrap array value."))
        | _ -> None

    let rec unwrapListExpr (lstValues: obj list, lstExp: MethodCallExpression) =
        if lstExp.Arguments.Count > 0 then
            match lstExp.Arguments.[0] with
            | Constant c -> unwrapListExpr (lstValues @ [c.Value], (lstExp.Arguments.[1] :?> MethodCallExpression))
            | _ -> notImpl()
        else 
            lstValues    

    let (|ListInit|_|) (exp: Expression) = 
        match exp with
        | MethodCall c when c.Method.Name = "Cons" ->
            let values = unwrapListExpr ([], c)
            Some values
        | _ -> None

    let (|Member|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.MemberAccess -> Some (exp :?> MemberExpression)
        | _ -> None

    let (|BoolMember|_|) (exp: Expression) = 
        match exp with
        | Member m when m.Type = typeof<bool> -> Some m
        | _ -> None

    let (|BoolConstant|_|) (exp: Expression) = 
        match exp with
        | Constant c when c.Type = typeof<bool> -> Some (c.Value :?> bool)
        | _ -> None

    let (|Parameter|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Parameter -> Some (exp :?> ParameterExpression)
        | _ -> None

[<AutoOpen>]
module SqlPatterns = 

    let (|Not|_|) (exp: Expression) = 
        match exp.NodeType with
        | ExpressionType.Not -> Some ((exp :?> UnaryExpression).Operand)
        | _ -> None

    let (|BinaryAnd|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.And
        | ExpressionType.AndAlso -> Some (exp :?> BinaryExpression)
        | _ -> None

    let (|BinaryOr|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Or
        | ExpressionType.OrElse -> Some (exp :?> BinaryExpression)
        | _ -> None

    let (|BinaryCompare|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Equal
        | ExpressionType.NotEqual
        | ExpressionType.GreaterThan
        | ExpressionType.GreaterThanOrEqual
        | ExpressionType.LessThan
        | ExpressionType.LessThanOrEqual -> Some (exp :?> BinaryExpression)
        | _ -> None

    let (|Call|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Call -> Some (exp :?> MethodCallExpression)
        | _ -> None

    let isOptionType (t: Type) = 
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>>

    let isNullableType (t: Type) = 
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Nullable<_>>

    let isOptionOrNullableType (t: Type) = 
        t.IsGenericType && (
            let genericTypeDef = t.GetGenericTypeDefinition()
            genericTypeDef = typedefof<Option<_>> || 
            genericTypeDef = typedefof<Nullable<_>>
        )

    let tryGetMember(x: Expression) = 
        match x with
        | Member m when m.Expression = null -> 
            None
        | Member m when m.Expression.NodeType = ExpressionType.Parameter || m.Expression.NodeType = ExpressionType.MemberAccess -> 
            Some m
        | MethodCall opt when opt.Type |> isOptionType ->        
            if opt.Arguments.Count > 0 then
                // Option.Some
                match opt.Arguments.[0] with
                | Member m -> Some m
                | _ -> None
            else None
        | MethodCall nul when nul.Type |> isNullableType -> 
            if nul.Arguments.Count > 0 then
                // Nullable.Value
                match nul.Arguments.[0] with
                | Member m -> Some m
                | _ -> None
            else None
        | Unary u when u.Operand.NodeType = ExpressionType.MemberAccess -> 
            Some (u.Operand :?> MemberExpression)
        | _ -> 
            None
                
    // Extract constant value from nested object/properties
    let rec unwrapMember (m: MemberExpression) =
        match m.Expression with
        | Constant c -> Some c.Value
        | Member m -> unwrapMember m
        | _ -> None

    let compileAndEvaluateExpression (exp: Expression) = 
        try
            let lambda = Expression.Lambda(exp)
            let compiled = lambda.CompileFast()
            compiled.DynamicInvoke()
        with ex ->  
            notImplMsg $"Unable to evaluate query parameter expression:\n{exp}"

    /// Handles extended properties on Nullable and Option types.
    [<RequireQualifiedAccess>]
    type ExtProperty = 
        | IsSome
        | IsNone
        | HasValue
        | Value
        | NA

    /// A property member with extended property info for Nullable and Option types.
    let (|Property|_|) (exp: Expression) =
        match exp with
        | Member m when 
            m.Member.DeclaringType <> null && 
            m.Member.DeclaringType |> isOptionOrNullableType && 
            (m.Member.Name = "Value" || m.Member.Name = "HasValue" || m.Member.Name = "IsSome" || m.Member.Name = "IsNone") -> 

            let ext = 
                match m.Member.Name with
                | "Value" -> ExtProperty.Value
                | "IsSome" -> ExtProperty.IsSome
                | "IsNone" -> ExtProperty.IsNone
                | "HasValue" -> ExtProperty.HasValue
                | _ -> ExtProperty.NA

            tryGetMember m.Expression
            |> Option.map (fun pm -> pm, ext)
        | _ -> 
            tryGetMember exp
            |> Option.map (fun pm -> pm, ExtProperty.NA)

    /// A property/column in a record/table mapped to this query via a `for` or `join` clause.
    let (|MappedColumn|_|) (tables: TableMapping seq) (exp: Expression) = 
        match exp with
        | Property (p, ext) when tables |> Seq.exists (fun tbl -> tbl.IsInTable p) ->
            Some (p, ext)
        | _ -> 
            None

    /// A constant value or an expression that can be evaluated to a constant value.
    let (|Value|_|) (exp: Expression) =
        match exp with
        | Constant c -> Some c.Value
        // Do not try to evaluate QueryFunctions like `isIn`, `isNotIn`, etc.
        | Call c when not (isSqlHydraFunction c.Method) ->
            compileAndEvaluateExpression exp |> Some
        | _ -> None

    let (|AggregateColumn|_|) (exp: Expression) =
        match exp with
        | MethodCall m when aggregateMethodNames.Contains m.Method.Name ->
            let aggType = aggTypeOf m.Method.Name
            match m.Arguments.[0] with
            | Property p -> Some (aggType, p)
            // Aggregate over an arbitrary expression (e.g. sumBy(caseWhen ...)): not a column,
            // so don't match — the expression falls through to full expression rendering.
            | _ -> None
        | _ -> None

// ─── NormalizedExpression Patterns ───────────────────────────────────────────
// Active patterns on NormalizedExpression that delegate to existing Expression
// patterns for semantic checks. No semantic logic is duplicated.

open ExpressionNormalizer

[<AutoOpen>]
module NormalizedPatterns =

    /// Extracts alias by following NMemberAccess chain to NParameter.
    let rec nVisitAlias (nexp: NormalizedExpression) : string =
        match nexp with
        | NMemberAccess(inner, _) -> nVisitAlias inner
        | NParameter p -> p.Name
        | _ -> notImpl()

    /// Binary AND (And or AndAlso).
    let (|NBinaryAnd|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NBinary(left, op, right) when op = ExpressionType.And || op = ExpressionType.AndAlso -> Some (left, right)
        | _ -> None

    /// Binary OR (Or or OrElse).
    let (|NBinaryOr|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NBinary(left, op, right) when op = ExpressionType.Or || op = ExpressionType.OrElse -> Some (left, right)
        | _ -> None

    /// Binary comparison (=, <>, >, >=, <, <=). Returns (left, op, right).
    let (|NBinaryCompare|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NBinary(left, op, right) ->
            match op with
            | ExpressionType.Equal | ExpressionType.NotEqual
            | ExpressionType.GreaterThan | ExpressionType.GreaterThanOrEqual
            | ExpressionType.LessThan | ExpressionType.LessThanOrEqual -> Some (left, op, right)
            | _ -> None
        | _ -> None

    /// Not / negation.
    let (|NNot|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NUnary(ExpressionType.Not, operand) -> Some operand
        | _ -> None

    /// Bool member access.
    let (|NBoolMember|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NMemberAccess(_, m) when m.Type = typeof<bool> -> Some m
        | _ -> None

    /// Bool constant.
    let (|NBoolConstant|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NConstant(v, t) when t = typeof<bool> -> Some (v :?> bool)
        | _ -> None

    /// Property with extended info (Option/Nullable awareness).
    /// Delegates to the existing Property active pattern on the original Expression.
    let (|NProperty|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NMemberAccess(_, m) ->
            match (m :> Expression) with
            | Property (p, ext) -> Some (p, ext)
            | _ -> None
        | NMethodCall(call, _) ->
            // Handle Option.Some/Nullable wrapping (e.g., Some c.ProductCategoryID)
            match (call :> Expression) with
            | Property (p, ext) -> Some (p, ext)
            | _ -> None
        | NUnary(ExpressionType.Convert, NMemberAccess(_, m)) ->
            // Handle implicit conversions wrapping a property
            match (m :> Expression) with
            | Property (p, ext) -> Some (p, ext)
            | _ -> None
        | _ -> None

    /// A constant value or an evaluable expression.
    /// Delegates to compileAndEvaluateExpression for non-constant evaluable expressions.
    let (|NValue|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NConstant(v, _) -> Some v
        | NMethodCall(call, _) when not (isSqlHydraFunction call.Method) ->
            compileAndEvaluateExpression (call :> Expression) |> Some
        | NMemberAccess(NConstant _, m) ->
            // Evaluable member access on a constant (e.g., captured variable from closure)
            compileAndEvaluateExpression (m :> Expression) |> Some
        | NUnary(ExpressionType.Convert, NConstant(v, t)) when t.IsPrimitive ->
            // Handle implicit conversions (e.g., int to int64)
            Some v
        | NUnknown exp when exp <> null ->
            try compileAndEvaluateExpression exp |> Some
            with _ -> None
        | _ -> None

    /// Aggregate column pattern (minBy, maxBy, sumBy, avgBy, countBy, avgByAs).
    /// Matches only when the aggregate's argument resolves to a column (direct Property or
    /// Option/Nullable Value chain). Aggregates over arbitrary expressions (e.g.
    /// `countBy(caseWhen ...)`) fall through to the NMethodCall arm, which delegates to
    /// `visitSqlFn`/`renderExpr` for full expression rendering.
    let (|NAggregateColumn|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NMethodCall(m, _) when aggregateMethodNames.Contains m.Method.Name ->
            let aggType = aggTypeOf m.Method.Name
            match m.Arguments.[0] with
            | Property p -> Some (aggType, p)
            | _ -> None
        | _ -> None

    /// List initializer — delegates to original ListInit pattern.
    let (|NListInit|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NMethodCall(call, _) when call.Method.Name = "Cons" ->
            match (call :> Expression) with
            | ListInit values -> Some values
            | _ -> None
        | _ -> None

    /// Array initializer — delegates to original ArrayInit pattern.
    let (|NArrayInit|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NUnknown exp ->
            match exp with
            | ArrayInit values -> Some values
            | _ -> None
        | _ -> None

let tryGetComparison (expType: ExpressionType) =
    match expType with
    | ExpressionType.Equal -> Some "="
    | ExpressionType.NotEqual -> Some "<>"
    | ExpressionType.GreaterThan -> Some ">"
    | ExpressionType.GreaterThanOrEqual -> Some ">="
    | ExpressionType.LessThan -> Some "<"
    | ExpressionType.LessThanOrEqual -> Some "<="
    | _ -> None

/// Maps a binary expression node to its SQL operator: comparisons (`=`,`<>`,…) plus
/// logical/arithmetic operators (`AND`/`OR`/`+`/`-`/`*`/`/`/`%`). `None` if unsupported.
let tryGetBinaryOp (expType: ExpressionType) =
    match tryGetComparison expType with
    | Some s -> Some s
    | None ->
        match expType with
        | ExpressionType.AndAlso -> Some "AND"
        | ExpressionType.OrElse -> Some "OR"
        | ExpressionType.Add -> Some "+"
        | ExpressionType.Subtract -> Some "-"
        | ExpressionType.Multiply -> Some "*"
        | ExpressionType.Divide -> Some "/"
        | ExpressionType.Modulo -> Some "%"
        | _ -> None

let getComparison (expType: ExpressionType) =
    match tryGetComparison expType with
    | Some s -> s
    | None -> notImplMsg "Unsupported comparison type"

let reverseComparison (expType: ExpressionType) =
    match expType with
    | ExpressionType.GreaterThan -> ExpressionType.LessThan
    | ExpressionType.GreaterThanOrEqual -> ExpressionType.LessThanOrEqual
    | ExpressionType.LessThan -> ExpressionType.GreaterThan
    | ExpressionType.LessThanOrEqual -> ExpressionType.GreaterThanOrEqual
    | _ -> expType


let getReverseComparison = getComparison << reverseComparison

let toComparisonOp (expType: ExpressionType) =
    match expType with
    | ExpressionType.Equal -> Eq
    | ExpressionType.NotEqual -> NotEq
    | ExpressionType.GreaterThan -> Gt
    | ExpressionType.GreaterThanOrEqual -> GtEq
    | ExpressionType.LessThan -> Lt
    | ExpressionType.LessThanOrEqual -> LtEq
    | _ -> notImplMsg "Unsupported comparison type"

let reverseComparisonOp (op: ComparisonOp) =
    match op with
    | Gt -> Lt
    | GtEq -> LtEq
    | Lt -> Gt
    | LtEq -> GtEq
    | op -> op
    
let visitAlias (exp: Expression) =
    let rec visit (exp: Expression) =
        match exp with
        | Member m -> visit m.Expression
        | Parameter p -> p.Name
        | _ -> notImpl()
    visit exp

let private compileAndEval (e: Expression) =
    System.Linq.Expressions.Expression.Lambda(e).Compile().DynamicInvoke()

let private inv = System.Globalization.CultureInfo.InvariantCulture

let private isNullaryDU (t: System.Type) =
    FSharp.Reflection.FSharpType.IsUnion(t)
    && FSharp.Reflection.FSharpType.GetUnionCases(t) |> Array.forall (fun c -> c.GetFields().Length = 0)

let private formatFloat (s: string) =
    if s.Contains(".") || s.Contains("e") || s.Contains("E") then s else s + ".0"

/// Formats a numeric constant as SQL literal, preserving the type's decimal form for floats.
/// `1.0` (double) → "1.0", not "1" (which Postgres types as integer and breaks `1.0 - <vector>` queries).
/// Enums and nullary DUs are quoted as their string name — Postgres treats bare identifiers as columns.
let private formatNumericLiteral (value: obj) (clrType: System.Type) =
    match clrType with
    | t when t = typeof<float> || t = typeof<double> -> (value :?> double).ToString("R", inv) |> formatFloat
    | t when t = typeof<single> || t = typeof<float32> -> (value :?> single).ToString("R", inv) |> formatFloat
    | t when t = typeof<decimal> -> (value :?> decimal).ToString(inv)
    | t when t.IsEnum || isNullaryDU t -> $"'{value}'"
    // Integer primitives (int, int64, byte, ...) — ToString is safe SQL for these.
    // char/bool are excluded: bool is handled in renderObjAsLiteral; char would emit unquoted.
    | t when t.IsPrimitive && t <> typeof<char> && t <> typeof<bool> -> sprintf "%O" value
    | t -> failwithf "Cannot format SQL literal of type %s — wrap with inlineValue or parameterize." t.FullName

/// Renders a `ConstantExpression` to a SQL literal. When `handleBool` is true, bool constants
/// emit `TRUE`/`FALSE`; otherwise they fall through to numeric formatting (matching the
/// orderBy walker, which never receives bool constants). Shared by the expression walkers.
let private renderConstant (handleBool: bool) (c: ConstantExpression) =
    if c.Value = null then "NULL"
    elif c.Type = typeof<string> then $"'{c.Value}'"
    elif handleBool && c.Type = typeof<bool> then (if c.Value :?> bool then "TRUE" else "FALSE")
    else formatNumericLiteral c.Value c.Type

/// Renders an evaluated runtime value as a SQL literal fragment.
/// Used for `inlineValue` and static-field references (Guid.Empty, DateTime.MinValue, etc.).
let private renderObjAsLiteral (v: obj) =
    match v with
    | null -> "NULL"
    | :? string as s -> $"'{s}'"
    | :? bool as b -> if b then "TRUE" else "FALSE"
    | :? System.Guid as g -> $"'{g}'"
    | :? System.DateTime as dt ->
        let s = dt.ToString("yyyy-MM-dd HH:mm:ss", inv)
        $"'{s}'"
    | :? System.DateTimeOffset as dto ->
        let s = dto.ToString("yyyy-MM-dd HH:mm:sszzz", inv)
        $"'{s}'"
    | _ -> formatNumericLiteral v (v.GetType())

/// Converts a SQL function MethodCall expression to a SQL fragment string.
/// Also renders argument expressions when called recursively as the entry point for
/// general select-fragment compilation (caseWhen, castAs, etc.).
/// Example: LEN(p.FirstName) -> "LEN({p}.{FirstName})"
let rec visitSqlFn (qualifyColumn: string -> MemberInfo -> string) (exp: Expression) : string =
    /// Render an arbitrary expression as a SQL fragment (literals inline, columns qualified, nested fns recursed).
    /// Supports: Member columns, static-field Members, Constants, Unary Convert, Binary arithmetic/compare, MethodCall.
    /// `inlineValue x` is rendered as the value's literal form (numeric/string).
    let rec renderExpr (arg: Expression) : string =
        match arg with
        // Unwrap implicit numeric conversions (e.g., int → float when a column type widens).
        | :? UnaryExpression as u when u.NodeType = ExpressionType.Convert ->
            renderExpr u.Operand
        // inlineValue marker: compile-and-eval the inner expression and emit as a literal.
        | MethodCall m when m.Method.Name = nameof inlineValue && m.Arguments.Count = 1 ->
            renderObjAsLiteral (compileAndEval m.Arguments.[0])
        | Member mem when mem.Expression <> null ->
            let alias = visitAlias mem.Expression
            qualifyColumn alias mem.Member
        // Static fields (Guid.Empty, DateTime.MinValue, String.Empty) — null .Expression.
        | Member mem -> renderObjAsLiteral (compileAndEval mem)
        | :? ConstantExpression as c -> renderConstant true c
        | :? BinaryExpression as b ->
            let left = renderExpr b.Left
            let right = renderExpr b.Right
            let op =
                match tryGetBinaryOp b.NodeType with
                | Some s -> s
                | None -> notImplMsg $"Unsupported binary operator in expression: {b.NodeType}"
            $"{left} {op} {right}"
        | MethodCall _ as nested -> visitSqlFn qualifyColumn nested
        | _ -> notImplMsg $"Unsupported expression in select/caseWhen fragment: {arg.NodeType}"

    /// Extract (cond, value) pairs from an F# list literal like `[ a > 1, "x"; b > 2, "y" ]`.
    let rec extractListItems (exp: Expression) : (string * string) list =
        match exp with
        | :? MethodCallExpression as m when m.Method.Name = "Cons" || m.Method.Name = "op_ColonColon" ->
            let (cond, value) = extractTuple m.Arguments.[0]
            (cond, value) :: extractListItems m.Arguments.[1]
        | :? NewExpression as n when n.Arguments.Count = 2 ->
            let (cond, value) = extractTuple n.Arguments.[0]
            (cond, value) :: extractListItems n.Arguments.[1]
        | :? MemberExpression as m when m.Member.Name = "Empty" -> []
        | :? DefaultExpression -> []
        | _ ->
            // Fallback: compile-and-eval to runtime list.
            try
                match compileAndEval exp with
                | :? System.Collections.IEnumerable as items ->
                    [ for item in items do
                        let t = item.GetType()
                        let cond = t.GetProperty("Item1").GetValue(item) :?> bool
                        let v = t.GetProperty("Item2").GetValue(item)
                        let condStr = if cond then "TRUE" else "FALSE"
                        let valStr =
                            match v with
                            | null -> "NULL"
                            | :? string as s -> $"'{s}'"
                            | x -> sprintf "%O" x
                        yield (condStr, valStr) ]
                | _ -> notImplMsg $"Cannot extract caseWhenMulti list: {exp.NodeType}"
            with ex -> notImplMsg $"Cannot extract caseWhenMulti list: {exp.NodeType} ({ex.Message})"
    and extractTuple (exp: Expression) : string * string =
        match exp with
        | :? NewExpression as n when n.Arguments.Count = 2 ->
            (renderExpr n.Arguments.[0], renderExpr n.Arguments.[1])
        | :? MethodCallExpression as m when m.Method.Name = "NewTuple" ->
            (renderExpr m.Arguments.[0], renderExpr m.Arguments.[1])
        | _ -> notImplMsg $"Cannot extract caseWhenMulti tuple: {exp.NodeType}"

    match exp with
    // CAST(expr AS sqlType) — target SQL type inferred from the F# return type.
    | MethodCall m when m.Method.Name = nameof castAs && m.Arguments.Count = 1 ->
        $"CAST({renderExpr m.Arguments.[0]} AS {sqlTypeForClrType m.Method.ReturnType})"
    // CASE WHEN cond THEN then ELSE else END
    | MethodCall m when m.Method.Name = nameof caseWhen && m.Arguments.Count = 3 ->
        $"CASE WHEN {renderExpr m.Arguments.[0]} THEN {renderExpr m.Arguments.[1]} ELSE {renderExpr m.Arguments.[2]} END"
    // Multi-branch CASE WHEN
    | MethodCall m when m.Method.Name = nameof caseWhenMulti && m.Arguments.Count = 2 ->
        let whens =
            extractListItems m.Arguments.[0]
            |> List.map (fun (c, v) -> $"WHEN {c} THEN {v}")
            |> String.concat " "
        $"CASE {whens} ELSE {renderExpr m.Arguments.[1]} END"
    // Lateral subquery column reference: lateralCol "alias" "col" → "alias"."col"
    | MethodCall m when m.Method.Name = nameof lateralCol && m.Arguments.Count = 2 ->
        let alias = compileAndEval m.Arguments.[0] :?> string
        let column = compileAndEval m.Arguments.[1] :?> string
        $"\"{alias}\".\"{column}\""
    // Raw SQL escape hatch
    | MethodCall m when m.Method.Name = nameof rawExpr && m.Arguments.Count = 1 ->
        compileAndEval m.Arguments.[0] :?> string
    // PostgreSQL INTERVAL literal: interval "7 days" → INTERVAL '7 days'
    // (Method name string-matched because `interval` lives in NpgsqlExtensions and isn't in scope here.)
    | MethodCall m when m.Method.Name = "interval" && m.Arguments.Count = 1 ->
        let value = compileAndEval m.Arguments.[0] :?> string
        $"INTERVAL '{value}'"
    // Aggregates → render via renderAggregate (handles COUNT(DISTINCT col)).
    | MethodCall m when aggregateMethodNames.Contains m.Method.Name ->
        let aggType = aggTypeOf m.Method.Name
        match m.Arguments.[0] with
        | Member mem when mem.Expression <> null ->
            let alias = visitAlias mem.Expression
            renderAggregate aggType (qualifyColumn alias mem.Member)
        | inner ->
            // Nested expression inside aggregate (e.g. SUM(CAST(...)) or MAX(SUM(...)))
            renderAggregate aggType (renderExpr inner)
    | MethodCall m ->
        // Infix-operator seam: a 2-arg function registered via SqlHydraInfixOperatorAttribute
        // (e.g. cosine_distance → <=>) is emitted infix: (lhs <=> rhs).
        match (if m.Arguments.Count = 2 then InfixOperators.tryGetOperator m.Method.Name else None) with
        | Some op ->
            $"({renderExpr m.Arguments.[0]} {op} {renderExpr m.Arguments.[1]})"
        | None ->
            let args = m.Arguments |> Seq.map renderExpr |> String.concat ", "
            $"{m.Method.Name.ToUpperInvariant()}({args})"
    | _ ->
        notImplMsg $"Expected a method call expression but got: {exp.NodeType}"

/// Delegates to existing visitSqlFn by extracting the original MethodCallExpression.
let nVisitSqlFn (qualifyColumn: string -> MemberInfo -> string) (nexp: NormalizedExpression) : string =
    match nexp with
    | NMethodCall(m, _) -> visitSqlFn qualifyColumn (m :> Expression)
    | _ -> notImplMsg $"Expected NMethodCall for SQL function"

let visitWhere<'T> (tables: TableMapping seq) (filter: Expression<Func<'T, bool>>) (qualifyColumn: string -> MemberInfo -> string) : WhereClause =
    let (|NColumn|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NProperty (p, ext) when tables |> Seq.exists (fun tbl -> tbl.IsInTable p) -> Some (p, ext)
        | _ -> None

    /// Evaluate a NormalizedExpression to a runtime value.
    let nEvaluate (nexp: NormalizedExpression) =
        match nexp with
        | NValue v -> v
        | NMemberAccess(_, m) -> compileAndEvaluateExpression (m :> Expression)
        | NMethodCall(m, _) -> compileAndEvaluateExpression (m :> Expression)
        | NUnknown exp -> compileAndEvaluateExpression exp
        | _ -> notImplMsg $"Unable to evaluate expression: {nexp}"

    let rec visit (nexp: NormalizedExpression) : WhereClause =
        match nexp with
        // Idiomatic F#: `set.Contains col`, `list.Contains col`, `array.Contains col`,
        // and `Seq.contains col xs` / `List.contains col xs`. Compile to `col IN (values)`.
        // The collection is compile-and-eval'd (it must be a closed-over value, not a column).
        | NMethodCall(m, args) when m.Method.Name = "Contains" ->
            let receiverExp, columnNExp =
                if m.Object <> null && args.Length = 1 then
                    // Instance method: receiver.Contains(col)
                    m.Object, args.[0]
                elif args.Length = 2 then
                    // Static: Module.contains col xs OR xs.Contains col-via-extension
                    m.Arguments.[0], args.[1]
                else
                    notImplMsg $"Unsupported Contains shape: {nexp}"
            match columnNExp with
            | NColumn (p, _) ->
                let receiver = compileAndEvaluateExpression receiverExp
                let queryParameters =
                    (receiver :?> System.Collections.IEnumerable)
                    |> Seq.cast<obj>
                    |> Seq.map (QueryUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                InValues(fqCol, queryParameters)
            | _ -> notImplMsg $"Unsupported Contains column argument: {columnNExp}"
        | NMethodCall(m, args) when List.contains m.Method.Name [ nameof isIn; nameof isNotIn; nameof op_BarEqualsBar; nameof op_BarLessGreaterBar ] ->
            let isIn = List.contains m.Method.Name [ nameof isIn; nameof op_BarEqualsBar ]

            match args.[0], args.[1] with
            | NColumn (p, _), NMethodCall(subqueryExpr, _) when subqueryExpr.Method.Name = nameof subqueryMany ->
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                if isIn then InSubQuery(fqCol, selectSubquery.SelectIR)
                else NotInSubQuery(fqCol, selectSubquery.SelectIR)
            | NColumn (p, _), NListInit values ->
                let queryParameters =
                    values
                    |> Seq.map (QueryUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if isIn then InValues(fqCol, queryParameters)
                else NotInValues(fqCol, queryParameters)
            | NColumn (p, _), NArrayInit values ->
                let queryParameters =
                    values
                    |> Seq.map (QueryUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if isIn then InValues(fqCol, queryParameters)
                else NotInValues(fqCol, queryParameters)
            | NColumn (p, _), NValue value ->
                let queryParameters =
                    (value :?> System.Collections.IEnumerable)
                    |> Seq.cast<obj>
                    |> Seq.map (QueryUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if isIn then InValues(fqCol, queryParameters)
                else NotInValues(fqCol, queryParameters)
            | NColumn _, NMethodCall(c, _) when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()

        // like / notLike fns
        | NMethodCall(m, args) when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match args.[0], args.[1] with
            | NColumn (p, _), NValue value ->
                let pattern = QueryUtils.getQueryParameterForValue p.Member value
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> Like(fqCol, pattern)
                | _ -> NotLike(fqCol, pattern)
            | _ -> notImpl()

        // isNull / isNotNull
        | NMethodCall(m, args) when List.contains m.Method.Name [ nameof isNullValue; "IsNull"; nameof isNotNullValue ] ->
            match args.[0] with
            | NColumn (p, _) ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if m.Method.Name = nameof isNullValue || m.Method.Name = "IsNull"
                then IsNull(fqCol)
                else IsNotNull(fqCol)
            | _ -> notImpl()

        // areEqual / notEqual
        | NMethodCall(m, args) when List.contains m.Method.Name [ nameof areEqual; nameof notEqual ] ->
            match args.[0], args.[1] with
            | NColumn (p1, _), NColumn (p2, _) ->
                let alias1 = visitAlias p1.Expression
                let fqCol1 = qualifyColumn alias1 p1.Member
                let alias2 = visitAlias p2.Expression
                let fqCol2 = qualifyColumn alias2 p2.Member
                let compOp = if m.Method.Name = nameof areEqual then Eq else NotEq
                CompareColumns(fqCol1, compOp, fqCol2)
            | NColumn (p, _), NValue value | NValue value, NColumn (p, _) ->
                let alias1 = visitAlias p.Expression
                let fqCol1 = qualifyColumn alias1 p.Member
                let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                let compOp = if m.Method.Name = nameof areEqual then Eq else NotEq
                Compare(fqCol1, compOp, Parameter queryParameter)
            | _ -> notImpl()

        // Nullable / Option .HasValue / .IsSome
        | NMemberAccess(_, bm) & NColumn (p, ext) when
            bm.Type = typeof<bool>
            && p.Type |> isOptionOrNullableType
            && (ext = ExtProperty.HasValue || ext = ExtProperty.IsSome) ->
            let alias = visitAlias p.Expression
            let m = tryGetMember p
            let fqCol = qualifyColumn alias m.Value.Member
            IsNotNull(fqCol)

        | NNot (NMemberAccess(_, bm) & NColumn (p, ext)) when
            bm.Type = typeof<bool>
            && p.Type |> isOptionOrNullableType
            && (ext = ExtProperty.HasValue || ext = ExtProperty.IsSome) ->
            let alias = visitAlias p.Expression
            let m = tryGetMember p
            let fqCol = qualifyColumn alias m.Value.Member
            IsNull(fqCol)

        // Option.IsNone
        | NMemberAccess(_, bm) & NColumn (p, ext) when
            bm.Type = typeof<bool>
            && p.Type |> isOptionType
            && ext = ExtProperty.IsNone ->
            let alias = visitAlias p.Expression
            let m = tryGetMember p
            let fqCol = qualifyColumn alias m.Value.Member
            IsNull(fqCol)

        | NNot (NMemberAccess(_, bm) & NColumn (p, ext)) when
            bm.Type = typeof<bool>
            && p.Type |> isOptionType
            && ext = ExtProperty.IsNone ->
            let alias = visitAlias p.Expression
            let m = tryGetMember p
            let fqCol = qualifyColumn alias m.Value.Member
            IsNotNull(fqCol)

        // Bool column `where user.IsEnabled`
        | NMemberAccess(_, bm) & NColumn (p, _) when bm.Type = typeof<bool> ->
            let alias = visitAlias p.Expression
            let fqCol = qualifyColumn alias p.Member
            BoolColumn(fqCol, true)

        | NNot (NMemberAccess(_, bm) & NColumn (p, _)) when bm.Type = typeof<bool> ->
            let alias = visitAlias p.Expression
            let fqCol = qualifyColumn alias p.Member
            BoolColumn(fqCol, false)

        | NNot operand ->
            let clause = visit operand
            WhereClause.Not(clause)

        | NBinaryAnd(left, right) ->
            match left with
            | NValue enabled ->
                if enabled :?> bool
                then visit right
                else Empty
            | _ ->
                let lt = visit left
                let rt = visit right
                WhereClause.combineAnd lt rt

        | NBinaryOr(left, right) ->
            match left with
            | NValue enabled ->
                if enabled :?> bool
                then visit right
                else Empty
            | _ ->
                let lt = visit left
                let rt = visit right
                WhereClause.combineOr lt rt

        | NBinaryCompare(left, op, right) ->
            let compOp = toComparisonOp op
            let comparison = getComparison op
            match left, right with

            // Property to subquery
            | NColumn (p1, _), NMethodCall(subqueryExpr, _) when subqueryExpr.Method.Name = nameof subqueryOne ->
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                let alias = visitAlias p1.Expression
                let fqCol = qualifyColumn alias p1.Member
                Compare(fqCol, compOp, SubQuery selectSubquery.SelectIR)

            // Col to col
            | NColumn (p1, _), NColumn (p2, _) ->
                let lt =
                    let alias = visitAlias p1.Expression
                    qualifyColumn alias p1.Member
                let rt =
                    let alias = visitAlias p2.Expression
                    qualifyColumn alias p2.Member
                CompareColumns(lt, compOp, rt)

            // Column = null
            | NColumn (p, _), NConstant(null, _) | NConstant(null, _), NColumn (p, _) when op = ExpressionType.Equal ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                IsNull(fqCol)

            // Column <> null
            | NColumn (p, _), NConstant(null, _) | NConstant(null, _), NColumn (p, _) when op = ExpressionType.NotEqual ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                IsNotNull(fqCol)

            // Option.IsSome / Nullable.HasValue null check (Equal)
            | NColumn (p, ext), NBoolConstant value | NBoolConstant value, NColumn (p, ext) when
                p.Type |> isOptionOrNullableType
                && (ext = ExtProperty.HasValue || ext = ExtProperty.IsSome)
                && op = ExpressionType.Equal ->
                let alias = visitAlias p.Expression
                let m = tryGetMember p
                let fqCol = qualifyColumn alias m.Value.Member
                match value with
                | true -> IsNotNull(fqCol)
                | false -> IsNull(fqCol)

            // Option.IsSome / Nullable.HasValue null check (NotEqual)
            | NColumn (p, ext), NBoolConstant value | NBoolConstant value, NColumn (p, ext) when
                p.Type |> isOptionOrNullableType
                && (ext = ExtProperty.HasValue || ext = ExtProperty.IsSome)
                && op = ExpressionType.NotEqual ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                match value with
                | true -> IsNull(fqCol)
                | false -> IsNotNull(fqCol)

            // Nullable.Value comparisons
            | NColumn (p, ext), NValue value | NValue value, NColumn (p, ext) when
                p.Type |> isOptionOrNullableType
                && ext = ExtProperty.Value ->
                let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                let alias = visitAlias p.Expression
                let m = tryGetMember p
                let fqCol = qualifyColumn alias m.Value.Member
                Compare(fqCol, compOp, Parameter queryParameter)

            | NColumn (p, _), _ ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                // RHS may be a captured value (compile-and-eval) OR an outer-scope column
                // reference in a lateral subquery (where the column's parameter isn't in
                // the local `tables` map). Try eval; on failure, fall back to column ref.
                let valueResult =
                    try Some (nEvaluate right) with _ -> None
                match valueResult with
                | Some value ->
                    match value with
                    | null when op = ExpressionType.Equal -> IsNull(fqCol)
                    | null when op = ExpressionType.NotEqual -> IsNotNull(fqCol)
                    | _ ->
                        let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                        Compare(fqCol, compOp, Parameter queryParameter)
                | None ->
                    match right with
                    | NMemberAccess(_, m) when m.Expression <> null ->
                        let rhsAlias = visitAlias m.Expression
                        let rhsCol = qualifyColumn rhsAlias m.Member
                        CompareColumns(fqCol, compOp, rhsCol)
                    | _ -> notImplMsg $"Unable to evaluate where RHS: {right}"

            | _, NColumn (p, _) ->
                let valueResult =
                    try Some (nEvaluate left) with _ -> None
                let reversedOp = reverseComparisonOp compOp
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                match valueResult with
                | None ->
                    match left with
                    | NMemberAccess(_, m) when m.Expression <> null ->
                        let lhsAlias = visitAlias m.Expression
                        let lhsCol = qualifyColumn lhsAlias m.Member
                        CompareColumns(lhsCol, compOp, fqCol)
                    | _ -> notImplMsg $"Unable to evaluate where LHS: {left}"
                | Some value ->
                match value with
                | null when reversedOp = Eq -> IsNull(fqCol)
                | null when reversedOp = NotEq -> IsNotNull(fqCol)
                | _ ->
                    let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                    Compare(fqCol, reversedOp, Parameter queryParameter)

            // SQL function compared to value
            | NMethodCall _, NValue value ->
                let sqlFragment = nVisitSqlFn qualifyColumn left
                RawWhere($"{sqlFragment} {comparison} ?", [| value |])

            // Value compared to SQL function
            | NValue value, NMethodCall _ ->
                let sqlFragment = nVisitSqlFn qualifyColumn right
                let reversedComparison = getReverseComparison op
                RawWhere($"{sqlFragment} {reversedComparison} ?", [| value |])

            // SQL function compared to column
            | NMethodCall _, NColumn (p, _) ->
                let sqlFragment = nVisitSqlFn qualifyColumn left
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                RawWhere($"{sqlFragment} {comparison} {fqCol}", [||])

            // Column compared to SQL function
            | NColumn (p, _), NMethodCall _ ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                let sqlFragment = nVisitSqlFn qualifyColumn right
                RawWhere($"{fqCol} {comparison} {sqlFragment}", [||])

            // SQL function compared to SQL function
            | NMethodCall _, NMethodCall _ ->
                let sqlFragment1 = nVisitSqlFn qualifyColumn left
                let sqlFragment2 = nVisitSqlFn qualifyColumn right
                RawWhere($"{sqlFragment1} {comparison} {sqlFragment2}", [||])

            // Joined table parameter compared to None (e.g., where (d = None) after leftJoin')
            | NParameter p, _ | _, NParameter p when p.Type |> isOptionType ->
                let innerType = p.Type.GetGenericArguments().[0]
                let firstField = FSharp.Reflection.FSharpType.GetRecordFields(innerType).[0]
                let fqCol = qualifyColumn p.Name firstField
                match op with
                | ExpressionType.Equal -> IsNull(fqCol)
                | ExpressionType.NotEqual -> IsNotNull(fqCol)
                | _ -> notImplMsg $"Unsupported comparison for joined table parameter: {op}"

            | NValue _, NValue _ ->
                notImplMsg("Value to value comparisons are not currently supported. Ex: where (1 = 1)")

            | _ ->
                // Fallback: outer-scope column refs (lateral subquery referencing
                // a correlate-d parent table). The parameter isn't in the local
                // `tables` map so NColumn doesn't match, but visitAlias still
                // returns the parameter name as alias.
                let rec asOuterCol (n: NormalizedExpression) =
                    match n with
                    | NMemberAccess(_, m) when (m.Expression :? ParameterExpression) -> Some m
                    | NUnary(ExpressionType.Convert, inner) -> asOuterCol inner
                    | _ -> None
                let tryEval (n: NormalizedExpression) =
                    try Some (nEvaluate n) with _ -> None
                match asOuterCol left, asOuterCol right with
                | Some ml, Some mr ->
                    let lt = qualifyColumn (visitAlias ml.Expression) ml.Member
                    let rt = qualifyColumn (visitAlias mr.Expression) mr.Member
                    CompareColumns(lt, compOp, rt)
                | Some ml, None ->
                    let fq = qualifyColumn (visitAlias ml.Expression) ml.Member
                    match tryEval right with
                    | Some null when op = ExpressionType.Equal -> IsNull(fq)
                    | Some null when op = ExpressionType.NotEqual -> IsNotNull(fq)
                    | Some v ->
                        let qp = QueryUtils.getQueryParameterForValue ml.Member v
                        Compare(fq, compOp, Parameter qp)
                    | None -> notImplMsg $"[where-cmp] cannot eval RHS: {right}"
                | None, Some mr ->
                    let fq = qualifyColumn (visitAlias mr.Expression) mr.Member
                    let rev = reverseComparisonOp compOp
                    match tryEval left with
                    | Some null when op = ExpressionType.Equal -> IsNull(fq)
                    | Some null when op = ExpressionType.NotEqual -> IsNotNull(fq)
                    | Some v ->
                        let qp = QueryUtils.getQueryParameterForValue mr.Member v
                        Compare(fq, rev, Parameter qp)
                    | None -> notImplMsg $"[where-cmp] cannot eval LHS: {left}"
                | None, None ->
                    notImplMsg $"[where-cmp-fallthrough] op={compOp}\nleft={left}\nright={right}"

        | _ ->
            notImplMsg $"Unsupported expression type in where clause: {nexp}"

    visit (ExpressionNormalizer.toNormalizedExpression (filter :> Expression))

let visitHaving<'T> (tables: TableMapping seq) (filter: Expression<Func<'T, bool>>) (qualifyColumn: string -> MemberInfo -> string) : WhereClause =
    let (|NColumn|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NProperty (p, ext) when tables |> Seq.exists (fun tbl -> tbl.IsInTable p) -> Some (p, ext)
        | _ -> None

    let rec visit (nexp: NormalizedExpression) : WhereClause =
        match nexp with
        | NNot operand ->
            let clause = visit operand
            WhereClause.Not(clause)
        | NMethodCall(m, args) when List.contains m.Method.Name [ nameof isIn; nameof isNotIn; nameof op_BarEqualsBar; nameof op_BarLessGreaterBar ] ->
            let isIn = List.contains m.Method.Name [ nameof isIn; nameof op_BarEqualsBar ]

            match args.[0], args.[1] with
            | NColumn (p, _), NMethodCall(subqueryExpr, _) when subqueryExpr.Method.Name = nameof subqueryMany ->
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                if isIn then InSubQuery(fqCol, selectSubquery.SelectIR)
                else NotInSubQuery(fqCol, selectSubquery.SelectIR)
            | NColumn (p, _), NListInit values ->
                let queryParameters =
                    values
                    |> Seq.map (QueryUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if isIn then InValues(fqCol, queryParameters)
                else NotInValues(fqCol, queryParameters)
            | NColumn (p, _), NArrayInit values ->
                let queryParameters =
                    values
                    |> Seq.map (QueryUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if isIn then InValues(fqCol, queryParameters)
                else NotInValues(fqCol, queryParameters)
            | NColumn (p, _), NValue value ->
                let queryParameters =
                    (value :?> System.Collections.IEnumerable)
                    |> Seq.cast<obj>
                    |> Seq.map (QueryUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if isIn then InValues(fqCol, queryParameters)
                else NotInValues(fqCol, queryParameters)
            | NColumn _, NMethodCall(c, _) when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()
        | NMethodCall(m, args) when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match args.[0], args.[1] with
            | NColumn (p, _), NValue value ->
                let pattern = QueryUtils.getQueryParameterForValue p.Member value
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> Like(fqCol, pattern)
                | _ -> NotLike(fqCol, pattern)
            | _ -> notImpl()
        | NMethodCall(m, args) when m.Method.Name = nameof isNullValue || m.Method.Name = nameof isNotNullValue ->
            match args.[0] with
            | NColumn (p, _) ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if m.Method.Name = nameof isNullValue
                then IsNull(fqCol)
                else IsNotNull(fqCol)
            | _ -> notImpl()
        | NMethodCall(m, args) when aggregateMethodNames.Contains m.Method.Name ->
            visit args.[0]
        | NBinaryAnd(left, right) ->
            let lt = visit left
            let rt = visit right
            WhereClause.combineAnd lt rt
        | NBinaryOr(left, right) ->
            let lt = visit left
            let rt = visit right
            WhereClause.combineOr lt rt
        | NBinaryCompare(left, op, right) ->
            let compOp = toComparisonOp op
            let comparison = getComparison op
            match left, right with
            | NColumn (p1, _), NMethodCall(subqueryExpr, _) when subqueryExpr.Method.Name = nameof subqueryOne ->
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                let alias = visitAlias p1.Expression
                let fqCol = qualifyColumn alias p1.Member
                Compare(fqCol, compOp, SubQuery selectSubquery.SelectIR)
            | NAggregateColumn (aggType, (p1, _)), NColumn (p2, _) ->
                let lt =
                    let alias = visitAlias p1.Expression
                    qualifyColumn alias p1.Member
                let rt =
                    let alias = visitAlias p2.Expression
                    qualifyColumn alias p2.Member
                RawWhere($"{renderAggregate aggType lt} {comparison} {rt}", [||])
            | NAggregateColumn (aggType, (p, _)), NValue value ->
                let alias = visitAlias p.Expression
                let lt = qualifyColumn alias p.Member
                RawWhere($"{renderAggregate aggType lt} {comparison} ?", [|value|])
            | NColumn (p1, _), NColumn (p2, _) ->
                let lt =
                    let alias = visitAlias p1.Expression
                    qualifyColumn alias p1.Member
                let rt =
                    let alias = visitAlias p2.Expression
                    qualifyColumn alias p2.Member
                CompareColumns(lt, compOp, rt)
            | NColumn (p, _), NValue value ->
                match op, value with
                | ExpressionType.Equal, null ->
                    let alias = visitAlias p.Expression
                    IsNull(qualifyColumn alias p.Member)
                | ExpressionType.NotEqual, null ->
                    let alias = visitAlias p.Expression
                    IsNotNull(qualifyColumn alias p.Member)
                | _ ->
                    let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                    let alias = visitAlias p.Expression
                    Compare(qualifyColumn alias p.Member, compOp, Parameter queryParameter)
            | NValue _, NValue _ ->
                notImplMsg("Value to value comparisons are not currently supported. Ex: having (1 = 1)")
            | _ ->
                notImpl()

        | _ ->
            notImplMsg $"Unsupported expression type in having clause: {nexp}"

    visit (ExpressionNormalizer.toNormalizedExpression (filter :> Expression))

/// Returns a list of one or more fully qualified column names: ["{schema}.{table}.{column}"]
let visitPropertiesSelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) (qualifyColumn: string -> MemberInfo -> string) =
    let rec visit (nexp: NormalizedExpression) : string list =
        match nexp with
        | NNew(_, args) ->
            args |> List.collect visit
        | NMemberAccess(inner, m) ->
            let alias = nVisitAlias inner
            let column = qualifyColumn alias m.Member
            [column]
        | _ -> notImpl()

    visit (ExpressionNormalizer.toNormalizedExpression (propertySelector :> Expression))

[<NoComparison>]
type OrderBy =
    | OrderByColumn of tableAlias: string * MemberInfo
    | OrderByAggregateColumn of aggregateType: string * tableAlias: string * MemberInfo
    /// `orderBy (cosine_distance(col, vec))` and similar method-call expressions.
    /// Carries the rendered SQL fragment with `?` placeholders bound to `parameters`.
    | OrderByExpression of fragment: string * parameters: obj[]
    | OrderByIgnored

/// Returns a column MemberInfo.
let visitOrderByPropertySelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (nexp: NormalizedExpression) : OrderBy =
        match nexp with
        | NMethodCall(m, args) when m.Method.Name = nameof op_HatHat ->
            // ^^ operator conditionally adds property to order by clause
            match args.[0], args.[1] with
            | NValue enabled, NProperty (p, _) ->
                if enabled :?> bool then
                    let alias = visitAlias p.Expression
                    OrderByColumn (alias, p.Member)
                else
                    OrderByIgnored
            | _ ->
                notImpl()
        | NAggregateColumn (aggType, (p, _)) ->
            let alias = visitAlias p.Expression
            OrderByAggregateColumn (aggType, alias, p.Member)
        // Method-call orderBy (e.g. orderBy (cosine_distance(col, inlineValue vec))) — render directly.
        // Walk the expression building SQL fragment + parameter list. inlineValue args become bound
        // parameters; columns are qualified; InfixOperators registrations rewrite to infix.
        | NMethodCall(m, _) ->
            let parms = ResizeArray<obj>()
            let qualifyColumn alias (mem: MemberInfo) = $"\"{alias}\".\"{mem.Name}\""
            let rec render (e: Expression) : string =
                match e with
                | :? UnaryExpression as u when u.NodeType = ExpressionType.Convert ->
                    render u.Operand
                | :? MethodCallExpression as mc when mc.Method.Name = nameof inlineValue && mc.Arguments.Count = 1 ->
                    let value = compileAndEval mc.Arguments.[0]
                    parms.Add(if isNull value then box System.DBNull.Value else value)
                    "?"
                | :? MemberExpression as mem when mem.Expression <> null ->
                    let alias = visitAlias mem.Expression
                    qualifyColumn alias mem.Member
                | :? ConstantExpression as c -> renderConstant false c
                | :? MethodCallExpression as mc when mc.Arguments.Count = 2 && (InfixOperators.tryGetOperator mc.Method.Name).IsSome ->
                    let op = (InfixOperators.tryGetOperator mc.Method.Name).Value
                    $"({render mc.Arguments.[0]} {op} {render mc.Arguments.[1]})"
                | :? MethodCallExpression as mc when aggregateMethodNames.Contains mc.Method.Name ->
                    let aggType = aggTypeOf mc.Method.Name
                    renderAggregate aggType (render mc.Arguments.[0])
                | :? MethodCallExpression as mc ->
                    let args = mc.Arguments |> Seq.map render |> String.concat ", "
                    $"{mc.Method.Name.ToUpperInvariant()}({args})"
                | _ ->
                    notImplMsg $"Unsupported expression in orderBy method-call: {e.NodeType}"
            let frag = render (m :> Expression)
            OrderByExpression (frag, parms.ToArray())
        | NMemberAccess(inner, m) ->
            if m.Member.DeclaringType |> isOptionOrNullableType then
                visit inner
            else
                let alias = visitAlias m.Expression
                OrderByColumn (alias, m.Member)
        | NProperty (p, _) ->
            let alias = visitAlias p.Expression
            OrderByColumn (alias, p.Member)
        | _ -> notImpl()

    visit (ExpressionNormalizer.toNormalizedExpression (propertySelector :> Expression))

[<NoComparison>]
type JoinedPropertyInfo =
    {
        Alias: string
        Member: MemberInfo
    }

/// Returns one or more column members
let visitJoin<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (nexp: NormalizedExpression) : JoinedPropertyInfo list =
        match nexp with
        | NNew(_, args) ->
            args |> List.collect visit
        | NMethodCall(m, args) when m.Method.Name = "Some" ->
            // Option.Some wrapping — visit the inner argument
            visit args.[0]
        | NMemberAccess(inner, m) ->
            if m.Member.DeclaringType |> isOptionOrNullableType
            then visit inner
            else
                let alias = visitAlias m.Expression
                [ { Alias = alias; Member = m.Member } ]
        | NProperty (p, _) ->
            let alias = visitAlias p.Expression
            [ { Alias = alias; Member = p.Member } ]
        | _ -> notImpl()

    visit (ExpressionNormalizer.toNormalizedExpression (propertySelector :> Expression))

/// Returns a column MemberInfo.
let visitPropertySelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (nexp: NormalizedExpression) : MemberInfo =
        match nexp with
        | NMemberAccess(inner, m) ->
            if m.Member.DeclaringType |> isOptionOrNullableType
            then visit inner
            else m.Member
        | NProperty (p, _) -> p.Member
        | _ -> notImpl()

    visit (ExpressionNormalizer.toNormalizedExpression (propertySelector :> Expression))

[<NoComparison>]
type Selection =
    | SelectedTable of tableAlias: string * tableType: Type
    | SelectedColumn of tableAlias: string * column: string * columnType: Type * isOpt: bool * isNullable: bool
    | SelectedExpression of sqlFragment: string
    /// Select projection with bound parameters (e.g. `1.0 - cosine_distance(col, inlineValue v)`).
    /// Fragment uses `?` placeholders that the emitter binds in order.
    | SelectedExpressionWithParams of sqlFragment: string * parameters: obj[]
    /// Selection with an explicit `AS "alias"` (anonymous-record field name).
    /// Wraps any of the above; the inner Selection is rendered, then the alias appended.
    | SelectedAs of inner: Selection * alias: string


/// Visits a join predicate expression and builds a WhereClause for the JOIN ON condition.
/// Used by the `on'` operation to support predicate-style joins.
let visitJoinPredicate<'T> (tables: TableMapping seq) (predicate: Expression<Func<'T, bool>>) (qualifyColumn: string -> MemberInfo -> string) : WhereClause =
    /// A column/property on a mapped table/record.
    let (|NColumn|_|) (nexp: NormalizedExpression) =
        match nexp with
        | NProperty (p, ext) when tables |> Seq.exists (fun tbl -> tbl.IsInTable p) -> Some (p, ext)
        | _ -> None

    let rec visit (nexp: NormalizedExpression) : WhereClause =
        match nexp with
        | NBinaryAnd(left, right) ->
            let lt = visit left
            let rt = visit right
            WhereClause.combineAndFlat lt rt
        | NBinaryOr(left, right) ->
            let lt = visit left
            let rt = visit right
            WhereClause.combineOr lt rt
        | NBinaryCompare(left, op, right) ->
            let compOp = toComparisonOp op
            match left, right with
            // Handle col to col comparisons (the primary join case)
            | NColumn (p1, _), NColumn (p2, _) ->
                let lt =
                    let alias = visitAlias p1.Expression
                    qualifyColumn alias p1.Member
                let rt =
                    let alias = visitAlias p2.Expression
                    qualifyColumn alias p2.Member
                CompareColumns(lt, compOp, rt)

            // Handle column to value comparisons
            | NColumn (p, _), NValue value ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                match value with
                | null when op = ExpressionType.Equal -> IsNull(fqCol)
                | null when op = ExpressionType.NotEqual -> IsNotNull(fqCol)
                | _ ->
                    let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                    Compare(fqCol, compOp, Parameter queryParameter)

            // Handle value to column comparisons (reversed)
            | NValue value, NColumn (p, _) ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                let reversedOp = reverseComparisonOp compOp
                match value with
                | null when reversedOp = Eq -> IsNull(fqCol)
                | null when reversedOp = NotEq -> IsNotNull(fqCol)
                | _ ->
                    let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                    Compare(fqCol, reversedOp, Parameter queryParameter)

            // Option.Value / Nullable.Value column compared to another column → CompareColumns.
            | NColumn (p, ext), NColumn (p2, _) when ext = ExtProperty.Value ->
                let alias1 = visitAlias p.Expression
                let m1 = tryGetMember p
                let lt = qualifyColumn alias1 m1.Value.Member
                let alias2 = visitAlias p2.Expression
                let rt = qualifyColumn alias2 p2.Member
                CompareColumns(lt, compOp, rt)
            | NColumn (p1, _), NColumn (p2, ext2) when ext2 = ExtProperty.Value ->
                let alias1 = visitAlias p1.Expression
                let lt = qualifyColumn alias1 p1.Member
                let alias2 = visitAlias p2.Expression
                let m2 = tryGetMember p2
                let rt = qualifyColumn alias2 m2.Value.Member
                CompareColumns(lt, compOp, rt)
            // Both sides Value-wrapped (e.g. left.Value.x = right.Value.y)
            | NColumn (p1, ext1), NColumn (p2, ext2) when ext1 = ExtProperty.Value && ext2 = ExtProperty.Value ->
                let alias1 = visitAlias p1.Expression
                let m1 = tryGetMember p1
                let lt = qualifyColumn alias1 m1.Value.Member
                let alias2 = visitAlias p2.Expression
                let m2 = tryGetMember p2
                let rt = qualifyColumn alias2 m2.Value.Member
                CompareColumns(lt, compOp, rt)
            // Nullable.Value / Option.Value column compared to a value (compile-eval'd if needed).
            | NColumn (p, ext), _ when ext = ExtProperty.Value ->
                let value =
                    match right with
                    | NValue v -> v
                    | NMemberAccess(_, m) -> compileAndEvaluateExpression (m :> Expression)
                    | NUnknown exp -> compileAndEvaluateExpression exp
                    | _ -> notImplMsg "Unable to evaluate join predicate value"
                let alias = visitAlias p.Expression
                let m = tryGetMember p
                let fqCol = qualifyColumn alias m.Value.Member
                match value with
                | null when op = ExpressionType.Equal -> IsNull(fqCol)
                | null when op = ExpressionType.NotEqual -> IsNotNull(fqCol)
                | _ ->
                    let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                    Compare(fqCol, compOp, Parameter queryParameter)

            // Column compared to a non-NValue expression. Could be either:
            //   (a) a captured local / static member — compile-and-eval to a parameter, or
            //   (b) a column reference whose receiver isn't in the local `tables` map (e.g. a
            //       correlated outer-scope parameter from a lateral subquery).
            // Try compile-and-eval first; if it fails (because the expression references a
            // free query parameter), fall back to treating it as a column ref via the
            // underlying member chain.
            | NColumn (p, _), (NMemberAccess _ | NMethodCall _ | NUnknown _) ->
                let evalRhs () =
                    match right with
                    | NMemberAccess(_, m) -> Some (compileAndEvaluateExpression (m :> Expression))
                    | NMethodCall(m, _) -> Some (compileAndEvaluateExpression (m :> Expression))
                    | NUnknown e -> Some (compileAndEvaluateExpression e)
                    | _ -> None
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                let tryColumnRef () =
                    match right with
                    | NMemberAccess(_, m) when m.Expression <> null ->
                        try
                            let rhsAlias = visitAlias m.Expression
                            Some (qualifyColumn rhsAlias m.Member)
                        with _ -> None
                    | _ -> None
                let result =
                    try evalRhs () |> Option.map Choice1Of2
                    with _ -> tryColumnRef () |> Option.map Choice2Of2
                match result with
                | Some (Choice1Of2 value) ->
                    match value with
                    | null when op = ExpressionType.Equal -> IsNull(fqCol)
                    | null when op = ExpressionType.NotEqual -> IsNotNull(fqCol)
                    | _ ->
                        let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                        Compare(fqCol, compOp, Parameter queryParameter)
                | Some (Choice2Of2 rhsCol) ->
                    CompareColumns(fqCol, compOp, rhsCol)
                | None ->
                    notImplMsg $"Unable to render join predicate RHS: {right}"

            // Reverse: captured value or outer-scope column compared to local column.
            | (NMemberAccess _ | NMethodCall _ | NUnknown _), NColumn (p, _) ->
                let evalLhs () =
                    match left with
                    | NMemberAccess(_, m) -> Some (compileAndEvaluateExpression (m :> Expression))
                    | NMethodCall(m, _) -> Some (compileAndEvaluateExpression (m :> Expression))
                    | NUnknown e -> Some (compileAndEvaluateExpression e)
                    | _ -> None
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                let tryColumnRef () =
                    match left with
                    | NMemberAccess(_, m) when m.Expression <> null ->
                        try
                            let lhsAlias = visitAlias m.Expression
                            Some (qualifyColumn lhsAlias m.Member)
                        with _ -> None
                    | _ -> None
                let result =
                    try evalLhs () |> Option.map Choice1Of2
                    with _ -> tryColumnRef () |> Option.map Choice2Of2
                let reversedOp = reverseComparisonOp compOp
                match result with
                | Some (Choice1Of2 value) ->
                    match value with
                    | null when reversedOp = Eq -> IsNull(fqCol)
                    | null when reversedOp = NotEq -> IsNotNull(fqCol)
                    | _ ->
                        let queryParameter = QueryUtils.getQueryParameterForValue p.Member value
                        Compare(fqCol, reversedOp, Parameter queryParameter)
                | Some (Choice2Of2 lhsCol) ->
                    CompareColumns(lhsCol, compOp, fqCol)
                | None ->
                    notImplMsg $"Unable to render join predicate LHS: {left}"

            | _ ->
                notImplMsg $"Unsupported join predicate comparison: {op}"
        | _ ->
            notImplMsg $"Unsupported join predicate expression: {nexp}"

    visit (ExpressionNormalizer.toNormalizedExpression (predicate :> Expression))

/// Renders a select-projection expression to a SQL fragment with bound parameters.
/// Used for arithmetic/inlineValue/method-call combinations like `1.0 - cosine_distance(col, inlineValue v)`.
/// inlineValue args are compile-and-eval'd as bound parameters (`?`); columns are qualified;
/// InfixOperators rewrites apply.
let private renderSelectExpression (exp: Expression) : string * obj[] =
    let parms = ResizeArray<obj>()
    let qualifyColumn alias (mem: MemberInfo) = $"{{{alias}}}.{{{mem.Name}}}"
    let rec render (e: Expression) : string =
        match e with
        | :? UnaryExpression as u when u.NodeType = ExpressionType.Convert ->
            render u.Operand
        | :? MethodCallExpression as mc when mc.Method.Name = nameof inlineValue && mc.Arguments.Count = 1 ->
            let value = compileAndEval mc.Arguments.[0]
            parms.Add(if isNull value then box System.DBNull.Value else value)
            "?"
        | :? MemberExpression as mem when mem.Expression <> null ->
            let alias = visitAlias mem.Expression
            qualifyColumn alias mem.Member
        | :? ConstantExpression as c -> renderConstant true c
        | :? BinaryExpression as b ->
            let left = render b.Left
            let right = render b.Right
            let op =
                match tryGetBinaryOp b.NodeType with
                | Some s -> s
                | None -> notImplMsg $"Unsupported binary operator in select expression: {b.NodeType}"
            $"{left} {op} {right}"
        | :? MethodCallExpression as mc when mc.Arguments.Count = 2 && (InfixOperators.tryGetOperator mc.Method.Name).IsSome ->
            let op = (InfixOperators.tryGetOperator mc.Method.Name).Value
            $"({render mc.Arguments.[0]} {op} {render mc.Arguments.[1]})"
        // Aggregates: countBy/sumBy/etc. → SUM(col), COUNT(DISTINCT col) for countDistinct.
        | :? MethodCallExpression as mc when aggregateMethodNames.Contains mc.Method.Name ->
            let aggType = aggTypeOf mc.Method.Name
            renderAggregate aggType (render mc.Arguments.[0])
        | :? MethodCallExpression as mc ->
            // Delegate to visitSqlFn for caseWhen/castAs/coalesce/etc. Fall back to a
            // generic "name(args)" form only if visitSqlFn explicitly rejects the shape.
            try visitSqlFn qualifyColumn (mc :> Expression)
            with :? System.NotImplementedException ->
                let args = mc.Arguments |> Seq.map render |> String.concat ", "
                $"{mc.Method.Name.ToUpperInvariant()}({args})"
        | _ ->
            notImplMsg $"Unsupported expression in select projection: {e.NodeType}"
    let frag = render exp
    frag, parms.ToArray()

/// Returns a list of one or more fully qualified table names: ["{schema}.{table}"]
let visitSelect<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (nexp: NormalizedExpression) : Selection list =
        match nexp with
        | NMethodCall(m, args) when m.Method.Name = "Some" ->
            visit args.[0]
        // Handle direct OptionModule.Map calls
        | NMethodCall(m, args) when m.Method.Name = "Map"
            && m.Method.DeclaringType <> null
            && m.Method.DeclaringType.Name = "OptionModule"
            && args.Length = 2 ->
            let source = m.Arguments.[1] // original Expression for visitAlias
            let mappingArg = m.Arguments.[0]
            let rec extractMember (exp: Expression) =
                match exp with
                | :? LambdaExpression as lam -> extractMember lam.Body
                | :? UnaryExpression as u when u.NodeType = ExpressionType.Convert -> extractMember u.Operand
                | Member m -> Some m
                | _ -> None
            match extractMember mappingArg with
            | Some memberExp ->
                let alias = visitAlias source
                [ SelectedColumn (alias, memberExp.Member.Name, memberExp.Type, true, false) ]
            | None -> notImplMsg $"Unsupported Option.map mapping expression: {mappingArg.NodeType}"
        | NMethodCall(m, _) when m.Method.Name = "op_PipeRight" && m.Arguments.Count = 2 ->
            // Handle: r |> Option.map _.ColumnA
            // Use original Expression arguments for the complex Option.map lambda extraction
            let source = m.Arguments.[0]
            let pipeArg = m.Arguments.[1]
            let rec findOptionMapLambda (exp: Expression) =
                match exp with
                | :? MethodCallExpression as invoke when invoke.Method.Name = "Invoke" ->
                    match invoke.Arguments.[0] with
                    | :? MethodCallExpression as toFF when toFF.Method.Name = "ToFSharpFunc" ->
                        match toFF.Arguments.[0] with
                        | :? LambdaExpression as mapLam -> Some mapLam
                        | _ -> None
                    | _ -> None
                | :? MethodCallExpression as mc when
                    mc.Method.Name = "Map"
                    && mc.Method.DeclaringType <> null
                    && mc.Method.DeclaringType.Name = "OptionModule"
                    && mc.Arguments.Count = 2 ->
                    match mc.Arguments.[0] with
                    | :? LambdaExpression as mapLam -> Some mapLam
                    | :? MethodCallExpression as toFF when toFF.Method.Name = "ToFSharpFunc" ->
                        match toFF.Arguments.[0] with
                        | :? LambdaExpression as mapLam -> Some mapLam
                        | _ -> None
                    | _ -> None
                | :? MethodCallExpression as mc when mc.Method.Name = "ToFSharpFunc" && mc.Arguments.Count = 1 ->
                    match mc.Arguments.[0] with
                    | :? LambdaExpression as lam -> findOptionMapLambda lam.Body
                    | _ -> None
                | :? LambdaExpression as lam -> findOptionMapLambda lam.Body
                | _ -> None
            let rec containsOptionMap (exp: Expression) =
                match exp with
                | :? MethodCallExpression as mc ->
                    mc.Method.Name = "Map" && mc.Method.DeclaringType <> null && mc.Method.DeclaringType.Name = "OptionModule"
                    || mc.Arguments |> Seq.exists containsOptionMap
                    || (mc.Object <> null && containsOptionMap mc.Object)
                | :? LambdaExpression as lam -> containsOptionMap lam.Body
                | _ -> false
            if containsOptionMap pipeArg then
                match findOptionMapLambda pipeArg with
                | Some mapLam ->
                    match mapLam.Body with
                    | Member memberExp ->
                        let alias = visitAlias source
                        [ SelectedColumn (alias, memberExp.Member.Name, memberExp.Type, true, false) ]
                    | _ -> notImplMsg $"Unsupported Option.map lambda body: {mapLam.Body.NodeType}"
                | None -> notImplMsg $"Could not extract mapping lambda from Option.map expression"
            else
                let qualifyCol alias (mem: MemberInfo) = $"{{%s{alias}}}.{{%s{mem.Name}}}"
                let sqlFragment = visitSqlFn qualifyCol (m :> Expression)
                [ SelectedExpression sqlFragment ]
        | NAggregateColumn (aggType, (p, _)) ->
            let alias = visitAlias p.Expression
            let fqCol = $"{{%s{alias}}}.{{%s{p.Member.Name}}}"
            [ SelectedExpression (renderAggregate aggType fqCol) ]
        | NMethodCall(m, _) ->
            let qualifyCol alias (mem: MemberInfo) = $"{{%s{alias}}}.{{%s{mem.Name}}}"
            let sqlFragment = visitSqlFn qualifyCol (m :> Expression)
            [ SelectedExpression sqlFragment ]
        | NNew(newExpr, args) ->
            // Each anonymous-record / tuple / DU field initializer is one Selection.
            // newExpr.Members carries the member info for each constructor arg, which gives us
            // the F# field name to use as a SQL `AS "alias"` so the consumer reader can read by
            // field name rather than the underlying column name.
            // Binary, Unary, and inlineValue-bearing initializers fall through to the
            // expression-walker that emits SelectedExpressionWithParams.
            let memberNames =
                if newExpr.Members <> null && newExpr.Members.Count = args.Length then
                    newExpr.Members |> Seq.map (fun m -> Some m.Name) |> Seq.toList
                else
                    // Fallback: read fields/properties from the anonymous record type itself.
                    let t = newExpr.Type
                    let props = t.GetProperties()
                    if props.Length = args.Length then
                        props |> Array.map (fun p -> Some p.Name) |> Array.toList
                    else
                        args |> List.map (fun _ -> None)
            args
            |> List.mapi (fun i nargs -> i, nargs, memberNames.[i])
            |> List.collect (fun (i, narg, fieldName) ->
                let inner =
                    match narg with
                    | NMethodCall _ | NAggregateColumn _ | NMemberAccess _ | NParameter _ | NNew _ ->
                        visit narg
                    | _ ->
                        let frag, parms = renderSelectExpression newExpr.Arguments.[i]
                        [ SelectedExpressionWithParams (frag, parms) ]
                // Wrap with SelectedAs only when the field name is meaningful (not Item1/Item2/...
                // tuple-positional names) and differs from the underlying column.
                let isTuplePositional (n: string) =
                    n.StartsWith("Item")
                    && n.Length > 4
                    && System.Char.IsDigit(n.[4])
                let isMeaningfulAlias n = not (isTuplePositional n)
                match fieldName, inner with
                | Some fname, [ SelectedColumn (_, col, _, _, _) as sel ] when isMeaningfulAlias fname && fname <> col ->
                    [ SelectedAs (sel, fname) ]
                | Some fname, [ (SelectedExpression _ | SelectedExpressionWithParams _) as sel ] when isMeaningfulAlias fname ->
                    [ SelectedAs (sel, fname) ]
                | _ -> inner)
        | NParameter p ->
            [ SelectedTable (p.Name, p.Type) ]
        | NMemberAccess(inner, m) ->
            if m.Member.DeclaringType |> isOptionOrNullableType then
                visit inner
            else
                let isOptional, isNullable =
                    if m.Type.IsGenericType && m.Type.GetGenericTypeDefinition() = typedefof<Option<_>> then true, false
                    elif m.Type.IsGenericType && m.Type.GetGenericTypeDefinition() = typedefof<Nullable<_>> then false, true
                    else false, false
                let alias = visitAlias m.Expression
                [ SelectedColumn (alias, m.Member.Name, m.Type, isOptional, isNullable) ]
        | _ ->
            notImpl()

    visit (ExpressionNormalizer.toNormalizedExpression (propertySelector :> Expression))
