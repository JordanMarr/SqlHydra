/// LINQ builders for SqlKata.Query
[<AutoOpen>]
module SqlHydra.Query.KataBuilders

open System
open System.Collections.Generic
open System.Linq.Expressions
open SqlKata

[<AutoOpen>]
module FQ = 
    /// Fully qualified entity type name
    type [<Struct>] FQName = private FQName of string
    let fqName (t: Type) = FQName t.FullName

type TableMapping = { Name: string; Schema: string option }

/// Fully qualifies a column with: {?schema}.{table}.{column}
let private fullyQualifyColumn (tables: Map<FQName, TableMapping>) (property: Reflection.MemberInfo) =
    let tbl = tables.[fqName property.DeclaringType]
    match tbl.Schema with
    | Some schema -> $"%s{schema}.%s{tbl.Name}.%s{property.Name}"
    | None -> $"%s{tbl.Name}.%s{property.Name}"

/// Tries to find a table mapping for a given table record type. 
let private fullyQualifyTable (tables: Map<FQName, TableMapping>) (tableRecord: Type) =
    let tbl = tables.[fqName tableRecord]
    match tbl.Schema with
    | Some schema -> $"%s{schema}.%s{tbl.Name}"
    | None -> tbl.Name

type QuerySource<'T>(tableMappings) =
    interface IEnumerable<'T> with
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator() :> Collections.IEnumerator
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator()
    
    member internal this.TableMappings : Map<FQName, TableMapping> = tableMappings
    
    member internal this.GetOuterTableMapping() = 
        let outerEntity = typeof<'T>
        let fqn = 
            if outerEntity.Name.StartsWith "Tuple" // True for joined tables
            then outerEntity.GetGenericArguments() |> Array.head |> fqName
            else outerEntity |> fqName
        this.TableMappings.[fqn]

type QuerySource<'T, 'Query>(query, tableMappings) = 
    inherit QuerySource<'T>(tableMappings)
    member this.Query : 'Query = query

[<AutoOpen>]
module Table = 

    /// Maps the entity 'T to a table of the exact same name.
    let table<'T> = 
        let ent = typeof<'T>
        let tables = Map [fqName ent, { Name = ent.Name; Schema = None }]
        QuerySource<'T>(tables)

    /// Maps the entity 'T to a table of the given name.
    let table'<'T> (tableName: string) = 
        let ent = typeof<'T>
        let tables = Map [fqName ent, { Name = tableName; Schema = None }]
        QuerySource<'T>(tables)

    /// Maps the entity 'T to a schema of the given name.
    let inSchema<'T> (schemaName: string) (qs: QuerySource<'T>) =
        let ent = typeof<'T>
        let fqn = fqName ent
        let tbl = qs.TableMappings.[fqn]
        let tables = qs.TableMappings.Add(fqn, { tbl with Schema = Some schemaName })
        QuerySource<'T>(tables)

