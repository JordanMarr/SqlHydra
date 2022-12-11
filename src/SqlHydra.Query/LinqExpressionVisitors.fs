﻿module internal SqlHydra.Query.LinqExpressionVisitors

open System
open System.Linq.Expressions
open System.Reflection
open SqlKata

let notImpl() = raise (NotImplementedException())
let notImplMsg msg = raise (NotImplementedException msg)

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

    let (|Parameter|_|) (exp: Expression) =
        match exp.NodeType with
        | ExpressionType.Parameter -> Some (exp :?> ParameterExpression)
        | _ -> None

[<AutoOpen>]
module SqlPatterns = 

    let (|Not|_|) (exp: Expression) = 
        match exp.NodeType with
        | ExpressionType.Not -> Some (exp :?> UnaryExpression)
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

    let isOptionType (t: Type) = 
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>>

    /// A property member, a property wrapped in 'Some', or an option 'Value'.
    let (|Property|_|) (exp: Expression) =
        let tryGetMember(x: Expression) = 
            match x with
            | Member m when m.Expression.NodeType = ExpressionType.Parameter -> 
                Some m
            | MethodCall opt when opt.Type |> isOptionType ->        
                if opt.Arguments.Count > 0 then
                    // Option.Some
                    match opt.Arguments.[0] with
                    | Member m -> Some m
                    | _ -> None
                else None
            | _ -> None

        match exp with
        | Member m when m.Member.DeclaringType <> null && m.Member.DeclaringType |> isOptionType -> 
            // Handles option '.Value'
            tryGetMember m.Expression
        | _ -> 
            tryGetMember exp
            

    /// A constant value or an optional constant value
    let (|Value|_|) (exp: Expression) =
        match exp with
        | New n when n.Type.Name = "Guid" -> 
            let value = (n.Arguments.[0] :?> ConstantExpression).Value :?> string
            Some (Guid(value) |> box)
        | Member m when m.Expression.NodeType = ExpressionType.Constant -> 
            // Extract constant value from property (probably a record property)
            // NOTE: This currently does not unwind nested properties! 
            // NOTE: This uses reflection; it is more performant for user to manually unwrap and pass in constant.
            let parentObject = (m.Expression :?> ConstantExpression).Value
            match m.Member.MemberType with
            | MemberTypes.Field -> (m.Member :?> FieldInfo).GetValue(parentObject) |> Some
            | MemberTypes.Property -> (m.Member :?> PropertyInfo).GetValue(parentObject) |> Some
            | _ -> notImplMsg(sprintf "Unable to unwrap where value for '%s'" m.Member.Name)
        | Member m when m.Expression.NodeType = ExpressionType.MemberAccess -> 
            // Extract constant value from nested object/properties
            notImplMsg "Nested property value extraction is not supported in 'where' statements. Try manually unwrapping and passing in the value."
        | Constant c -> Some c.Value
        | ImplConvertConstant c -> Some c.Value
        | MethodCall opt when opt.Type |> isOptionType ->        
            if opt.Arguments.Count > 0 then
                // Option.Some
                match opt.Arguments.[0] with
                | Constant c -> Some c.Value
                | ImplConvertConstant c -> Some c.Value
                | _ -> None
            else
                // Option.None
                Some null
        | _ -> None

    let (|AggregateColumn|_|) (exp: Expression) =
        match exp with
        | MethodCall m when List.contains m.Method.Name [ nameof minBy; nameof maxBy; nameof sumBy; nameof avgBy; nameof countBy; nameof avgByAs ] ->
            let aggType = m.Method.Name.Replace("By", "").Replace("As", "").ToUpper()
            match m.Arguments.[0] with
            | Property p -> Some (aggType, p)
            | _ -> notImplMsg "Invalid argument to aggregate function."
        | _ -> None

let getComparison (expType: ExpressionType) =
    match expType with
    | ExpressionType.Equal -> "="
    | ExpressionType.NotEqual -> "<>"
    | ExpressionType.GreaterThan -> ">"
    | ExpressionType.GreaterThanOrEqual -> ">="
    | ExpressionType.LessThan -> "<"
    | ExpressionType.LessThanOrEqual -> "<="
    | _ -> notImplMsg "Unsupported comparison type"

