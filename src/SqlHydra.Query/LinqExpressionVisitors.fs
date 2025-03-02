module internal SqlHydra.Query.LinqExpressionVisitors

open System
open System.Data.SqlTypes
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

    let (|BoolMember|_|) (exp: Expression) = 
        match exp with
        | Member m when m.Type = typeof<bool> -> Some m
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

    /// A property member, a property wrapped in 'Some', or an option 'Value'.
    let (|Property|_|) (exp: Expression) =
        match exp with
        | Member m when m.Member.DeclaringType <> null && m.Member.DeclaringType |> isOptionOrNullableType && m.Member.Name = "Value" -> 
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
            let rec unwrapMember (m: MemberExpression) =
                match m.Expression with
                | Constant c -> Some c.Value
                | Member m -> unwrapMember m
                | _ -> None
            unwrapMember m
        | Constant c -> Some c.Value
        | ImplConvertConstant c -> Some c.Value
        | MethodCall opt when opt.Type |> isOptionOrNullableType ->        
            if opt.Arguments.Count > 0 then
                // Option.Some
                match opt.Arguments.[0] with
                | Constant c -> Some c.Value
                | ImplConvertConstant c -> Some c.Value
                | _ -> None
            else
                // Option.None
                Some null
        | Unary u -> 
            match u.Operand with
            | Constant c -> Some c.Value
            | _ -> None
        | Member m when m.Type.Name.StartsWith("Nullable") -> 
            // Handles nullable types
            // Extract constant value from nested object/properties
            let rec unwrapMember (m: MemberExpression) =
                match m.Expression with
                | Constant c -> Some c.Value
                | Member m -> unwrapMember m
                | _ -> None
            unwrapMember m
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

let reverseComparison (expType: ExpressionType) =
    match expType with
    | ExpressionType.GreaterThan -> ExpressionType.LessThan
    | ExpressionType.GreaterThanOrEqual -> ExpressionType.LessThanOrEqual
    | ExpressionType.LessThan -> ExpressionType.GreaterThan
    | ExpressionType.LessThanOrEqual -> ExpressionType.GreaterThanOrEqual
    | _ -> expType


let getReverseComparison = getComparison << reverseComparison
    
let visitAlias (exp: Expression) = 
    let rec visit (exp: Expression) = 
        match exp with 
        | Member m -> visit m.Expression
        | Property p -> visit p.Expression
        | Parameter p -> p.Name
        | _ -> notImpl()
    visit exp