let internal (|FromTable|FromJoinedTable|FromSubquery|FromJoinedSubquery|) (state: QuerySource<'T>) =
    match state with
    | :? QuerySource<'T, Query> as qs when qs.Query.Clauses |> Seq.exists (fun c -> c :? FromClause) -> FromSubquery qs
    | :? QuerySource<'T, Query> as qs when qs.Query.Clauses |> Seq.exists (fun c -> c :? QueryFromClause) -> FromJoinedSubquery qs
    | :? QuerySource<'T, Query> as qs -> FromJoinedTable qs
    | qs -> FromTable qs

type SelectExpressionBuilder<'Output>() =

    let getQueryOrDefault (state: QuerySource<'Result>) = // 'Result allows 'T to vary as the result of joins
        match state with
        | :? QuerySource<'Result, Query> as qs -> qs.Query
        | _ -> Query()            

    let mergeTableMappings (a: Map<FQName, TableMapping>, b: Map<FQName, TableMapping>) =
        Map (Seq.concat [ (Map.toSeq a); (Map.toSeq b) ])

    // NOTE: if joins exist, they will be called before `For` is called.
    /// FROM {table} or {subquery}
    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let outerTbl = state.GetOuterTableMapping()
        let tableName = match outerTbl.Schema with Some schema -> $"{schema}.{outerTbl.Name}" | None -> outerTbl.Name
        
        match state with
        // No joins exist -- only a FROM subquery
        | FromSubquery qs -> QuerySource<'T, Query>(Query().From(qs.Query), qs.TableMappings)
        
        // No joins exist -- only a `table<'T>`
        | FromTable qs -> QuerySource<'T, Query>(Query().From(tableName), qs.TableMappings)

        // Joins exist and have already been called. Set query FROM to outer (first / left-most) table.
        | FromJoinedTable qs -> QuerySource<'T, Query>(qs.Query.From(tableName), qs.TableMappings)
        
        // Joins exist and have already been called. Query FROM should have already been set to the subquery in the join/leftJoin.
        | FromJoinedSubquery qs -> qs

    /// INNER JOIN table where COLNAME equals to another COLUMN (including TABLE name)
    [<CustomOperation("join", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
    member this.Join (outerSource: QuerySource<'TOuter>, 
                      innerSource: QuerySource<'TInner>, 
                      outerKeySelector: Expression<Func<'TOuter,'Key>>, 
                      innerKeySelector: Expression<Func<'TInner,'Key>>, 
                      resultSelector: Expression<Func<'TOuter,'TInner,'Result>> ) = 

        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerSource.TableMappings)
        let outerPropertyName = LinqExpressionVisitors.visitPropertySelector<'TOuter, 'Key> outerKeySelector |> fullyQualifyColumn mergedTables
    
        let innerProperty = LinqExpressionVisitors.visitPropertySelector<'TInner, 'Key> innerKeySelector 
        let innerPropertyName = innerProperty |> fullyQualifyColumn mergedTables
        let innerTableName = 
            let tbl = mergedTables.[fqName innerProperty.DeclaringType]
            match tbl.Schema with
            | Some schema -> sprintf "%s.%s" schema tbl.Name
            | None -> tbl.Name

        match outerSource with
        | FromSubquery qs -> QuerySource<'Result, Query>(Query().From(qs.Query).Join(innerTableName, innerPropertyName, outerPropertyName), qs.TableMappings)
        | FromJoinedSubquery _ -> failwith "Multipled joined FROM subqueries are not yet supported."
        | FromTable qs -> QuerySource<'Result, Query>(Query().Join(innerTableName, innerPropertyName, outerPropertyName), mergedTables)
        | FromJoinedTable qs -> QuerySource<'Result, Query>(qs.Query.Join(innerTableName, innerPropertyName, outerPropertyName), mergedTables)

    /// LEFT JOIN table where COLNAME equals to another COLUMN (including TABLE name)
    [<CustomOperation("leftJoin", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
    member this.LeftJoin (outerSource: QuerySource<'TOuter>, 
                          innerSource: QuerySource<'TInner>, 
                          outerKeySelector: Expression<Func<'TOuter,'Key>>, 
                          innerKeySelector: Expression<Func<'TInner option,'Key>>, 
                          resultSelector: Expression<Func<'TOuter,'TInner option,'Result>> ) = 

        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerSource.TableMappings)
        let outerPropertyName = LinqExpressionVisitors.visitPropertySelector<'TOuter, 'Key> outerKeySelector |> fullyQualifyColumn mergedTables
    
        let innerProperty = LinqExpressionVisitors.visitPropertySelector<'TInner option, 'Key> innerKeySelector
        let innerPropertyName = innerProperty |> fullyQualifyColumn mergedTables
        let innerTableName = 
            let tbl = mergedTables.[fqName innerProperty.DeclaringType]
            match tbl.Schema with
            | Some schema -> sprintf "%s.%s" schema tbl.Name
            | None -> tbl.Name

        match outerSource with
        | FromSubquery qs -> QuerySource<'Result, Query>(Query().From(qs.Query).LeftJoin(innerTableName, innerPropertyName, outerPropertyName), qs.TableMappings)
        | FromJoinedSubquery _ -> failwith "Multipled joined FROM subqueries are not yet supported."
        | FromTable qs -> QuerySource<'Result, Query>(Query().LeftJoin(innerTableName, innerPropertyName, outerPropertyName), mergedTables)
        | FromJoinedTable qs -> QuerySource<'Result, Query>(qs.Query.LeftJoin(innerTableName, innerPropertyName, outerPropertyName), mergedTables)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    // Prevents errors while typing join statement if rest of query is not filled in yet.
    member this.Zero _ = 
        QuerySource<'T>(Map.empty)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state:QuerySource<'T>, [<ProjectionParameter>] whereExpression) = 
        let query = state |> getQueryOrDefault
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.Where(fun w -> where), state.TableMappings)

    /// Sets the SELECT statement and filters the query to include only the selected tables
    [<CustomOperation("select", MaintainsVariableSpace = true)>]
    member this.Select (state: QuerySource<'T>, [<ProjectionParameter>] selectExpression: Expression<Func<'T, 'Transform>>) =
        let query = state |> getQueryOrDefault

        // User should select one or more table records
        let selections = LinqExpressionVisitors.visitSelect<'T,'Transform> selectExpression

        let queryWithSelectedColumns =
            selections
            |> List.fold (fun (q: Query) -> function
                | LinqExpressionVisitors.SelectedTable t -> q.Select($"%s{fullyQualifyTable state.TableMappings t}.*")
                | LinqExpressionVisitors.SelectedColumn c -> q.Select(fullyQualifyColumn state.TableMappings c)
                | LinqExpressionVisitors.AggregateColumn (aggType, c) -> q.SelectRaw($"{aggType}({fullyQualifyColumn state.TableMappings c})")
            ) query

        QuerySource<'Transform, Query>(queryWithSelectedColumns, state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("orderBy", MaintainsVariableSpace = true)>]
    member this.OrderBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
        QuerySource<'T, Query>(query.OrderBy(propertyName), state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("thenBy", MaintainsVariableSpace = true)>]
    member this.ThenBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
        QuerySource<'T, Query>(query.OrderBy(propertyName), state.TableMappings)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("orderByDescending", MaintainsVariableSpace = true)>]
    member this.OrderByDescending (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
        QuerySource<'T, Query>(query.OrderByDesc(propertyName), state.TableMappings)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("thenByDescending", MaintainsVariableSpace = true)>]
    member this.ThenByDescending (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
        QuerySource<'T, Query>(query.OrderByDesc(propertyName), state.TableMappings)

    /// Sets the SKIP value for query
    [<CustomOperation("skip", MaintainsVariableSpace = true)>]
    member this.Skip (state:QuerySource<'T>, skip) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(query.Skip(skip), state.TableMappings)
    
    /// Sets the TAKE value for query
    [<CustomOperation("take", MaintainsVariableSpace = true)>]
    member this.Take (state:QuerySource<'T>, take) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(query.Take(take), state.TableMappings)

    /// Sets the GROUP BY for one or more columns.
    [<CustomOperation("groupBy", MaintainsVariableSpace = true)>]
    member this.GroupBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let properties = LinqExpressionVisitors.visitGroupBy<'T, 'Prop> propertySelector (fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.GroupBy(properties |> List.toArray), state.TableMappings)

    /// COUNT aggregate function
    [<CustomOperation("count", MaintainsVariableSpace = true)>]
    member this.Count (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault
        QuerySource<int, Query>(query.AsCount(), state.TableMappings)

    /// Sets query to return DISTINCT values
    [<CustomOperation("distinct", MaintainsVariableSpace = true)>]
    member this.Distinct (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault        
        QuerySource<'T, Query>(query.Distinct(), state.TableMappings)

    /// Unwraps the SqlKata query
    member this.Run (state: QuerySource<'T>) =
        state :?> QuerySource<'T, SqlKata.Query>

type DeleteExpressionBuilder<'T>() =

    let getQueryOrDefault (state: QuerySource<'Result>) =
        match state with
        | :? QuerySource<'Result, Query> as qs -> qs.Query
        | _ -> Query()            

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let tbl = state.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(
            query.From(match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name), 
            state.TableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state:QuerySource<'T>, [<ProjectionParameter>] whereExpression) = 
        let query = state |> getQueryOrDefault
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.Where(fun w -> where), state.TableMappings)

    /// Deletes all records in the table (only when there are is no where clause)
    [<CustomOperation("deleteAll", MaintainsVariableSpace = true)>]
    member this.DeleteAll (state:QuerySource<'T>) = 
        state :?> QuerySource<'T, Query>

    /// Unwraps the query
    member this.Run (state: QuerySource<'T>) =
        state :?> QuerySource<'T, SqlKata.Query>

type InsertQuerySpec<'T> = 
    {
        Table: string
        Entity: 'T option
        Fields: string list
    }
    static member Default = { Table = ""; Entity = Option<'T>.None; Fields = [] }

type InsertExpressionBuilder<'T>() =

    let getQueryOrDefault (state: QuerySource<'Result>) =
        match state with
        | :? QuerySource<'Result, InsertQuerySpec<'T>> as qs -> qs.Query
        | _ -> InsertQuerySpec.Default

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let tbl = state.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T>>(
            { query with Table = match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name }
            , state.TableMappings)

    /// Sets the TABLE name for query.
    [<CustomOperation("into")>]
    member this.Into (state: QuerySource<'T>, table: QuerySource<'T>) =
        let tbl = table.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T>>(
            { query with Table = match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name }
            , state.TableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets the single value for INSERT
    [<CustomOperation("entity", MaintainsVariableSpace = true)>]
    member this.Entity (state:QuerySource<'T>, value: 'T) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T>>(
            { query with Entity = value |> Some}
            , state.TableMappings)

    /// Includes a column in the insert query.
    [<CustomOperation("includeColumn", MaintainsVariableSpace = true)>]
    member this.IncludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let prop = (propertySelector |> LinqExpressionVisitors.visitPropertySelector<'T, 'Prop>).Name
        QuerySource<'T, InsertQuerySpec<'T>>({ query with Fields = query.Fields @ [ prop ] }, state.TableMappings)

    /// Excludes a column from the insert query.
    [<CustomOperation("excludeColumn", MaintainsVariableSpace = true)>]
    member this.ExcludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector
        let newQuery =
            query.Fields
            |> function
                | [] -> FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) |> Array.map (fun x -> x.Name) |> Array.toList
                | fields -> fields
            |> List.filter (fun f -> f <> prop.Name)
            |> (fun x -> { query with Fields = x })
        QuerySource<'T, InsertQuerySpec<'T>>(newQuery, state.TableMappings)

    /// Unwraps the query
    member this.Run (state: QuerySource<'T>) =
        state |> getQueryOrDefault

type UpdateQuerySpec<'T> = 
    {
        Table: string
        Entity: 'T option
        Fields: string list
        SetValues: (string * obj) list
        Where: Query option
        UpdateAll: bool
    }
    static member Default = 
        { Table = ""; Entity = Option<'T>.None; Fields = []; SetValues = []; Where = None; UpdateAll = false }

type UpdateExpressionBuilder<'T>() =
    
    let getQueryOrDefault (state: QuerySource<'Result>) =
        match state with
        | :? QuerySource<'Result, UpdateQuerySpec<'T>> as qs -> qs.Query
        | _ -> UpdateQuerySpec.Default

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let tbl = state.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, UpdateQuerySpec<'T>>(
            { query with Table = match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name }
            , state.TableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets the emtore entity ('T) to be updated
    [<CustomOperation("entity", MaintainsVariableSpace = true)>]
    member this.Entity (state: QuerySource<'T>, value: 'T) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, UpdateQuerySpec<'T>>(
            { query with Entity = value |> Some}
            , state.TableMappings)

    /// Sets a property of the entity ('T) to be updated
    [<CustomOperation("set", MaintainsVariableSpace = true)>]
    member this.Set (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector: Expression<Func<'T, 'Prop>>, value: 'Prop) = 
        let query = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector :?> Reflection.PropertyInfo
        QuerySource<'T, UpdateQuerySpec<'T>>(
            { query with SetValues = query.SetValues @ [ prop.Name, box value ] }
            , state.TableMappings)

    /// Includes a column in the insert query.
    [<CustomOperation("includeColumn", MaintainsVariableSpace = true)>]
    member this.IncludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let prop = (propertySelector |> LinqExpressionVisitors.visitPropertySelector<'T, 'Prop>).Name
        QuerySource<'T, UpdateQuerySpec<'T>>({ query with Fields = query.Fields @ [ prop ] }, state.TableMappings)

    /// Excludes a column from the insert query.
    [<CustomOperation("excludeColumn", MaintainsVariableSpace = true)>]
    member this.ExcludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector
        let newQuery =
            query.Fields
            |> function
                | [] -> FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) |> Array.map (fun x -> x.Name) |> Array.toList
                | fields -> fields
            |> List.filter (fun f -> f <> prop.Name)
            |> (fun x -> { query with Fields = x })
        QuerySource<'T, UpdateQuerySpec<'T>>(newQuery, state.TableMappings)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state: QuerySource<'T>, [<ProjectionParameter>] whereExpression) = 
        let query = state |> getQueryOrDefault
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (fullyQualifyColumn state.TableMappings)
        QuerySource<'T, UpdateQuerySpec<'T>>({ query with Where = Some where }, state.TableMappings)

    /// A safeguard that verifies that all records in the table should be updated.
    [<CustomOperation("updateAll", MaintainsVariableSpace = true)>]
    member this.UpdateAll (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, UpdateQuerySpec<'T>>({ query with UpdateAll = true }, state.TableMappings)

    /// Unwraps the query
    member this.Run (state: QuerySource<'T>) =
        let query = state |> getQueryOrDefault
        if query.Where = None && query.UpdateAll = false
        then failwith "An `update` expression must either contain a `where` clause or `updateAll`."
        query

let select<'T> = SelectExpressionBuilder<'T>()
let delete<'T> = DeleteExpressionBuilder<'T>()
let insert<'T> = InsertExpressionBuilder<'T>()
let update<'T> = UpdateExpressionBuilder<'T>()