let visitWhere<'T> (filter: Expression<Func<'T, bool>>) (qualifyColumn: MemberInfo -> string) =
    let rec visit (exp: Expression) (query: Query) : Query =
        match exp with
        | Lambda x -> visit x.Body query
        | Not x -> 
            let operand = visit x.Operand (Query())
            query.WhereNot(fun q -> operand)
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object (Query())
        | MethodCall m when List.contains m.Method.Name [ nameof isIn; nameof isNotIn; nameof op_BarEqualsBar; nameof op_BarLessGreaterBar ] ->
            let filter : (string * seq<obj>) -> Query = 
                match m.Method.Name with
                | nameof isIn | nameof op_BarEqualsBar -> query.WhereIn
                | _ -> query.WhereNotIn

            match m.Arguments.[0], m.Arguments.[1] with
            // Column is IN / NOT IN a subquery of values
            | Property p, MethodCall subqueryExpr when subqueryExpr.Method.Name = nameof subqueryMany ->
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let fqCol = qualifyColumn p.Member
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                match m.Method.Name with
                | nameof isIn | nameof op_BarEqualsBar -> query.WhereIn(fqCol, selectSubquery.ToKataQuery())
                | _ -> query.WhereNotIn(fqCol, selectSubquery.ToKataQuery())
            // Column is IN / NOT IN a list of values
            | Property p, ListInit values ->
                let queryParameters = 
                    values 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                filter(qualifyColumn p.Member, queryParameters)
            // Column is IN / NOT IN an array of values
            | Property p, ArrayInit values -> 
                let queryParameters = 
                    values 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                filter(qualifyColumn p.Member, queryParameters)
            // Column is IN / NOT IN an IEnumerable of values
            | Property p, Value value -> 
                let queryParameters = 
                    (value :?> System.Collections.IEnumerable) 
                    |> Seq.cast<obj> 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                filter(qualifyColumn p.Member, queryParameters)
            // Column is IN / NOT IN a sequence expression of values
            | Property p, MethodCall c when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()
        | MethodCall m when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match m.Arguments.[0], m.Arguments.[1] with
            | Property p, Value value -> 
                let pattern = string value
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> query.WhereLike(qualifyColumn p.Member, pattern, false)
                | _ -> query.WhereNotLike(qualifyColumn p.Member, pattern, false)
            | _ -> notImpl()
        | MethodCall m when m.Method.Name = nameof isNullValue || m.Method.Name = nameof isNotNullValue ->
            match m.Arguments.[0] with
            | Property p -> 
                if m.Method.Name = nameof isNullValue
                then query.WhereNull(qualifyColumn p.Member)
                else query.WhereNotNull(qualifyColumn p.Member)
            | _ -> notImpl()
        | BinaryAnd x ->
            let lt = visit x.Left (Query())
            let rt = visit x.Right (Query())
            query.Where(fun q -> lt).Where(fun q -> rt)
        | BinaryOr x -> 
            let lt = visit x.Left (Query())
            let rt = visit x.Right (Query())
            query.OrWhere(fun q -> lt).OrWhere(fun q -> rt)
        | BinaryCompare x ->
            match x.Left, x.Right with            
            | Property p1, MethodCall subqueryExpr when subqueryExpr.Method.Name = nameof subqueryOne ->
                // Handle property to subquery comparisons
                let comparison = getComparison exp.NodeType
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                query.Where(qualifyColumn p1.Member, comparison, selectSubquery.ToKataQuery())
            | Property p1, Property p2 ->
                // Handle col to col comparisons
                let lt = qualifyColumn p1.Member
                let comparison = getComparison exp.NodeType
                let rt = qualifyColumn p2.Member
                query.WhereColumns(lt, comparison, rt)
            | Property p, Value value ->
                // Handle column to value comparisons
                match exp.NodeType, value with
                | ExpressionType.Equal, null -> 
                    query.WhereNull(qualifyColumn p.Member)
                | ExpressionType.NotEqual, null -> 
                    query.WhereNotNull(qualifyColumn p.Member)
                | _ ->                     
                    let comparison = getComparison(exp.NodeType)
                    let queryParameter = KataUtils.getQueryParameterForValue p.Member value
                    query.Where(qualifyColumn p.Member, comparison, queryParameter)
            | Value v1, Value v2 ->
                // Not implemented because I didn't want to embed logic to properly format strings, dates, etc.
                // This can be easily added later if it is implemented in Dapper.FSharp.
                notImplMsg("Value to value comparisons are not currently supported. Ex: where (1 = 1)")
            | _ ->
                notImpl()
        | _ ->
            notImpl()

    visit (filter :> Expression) (Query())