let visitWhere<'T> (filter: Expression<Func<'T, bool>>) (qualifyColumn: string -> MemberInfo -> string) =
    let rec visit (exp: Expression) (query: Query) : Query =
        match exp with
        | Lambda x -> visit x.Body query
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object (Query())
        | MethodCall m when List.contains m.Method.Name [ nameof isIn; nameof isNotIn; nameof op_BarEqualsBar; nameof op_BarLessGreaterBar ] ->
            let filter : (string * seq<obj>) -> Query = 
                match m.Method.Name with
                | nameof isIn | nameof op_BarEqualsBar -> query.WhereIn
                | _ -> query.WhereNotIn

            match m.Arguments[0], m.Arguments[1] with
            // Column is IN / NOT IN a subquery of values
            | Property p, MethodCall subqueryExpr when subqueryExpr.Method.Name = nameof subqueryMany ->
                let subqueryConst = match subqueryExpr.Arguments[0] with | Constant c -> c | _ -> notImpl()
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
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
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                filter(fqCol, queryParameters)
            // Column is IN / NOT IN an array of values
            | Property p, ArrayInit values -> 
                let queryParameters = 
                    values 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                filter(fqCol, queryParameters)
            // Column is IN / NOT IN an IEnumerable of values
            | Property p, Value value -> 
                let queryParameters = 
                    (value :?> System.Collections.IEnumerable) 
                    |> Seq.cast<obj> 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                filter(fqCol, queryParameters)
            // Column is IN / NOT IN a sequence expression of values
            | Property p, MethodCall c when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()

        // like / notLike fns
        | MethodCall m when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match m.Arguments.[0], m.Arguments.[1] with
            | Property p, Value value -> 
                let pattern = string value
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> query.WhereLike(fqCol, pattern, false)
                | _ -> query.WhereNotLike(fqCol, pattern, false)
            | _ -> notImpl()

        // isNull / isNotNull
        | MethodCall m when List.contains m.Method.Name [ nameof isNullValue; "IsNull"; nameof isNotNullValue ] ->
            match m.Arguments.[0] with
            | Property p -> 
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if m.Method.Name = nameof isNullValue || m.Method.Name = "IsNull" // CompiledName for `isNull` = `IsNull`
                then query.WhereNull(fqCol)
                else query.WhereNotNull(fqCol)
            | _ -> notImpl()

        // areEqual / notEqual
        | MethodCall m when List.contains m.Method.Name [ nameof areEqual; nameof notEqual ] ->
            match m.Arguments.[0], m.Arguments.[1] with
            | Property p1, Property p2 -> 
                let alias1 = visitAlias p1.Expression
                let fqCol1 = qualifyColumn alias1 p1.Member
                let alias2 = visitAlias p2.Expression
                let fqCol2 = qualifyColumn alias2 p2.Member
                let comparison = if m.Method.Name = nameof areEqual then "=" else "<>"
                query.WhereColumns(fqCol1, comparison, fqCol2)
            | Property p, Value value | Value value, Property p ->
                let alias1 = visitAlias p.Expression
                let fqCol1 = qualifyColumn alias1 p.Member
                let queryParameter = KataUtils.getQueryParameterForValue p.Member value
                let comparison = if m.Method.Name = nameof areEqual then "=" else "<>"
                query.Where(fqCol1, comparison, queryParameter)
            | Property p, Value null | Value null, Property p ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if m.Method.Name = nameof areEqual
                then query.WhereNull(fqCol) 
                else query.WhereNotNull(fqCol)
            | _ -> notImpl()
        
        // Nullable / Option .HasValue / .IsSome `where user.HasValue`; `where user.IsSome`
        | BoolMember (Property p) when p.Member.DeclaringType |> isOptionOrNullableType && List.contains p.Member.Name ["HasValue"; "IsSome"] -> 
            let alias = visitAlias p.Expression
            let m = tryGetMember p.Expression
            let fqCol = qualifyColumn alias m.Value.Member
            query.WhereNotNull(fqCol)

        // Negated Nullable / Option .HasValue/ .IsSome `where (not user.HasValue)`; `where (not user.IsSome)`
        | Not (BoolMember (Property p)) when p.Member.DeclaringType |> isOptionOrNullableType && List.contains p.Member.Name ["HasValue"; "IsSome"] -> // `where (not user.HasValue)`; `where (not user.IsSome)`
            let alias = visitAlias p.Expression
            let m = tryGetMember p.Expression
            let fqCol = qualifyColumn alias m.Value.Member
            query.WhereNull(fqCol)

        // Option.IsNone `where user.IsNone`
        | BoolMember (Property p) when p.Member.DeclaringType |> isOptionType && p.Member.Name = "IsNone" -> 
            let alias = visitAlias p.Expression
            let m = tryGetMember p.Expression
            let fqCol = qualifyColumn alias m.Value.Member
            query.WhereNull(fqCol)

        // Negated Option.IsNone `where (not user.IsNone)`
        | Not (BoolMember (Property p)) when p.Member.DeclaringType |> isOptionType && p.Member.Name = "IsNone" -> 
            let alias = visitAlias p.Expression
            let m = tryGetMember p.Expression
            let fqCol = qualifyColumn alias m.Value.Member
            query.WhereNotNull(fqCol)

        // Bool property `where user.IsEnabled`; `where (user.IsEnabled.Value)`
        | BoolMember (Property m) ->
            let alias = visitAlias m.Expression
            let fqCol = qualifyColumn alias m.Member
            query.Where(fqCol, "=", true)

        | Not (BoolMember (Property m)) -> // `where (not user.IsEnabled)`; `where (not user.IsEnabled.Value); NOTE: This must exist before `Not` handler.
            let alias = visitAlias m.Expression
            let fqCol = qualifyColumn alias m.Member
            query.Where(fqCol, "=", false)
        | Not operand ->
            let operand = visit operand (Query())
            query.WhereNot(fun q -> operand)
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
            
            // Handle property to subquery comparisons
            | Property p1, MethodCall subqueryExpr when subqueryExpr.Method.Name = nameof subqueryOne ->
                let comparison = getComparison exp.NodeType
                let subqueryConst = match subqueryExpr.Arguments.[0] with | Constant c -> c | _ -> notImpl()
                let selectSubquery = subqueryConst.Value :?> SelectQuery
                let alias = visitAlias p1.Expression
                let fqCol = qualifyColumn alias p1.Member
                query.Where(fqCol, comparison, selectSubquery.ToKataQuery())
            
            // Handle col to col comparisons
            | Property p1, Property p2 ->
                let lt = 
                    let alias = visitAlias p1.Expression
                    qualifyColumn alias p1.Member
                let comparison = getComparison exp.NodeType
                let rt = 
                    let alias = visitAlias p2.Expression
                    qualifyColumn alias p2.Member
                query.WhereColumns(lt, comparison, rt)

            // Column = null comparisons
            | Property p, Value null | Value null, Property p when exp.NodeType = ExpressionType.Equal ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                query.WhereNull(fqCol)
            
            // Column <> null comparisons
            | Property p, Value null | Value null, Property p when exp.NodeType = ExpressionType.NotEqual ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                query.WhereNotNull(fqCol)
            
            // Nullable.HasValue null check
            | Property p, Value value | Value value, Property p when p.Member.DeclaringType |> isNullableType && p.Member.Name = "HasValue" && exp.NodeType = ExpressionType.Equal ->
                let alias = visitAlias p.Expression
                let m = tryGetMember p.Expression
                let fqCol = qualifyColumn alias m.Value.Member
                match value :?> bool with
                | true -> query.WhereNotNull(fqCol)
                | false -> query.WhereNull(fqCol)            
            // Nullable.HasValue null check
            | Property p, Value value | Value value, Property p when p.Member.DeclaringType |> isNullableType && p.Member.Name = "HasValue" && exp.NodeType = ExpressionType.NotEqual ->
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                match value :?> bool with
                | true -> query.WhereNull(fqCol)
                | false -> query.WhereNotNull(fqCol)

            // Nullable.Value comparisons
            | Property p, Value value | Value value, Property p when p.Member.DeclaringType |> isNullableType && p.Member.Name = "Value" ->
                let comparison = getComparison exp.NodeType
                let queryParameter = KataUtils.getQueryParameterForValue p.Member value
                let alias = visitAlias p.Expression
                let m = tryGetMember p.Expression
                let fqCol = qualifyColumn alias m.Value.Member
                query.Where(fqCol, comparison, queryParameter)

            // Column to value comparisons
            | Property p, Value value ->
                let comparison = getComparison(exp.NodeType)
                let queryParameter = KataUtils.getQueryParameterForValue p.Member value
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                query.Where(fqCol, comparison, queryParameter)
            
            // Value to column comparisons
            | Value value, Property p ->
                let comparison = getReverseComparison(exp.NodeType)
                let queryParameter = KataUtils.getQueryParameterForValue p.Member value
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                query.Where(fqCol, comparison, queryParameter)
            | Value v1, Value v2 ->
                notImplMsg("Value to value comparisons are not currently supported. Ex: where (1 = 1)")
            | _ ->
                notImpl()
        | _ ->
            notImpl()

    visit (filter :> Expression) (Query())

