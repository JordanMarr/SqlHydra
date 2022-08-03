module internal SqlHydra.Query.LinqExpressionVisitors

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

    /// A property member, a property wrapped in 'Some', or an option 'Value', along with the object the property is being
    /// called on
    let (|PropertyWithThis|_|) (exp: Expression) =
        let tryGetMember(x: Expression) = 
            match x with
            | Member m when m.Expression.NodeType = ExpressionType.Parameter -> 
                Some (m.Expression, m.Member)
            | MethodCall opt when opt.Type |> isOptionType ->
                if opt.Arguments.Count > 0 then
                    // Option.Some
                    match opt.Arguments.[0] with
                    | Member m -> Some (m.Expression, m.Member)
                    | _ -> None
                else None
            | _ -> None

        match exp with
        | Member m when m.Member.DeclaringType <> null && m.Member.DeclaringType |> isOptionType -> 
            // Handles option '.Value'
            tryGetMember m.Expression
        | _ -> 
            tryGetMember exp
    
    /// A property member, a property wrapped in 'Some', or an option 'Value'.
    let (|Property|_|) (exp: Expression) = (|PropertyWithThis|_|) exp |> Option.map snd
            

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
        | MethodCall opt when opt.Type |> isOptionType ->        
            if opt.Arguments.Count > 0 then
                // Option.Some
                match opt.Arguments.[0] with
                | Constant c -> Some c.Value
                | _ -> None
            else
                // Option.None
                Some null
        | _ -> None
    
    let (|AggregateColumn|_|) (exp: Expression) : (string * ParameterExpression * MemberInfo) option =
        match exp with
        | MethodCall m when List.contains m.Method.Name [ nameof minBy; nameof maxBy; nameof sumBy; nameof avgBy; nameof countBy; nameof avgByAs ] ->
            let aggType = m.Method.Name.Replace("By", "").Replace("As", "").ToUpper()
            match m.Arguments.[0] with
            | PropertyWithThis (Parameter pThis, p) -> Some (aggType, pThis, p)
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
                let fqCol = qualifyColumn p
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                match m.Method.Name with
                | nameof isIn | nameof op_BarEqualsBar -> query.WhereIn(fqCol, selectSubquery.ToKataQuery())
                | _ -> query.WhereNotIn(fqCol, selectSubquery.ToKataQuery())
            // Column is IN / NOT IN a list of values
            | Property p, ListInit values ->
                filter(qualifyColumn p, values)
            // Column is IN / NOT IN an array of values
            | Property p, ArrayInit values -> 
                filter(qualifyColumn p, values)
            // Column is IN / NOT IN an IEnumerable of values
            | Property p, Value value -> 
                let lstValues = (value :?> System.Collections.IEnumerable) |> Seq.cast<obj> |> Seq.toList
                filter(qualifyColumn p, lstValues)
            // Column is IN / NOT IN a sequence expression of values
            | Property p, MethodCall c when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()
        | MethodCall m when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match m.Arguments.[0], m.Arguments.[1] with
            | Property p, Value value -> 
                let pattern = string value
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> query.WhereLike(qualifyColumn p, pattern, false)
                | _ -> query.WhereNotLike(qualifyColumn p, pattern, false)
            | _ -> notImpl()
        | MethodCall m when m.Method.Name = nameof isNullValue || m.Method.Name = nameof isNotNullValue ->
            match m.Arguments.[0] with
            | Property p ->
                // FIXME: use table aliases instead of types
                if m.Method.Name = nameof isNullValue
                then query.WhereNull(qualifyColumn p)
                else query.WhereNotNull(qualifyColumn p)
            | Member mm ->
                match mm.Expression with
                | Member mmm ->
                    if mmm.Member.Name = "Value" && mmm.Member.DeclaringType |> isOptionType then
                        let table = (mmm.Expression :?> ParameterExpression).Name
                        let col = mm.Member.Name
                        let fqCol = table + "." + col
                        if m.Method.Name = nameof isNullValue
                        then query.WhereNull(fqCol)
                        else query.WhereNotNull(fqCol)
                    else notImpl ()
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
                query.Where(qualifyColumn p1, comparison, selectSubquery.ToKataQuery())
            | Property p1, Property p2 ->
                // Handle col to col comparisons
                let lt = qualifyColumn p1
                let comparison = getComparison exp.NodeType
                let rt = qualifyColumn p2
                query.WhereColumns(lt, comparison, rt)
            | Property p, Value value ->
                // Handle column to value comparisons
                let comparison = getComparison(exp.NodeType)
                query.Where(qualifyColumn p, comparison, value)
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
                let fqCol = qualifyColumn p
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                match m.Method.Name with
                | nameof isIn | nameof op_BarEqualsBar -> query.HavingIn(fqCol, selectSubquery.ToKataQuery())
                | _ -> query.HavingNotIn(fqCol, selectSubquery.ToKataQuery())
            // Column is IN / NOT IN a list of values
            | Property p, ListInit values ->
                filter(qualifyColumn p, values)
            // Column is IN / NOT IN an array of values
            | Property p, ArrayInit values -> 
                filter(qualifyColumn p, values)
            // Column is IN / NOT IN an IEnumerable of values
            | Property p, Value value -> 
                let lstValues = (value :?> System.Collections.IEnumerable) |> Seq.cast<obj> |> Seq.toList
                filter(qualifyColumn p, lstValues)
            // Column is IN / NOT IN a sequence expression of values
            | Property p, MethodCall c when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()
        | MethodCall m when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match m.Arguments.[0], m.Arguments.[1] with
            | Property p, Value value -> 
                let pattern = string value
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> query.HavingLike(qualifyColumn p, pattern, false)
                | _ -> query.HavingNotLike(qualifyColumn p, pattern, false)
            | _ -> notImpl()
        | MethodCall m when m.Method.Name = nameof isNullValue || m.Method.Name = nameof isNotNullValue ->
            match m.Arguments.[0] with
            | Property p -> 
                if m.Method.Name = nameof isNullValue
                then query.HavingNull(qualifyColumn p)
                else query.HavingNotNull(qualifyColumn p)
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
                query.Having(qualifyColumn p1, comparison, selectSubquery.ToKataQuery())
            | AggregateColumn (aggType, _, p1), Property p2 ->
                // Handle aggregate col to col comparisons
                let lt = qualifyColumn p1
                let comparison = getComparison exp.NodeType
                let rt = qualifyColumn p2
                query.HavingRaw($"{aggType}({lt}) {comparison} {rt}")
            | AggregateColumn (aggType, _, p), Value value ->
                // Handle aggregate column to value comparisons
                let lt = qualifyColumn p
                let comparison = getComparison(exp.NodeType)
                query.HavingRaw($"{aggType}({lt}) {comparison} ?", [value])
            | Property p1, Property p2 ->
                // Handle col to col comparisons
                let lt = qualifyColumn p1
                let comparison = getComparison exp.NodeType
                let rt = qualifyColumn p2
                query.HavingColumns(lt, comparison, rt)
            | Property p, Value value ->
                // Handle column to value comparisons
                let comparison = getComparison(exp.NodeType)
                query.Having(qualifyColumn p, comparison, value)
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
    | OrderByColumn of string * MemberInfo
    | OrderByAggregateColumn of aggregateType: string * MemberInfo