let visitHaving<'T> (filter: Expression<Func<'T, bool>>) (qualifyColumn: MemberInfo -> string) =
    let rec visit (exp: Expression) (query: Query) : Query =
        match exp with
        | Lambda x -> visit x.Body query
        | Not x -> 
            let operand = visit x.Operand (Query())
            query.HavingNot(fun q -> operand)
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object (Query())
        | MethodCall m when List.contains m.Method.Name [ nameof isIn; nameof isNotIn; nameof op_BarEqualsBar; nameof op_BarLessGreaterBar ] ->
            let filter : (string * seq<obj>) -> Query = 
                match m.Method.Name with
                | nameof isIn | nameof op_BarEqualsBar -> query.HavingIn
                | _ -> query.HavingNotIn

            match m.Arguments.[0], m.Arguments.[1] with
            // Column is IN / NOT IN a subquery of values
            | Property p, MethodCall subqueryExpr when subqueryExpr.Method.Name = nameof subqueryMany ->
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let fqCol = qualifyColumn p.Member
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                match m.Method.Name with
                | nameof isIn | nameof op_BarEqualsBar -> query.HavingIn(fqCol, selectSubquery.ToKataQuery())
                | _ -> query.HavingNotIn(fqCol, selectSubquery.ToKataQuery())
            // Column is IN / NOT IN a list of values
            | Property p, ListInit values ->
                let queryParameters = 
                    values 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                filter(qualifyColumn p.Member, queryParameters)
            // Column is IN / NOT IN an array of values
            | Property p, ArrayInit values -> 
                let queryParameters = 
                    values 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                filter(qualifyColumn p.Member, queryParameters)
            // Column is IN / NOT IN an IEnumerable of values
            | Property p, Value value -> 
                let queryParameters = 
                    (value :?> System.Collections.IEnumerable) 
                    |> Seq.cast<obj> 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                filter(qualifyColumn p.Member, queryParameters)
            // Column is IN / NOT IN a sequence expression of values
            | Property p, MethodCall c when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()
        | MethodCall m when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match m.Arguments.[0], m.Arguments.[1] with
            | Property p, Value value -> 
                let pattern = string value
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> query.HavingLike(qualifyColumn p.Member, pattern, false)
                | _ -> query.HavingNotLike(qualifyColumn p.Member, pattern, false)
            | _ -> notImpl()
        | MethodCall m when m.Method.Name = nameof isNullValue || m.Method.Name = nameof isNotNullValue ->
            match m.Arguments.[0] with
            | Property p -> 
                if m.Method.Name = nameof isNullValue
                then query.HavingNull(qualifyColumn p.Member)
                else query.HavingNotNull(qualifyColumn p.Member)
            | _ -> notImpl()
        | MethodCall m when List.contains m.Method.Name [ nameof minBy; nameof maxBy; nameof sumBy; nameof avgBy; nameof countBy; nameof avgByAs ] ->
            // Handle aggregate columns
            visit m.Arguments.[0] query
        | BinaryAnd x ->
            let lt = visit x.Left (Query())
            let rt = visit x.Right (Query())
            query.Having(fun q -> lt).Having(fun q -> rt)
        | BinaryOr x -> 
            let lt = visit x.Left (Query())
            let rt = visit x.Right (Query())
            query.OrHaving(fun q -> lt).OrHaving(fun q -> rt)
        | BinaryCompare x ->
            match x.Left, x.Right with            
            | Property p1, MethodCall subqueryExpr when subqueryExpr.Method.Name = nameof subqueryOne ->
                // Handle property to subquery comparisons
                let comparison = getComparison exp.NodeType
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                query.Having(qualifyColumn p1.Member, comparison, selectSubquery.ToKataQuery())
            | AggregateColumn (aggType, p1), Property p2 ->
                // Handle aggregate col to col comparisons
                let lt = qualifyColumn p1.Member
                let comparison = getComparison exp.NodeType
                let rt = qualifyColumn p2.Member
                query.HavingRaw($"{aggType}({lt}) {comparison} {rt}")
            | AggregateColumn (aggType, p), Value value ->
                // Handle aggregate column to value comparisons
                let lt = qualifyColumn p.Member
                let comparison = getComparison(exp.NodeType)
                query.HavingRaw($"{aggType}({lt}) {comparison} ?", [value])
            | Property p1, Property p2 ->
                // Handle col to col comparisons
                let lt = qualifyColumn p1.Member
                let comparison = getComparison exp.NodeType
                let rt = qualifyColumn p2.Member
                query.HavingColumns(lt, comparison, rt)
            | Property p, Value value ->
                // Handle column to value comparisons
                match exp.NodeType, value with
                | ExpressionType.Equal, null -> 
                    query.WhereNull(qualifyColumn p.Member)
                | ExpressionType.NotEqual, null -> 
                    query.WhereNotNull(qualifyColumn p.Member)
                | _ ->                     
                    let comparison = getComparison(exp.NodeType)
                    let queryParameter = KataUtils.getQueryParameterForValue p.Member value
                    query.Where(qualifyColumn p.Member, comparison, queryParameter)
            | Value v1, Value v2 ->
                // Not implemented because I didn't want to embed logic to properly format strings, dates, etc.
                // This can be easily added later if it is implemented in Dapper.FSharp.
                notImplMsg("Value to value comparisons are not currently supported. Ex: having (1 = 1)")
            | _ ->
                notImpl()
        | _ ->
            notImpl()

    visit (filter :> Expression) (Query())