let visitHaving<'T> (filter: Expression<Func<'T, bool>>) (qualifyColumn: string -> MemberInfo -> string) =
    let rec visit (exp: Expression) (query: Query) : Query =
        match exp with
        | Lambda x -> visit x.Body query
        | Not operand -> 
            let operand = visit operand (Query())
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
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
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

                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                filter(fqCol, queryParameters)
            // Column is IN / NOT IN an array of values
            | Property p, ArrayInit values -> 
                let queryParameters = 
                    values 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray

                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                filter(fqCol, queryParameters)
            // Column is IN / NOT IN an IEnumerable of values
            | Property p, Value value -> 
                let queryParameters = 
                    (value :?> System.Collections.IEnumerable) 
                    |> Seq.cast<obj> 
                    |> Seq.map (KataUtils.getQueryParameterForValue p.Member)
                    |> Seq.toArray

                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                filter(fqCol, queryParameters)
            // Column is IN / NOT IN a sequence expression of values
            | Property p, MethodCall c when c.Method.Name = "CreateSequence" ->
                notImplMsg "Unable to unwrap sequence expression. Please use a list or array instead."
            | _ -> notImpl()
        | MethodCall m when List.contains m.Method.Name [ nameof like; nameof notLike; nameof op_EqualsPercent; nameof op_LessGreaterPercent ] ->
            match m.Arguments.[0], m.Arguments.[1] with
            | Property p, Value value -> 
                let pattern = string value
                match m.Method.Name with
                | nameof like | nameof op_EqualsPercent -> 
                    let alias = visitAlias p.Expression
                    let fqCol = qualifyColumn alias p.Member
                    query.HavingLike(fqCol, pattern, false)
                | _ -> 
                    let alias = visitAlias p.Expression
                    let fqCol = qualifyColumn alias p.Member
                    query.HavingNotLike(fqCol, pattern, false)
            | _ -> notImpl()
        | MethodCall m when m.Method.Name = nameof isNullValue || m.Method.Name = nameof isNotNullValue ->
            match m.Arguments.[0] with
            | Property p -> 
                let alias = visitAlias p.Expression
                let fqCol = qualifyColumn alias p.Member
                if m.Method.Name = nameof isNullValue
                then query.HavingNull(fqCol)
                else query.HavingNotNull(fqCol)
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
                let alias = visitAlias p1.Expression
                let fqCol = qualifyColumn alias p1.Member
                query.Having(fqCol, comparison, selectSubquery.ToKataQuery())
            | AggregateColumn (aggType, p1), Property p2 ->
                // Handle aggregate col to col comparisons
                let lt = 
                    let alias = visitAlias p1.Expression
                    qualifyColumn alias p1.Member
                let comparison = getComparison exp.NodeType
                let rt = 
                    let alias = visitAlias p2.Expression
                    qualifyColumn alias p2.Member
                query.HavingRaw($"{aggType}({lt}) {comparison} {rt}")
            | AggregateColumn (aggType, p), Value value ->
                // Handle aggregate column to value comparisons
                let alias = visitAlias p.Expression
                let lt = qualifyColumn alias p.Member
                let comparison = getComparison(exp.NodeType)
                query.HavingRaw($"{aggType}({lt}) {comparison} ?", [value])
            | Property p1, Property p2 ->
                // Handle col to col comparisons
                let lt = 
                    let alias = visitAlias p1.Expression
                    qualifyColumn alias p1.Member
                let comparison = getComparison exp.NodeType
                let rt = 
                    let alias = visitAlias p2.Expression
                    qualifyColumn alias p2.Member
                query.HavingColumns(lt, comparison, rt)
            | Property p, Value value ->
                // Handle column to value comparisons
                match exp.NodeType, value with
                | ExpressionType.Equal, null -> 
                    let alias = visitAlias p.Expression
                    query.WhereNull(qualifyColumn alias p.Member)
                | ExpressionType.NotEqual, null -> 
                    let alias = visitAlias p.Expression
                    query.WhereNotNull(qualifyColumn alias p.Member)
                | _ ->                     
                    let comparison = getComparison(exp.NodeType)
                    let queryParameter = KataUtils.getQueryParameterForValue p.Member value
                    let alias = visitAlias p.Expression
                    query.Where(qualifyColumn alias p.Member, comparison, queryParameter)
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
let visitPropertiesSelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) (qualifyColumn: string -> MemberInfo -> string) =
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
            let alias = visitAlias m.Expression
            let column = qualifyColumn alias m.Member
            [column]
        | _ -> notImpl()

    visit (propertySelector :> Expression)