/// Returns a column MemberInfo.
let visitOrderByPropertySelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : OrderBy =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | AggregateColumn (aggType, _, p) -> OrderByAggregateColumn (aggType, p)
        | Member m -> 
            if m.Member.DeclaringType |> isOptionType
            then visit m.Expression
            else
                match m.Expression with
                | Parameter mp -> OrderByColumn (mp.Name, m.Member)
                | _ -> notImpl ()
        | PropertyWithThis (tblExp, p) -> OrderByColumn ((tblExp :?> ParameterExpression).Name, p)
        | _ -> notImpl()

    visit (propertySelector :> Expression)

/// Returns the table alias (if any), and one or more column members
let visitJoin<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : (string * MemberInfo) list =
        match exp with
        | Lambda x -> (*Some x.Parameters[0].Name,*) (visit x.Body)
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | New n -> 
            // Handle groupBy that returns a tuple of multiple columns
            (n.Arguments |> Seq.map visit |> Seq.toList |> List.collect id)
        | Member m ->
            if m.Member.DeclaringType |> isOptionType then
                visit m.Expression
            else
                let mObjName =
                    match m.Expression with
                    | Parameter p -> p.Name
                    | Member m -> (m.Expression :?> ParameterExpression).Name
                    | _ -> notImpl ()
                [ mObjName, m.Member ]
        | PropertyWithThis (Parameter obj, mi) ->
            [ obj.Name, mi ]
        | _ -> notImpl()

    visit (propertySelector :> Expression)

/// Returns a table alias and a column name.
let visitPropertySelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : {| tblAlias: string; col: MemberInfo  |} =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | Member m -> 
            if m.Member.DeclaringType |> isOptionType
            then visit m.Expression
            else
                match m.Expression with
                | Parameter mp -> {| tblAlias = mp.Name; col = m.Member |}
                | _ -> notImpl ()
        | PropertyWithThis (mThis, mi) ->
            {| tblAlias = (mThis :?> ParameterExpression).Name; col = mi |}
        | _ -> notImpl()

    visit (propertySelector :> Expression)

type Selection =
    | SelectedTable of string
    | SelectedColumn of string * string
    | SelectedAggregateColumn of aggregateType: string * tblAlias: string * col: MemberInfo

/// Returns a list of one or more fully qualified table names: ["{schema}.{table}"]
let visitSelect<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : Selection list =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | AggregateColumn (aggType, pThis, p) -> [ SelectedAggregateColumn (aggType, pThis.Name, p) ]            
        | New n -> 
            // Handle a tuple of multiple tables
            n.Arguments 
            |> Seq.map visit |> Seq.toList |> List.concat
        | Parameter p -> [ SelectedTable p.Name ]
        | Member m -> 
            if m.Member.DeclaringType |> isOptionType then
                visit m.Expression
            else
                match m.Expression with
                | Parameter mp -> [ SelectedColumn (mp.Name, m.Member.Name) ]
                | _ -> notImpl ()
        | _ -> 
            notImpl()

    visit (propertySelector :> Expression)