/// Returns a list of one or more fully qualified column names: ["{schema}.{table}.{column}"]
let visitPropertiesSelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) (qualifyColumn: MemberInfo -> string) =
    let rec visit (exp: Expression) : string list =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | New n -> 
            // Handle groupBy that returns a tuple of multiple columns
            n.Arguments |> Seq.map visit |> Seq.toList |> List.concat
        | Member m -> 
            // Handle groupBy for a single column
            let column = qualifyColumn m.Member
            [column]
        | _ -> notImpl()

    visit (propertySelector :> Expression)

type OrderBy =
    | OrderByColumn of MemberInfo
    | OrderByAggregateColumn of aggregateType: string * MemberInfo

/// Returns a column MemberInfo.
let visitOrderByPropertySelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : OrderBy =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | AggregateColumn (aggType, p) -> OrderByAggregateColumn (aggType, p.Member)
        | Member m -> 
            if m.Member.DeclaringType |> isOptionType
            then visit m.Expression
            else OrderByColumn m.Member
        | Property p -> OrderByColumn p.Member
        | _ -> notImpl()

    visit (propertySelector :> Expression)

type JoinedPropertyInfo = 
    {
        Alias: string
        Member: MemberInfo
    }

/// Returns one or more column members
let visitJoin<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : JoinedPropertyInfo list =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | New n -> 
            // Handle groupBy that returns a tuple of multiple columns
            n.Arguments |> Seq.map visit |> Seq.toList |> List.collect id
        | Member m -> 
            let alias =
                match m.Expression with
                | Parameter p -> p.Name
                | Member m -> (m.Expression :?> ParameterExpression).Name
                | _ -> notImpl()

            if m.Member.DeclaringType |> isOptionType
            then visit m.Expression
            else [ { Alias = alias; Member = m.Member } ]
        | Property p -> 
            let alias = (p.Expression :?> ParameterExpression).Name
            [ { Alias = alias; Member = p.Member }  ]
        | _ -> notImpl()

    visit (propertySelector :> Expression)

/// Returns a column MemberInfo.
let visitPropertySelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : MemberInfo =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | Member m -> 
            if m.Member.DeclaringType |> isOptionType
            then visit m.Expression
            else m.Member
        | Property p -> p.Member
        | _ -> notImpl()

    visit (propertySelector :> Expression)

type Selection =
    | SelectedTable of Type
    | SelectedColumn of MemberInfo
    | SelectedAggregateColumn of aggregateType: string * MemberInfo

/// Returns a list of one or more fully qualified table names: ["{schema}.{table}"]
let visitSelect<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : Selection list =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | AggregateColumn (aggType, p) -> [ SelectedAggregateColumn (aggType, p.Member) ]            
        | New n -> 
            // Handle a tuple of multiple tables
            n.Arguments 
            |> Seq.map visit |> Seq.toList |> List.concat
        | Parameter p -> 
            if p.Type |> isOptionType then
                let innerType = p.Type.GenericTypeArguments.[0]
                [ SelectedTable innerType ]
            else
                [ SelectedTable p.Type ]
        | Member m -> 
            if m.Member.DeclaringType |> isOptionType 
            then visit m.Expression
            else [ SelectedColumn m.Member ]
        | _ -> 
            notImpl()

    visit (propertySelector :> Expression)