type OrderBy =
    | OrderByColumn of tableAlias: string * MemberInfo
    | OrderByAggregateColumn of aggregateType: string * tableAlias: string * MemberInfo

/// Returns a column MemberInfo.
let visitOrderByPropertySelector<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : OrderBy =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | AggregateColumn (aggType, p) -> 
            let alias = visitAlias p.Expression
            OrderByAggregateColumn (aggType, alias, p.Member)
        | Member m -> 
            if m.Member.DeclaringType |> isOptionOrNullableType then 
                visit m.Expression
            else 
                let alias = visitAlias m.Expression
                OrderByColumn (alias, m.Member)
        | Property p -> 
            let alias = visitAlias p.Expression
            OrderByColumn (alias, p.Member)
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
            let alias = visitAlias m.Expression
            if m.Member.DeclaringType |> isOptionOrNullableType
            then visit m.Expression
            else [ { Alias = alias; Member = m.Member } ]
        | Property p -> 
            let alias = visitAlias p.Expression
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
            if m.Member.DeclaringType |> isOptionOrNullableType
            then visit m.Expression
            else m.Member
        | Property p -> p.Member
        | _ -> notImpl()

    visit (propertySelector :> Expression)

type Selection =
    | SelectedTable of tableAlias: string * tableType: Type
    | SelectedColumn of tableAlias: string * column: string * columnType: Type * isOpt: bool * isNullable: bool
    | SelectedAggregateColumn of aggregateType: string * tableAlias: string * column: string

/// Returns a list of one or more fully qualified table names: ["{schema}.{table}"]
let visitSelect<'T, 'Prop> (propertySelector: Expression<Func<'T, 'Prop>>) =
    let rec visit (exp: Expression) : Selection list =
        match exp with
        | Lambda x -> visit x.Body
        | MethodCall m when m.Method.Name = "Invoke" ->
            // Handle tuples
            visit m.Object
        | MethodCall m when m.Method.Name = "Some" ->
            // Columns selected from leftJoined tables may be wrapped in `Some` to make them optional.
            visit m.Arguments.[0]
        | AggregateColumn (aggType, p) -> 
            let alias = visitAlias p.Expression
            [ SelectedAggregateColumn (aggType, alias, p.Member.Name) ]            
        | New n -> 
            // Handle a tuple of multiple tables
            n.Arguments 
            |> Seq.map visit |> Seq.toList |> List.concat
        | Parameter p -> 
            [ SelectedTable (p.Name, p.Type) ]
        | Member m -> 
            if m.Member.DeclaringType |> isOptionOrNullableType then 
                visit m.Expression
            else 
                let isOptional, isNullable =
                    if m.Type.IsGenericType && m.Type.GetGenericTypeDefinition() = typedefof<Option<_>> then true, false
                    elif m.Type.IsGenericType && m.Type.GetGenericTypeDefinition() = typedefof<Nullable<_>> then false, true
                    else false, false

                let alias = visitAlias m.Expression
                [ SelectedColumn (alias, m.Member.Name, m.Type, isOptional, isNullable) ]
        | _ -> 
            notImpl()

    visit (propertySelector :> Expression)
