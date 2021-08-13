/// LINQ builders for SqlKata.Query
[<AutoOpen>]
module SqlHydra.Query.KataBuilders

open System
open System.Collections.Generic
open System.Linq.Expressions
open SqlKata

/// Represents a typed SqlKata query.
type TypedQuery<'T>(query: SqlKata.Query) =
    member this.Query = query

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
    | Some schema -> $"{schema}.{tbl.Name}"
    | None -> tbl.Name

type QuerySource<'T>(tableMappings) =
    interface IEnumerable<'T> with
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator() :> Collections.IEnumerator
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator()
    
    member this.TableMappings : Map<FQName, TableMapping> = tableMappings
    
    member this.GetOuterTableMapping() = 
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

type SelectExpressionBuilder<'Output>() =

    let getQueryOrDefault (state: QuerySource<'Result>) = // 'Result allows 'T to vary as the result of joins
        match state with
        | :? QuerySource<'Result, Query> as qs -> qs.Query
        | _ -> Query()            

    let mergeTableMappings (a: Map<FQName, TableMapping>, b: Map<FQName, TableMapping>) =
        Map (Seq.concat [ (Map.toSeq a); (Map.toSeq b) ])

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let tbl = state.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(
            query.From(match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name), 
            state.TableMappings)

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
        let selectedTypes = LinqExpressionVisitors.visitSelect<'T,'Transform> selectExpression

        let selections = 
            selectedTypes
            |> List.map (function
                | LinqExpressionVisitors.SelectedTable t -> $"%s{fullyQualifyTable state.TableMappings t}.*"
                | LinqExpressionVisitors.SelectedColumn c -> fullyQualifyColumn state.TableMappings c
            )
            |> List.toArray
                  
        QuerySource<'Transform, Query>(query.Select(selections), state.TableMappings)

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

    /// Sets the SKIP and TAKE value for query
    [<CustomOperation("skipTake", MaintainsVariableSpace = true)>]
    member this.SkipTake (state:QuerySource<'T>, skip, take) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(query.Skip(skip).Take(take), state.TableMappings)

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

        let outerQuery = outerSource |> getQueryOrDefault        
        QuerySource<'Result, Query>(outerQuery.Join(innerTableName, innerPropertyName, outerPropertyName), mergedTables)

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

        let outerQuery = outerSource |> getQueryOrDefault
        QuerySource<'Result, Query>(outerQuery.LeftJoin(innerTableName, innerPropertyName, outerPropertyName), mergedTables)

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

    ///// COUNT aggregate function for the selected column
    //[<CustomOperation("countBy", MaintainsVariableSpace = true)>]
    //member this.CountBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
    //    let query = state |> getQueryOrDefault
    //    let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
    //    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Count(propertyName, propertyName)] }, state.TableMappings)

    ///// AVG aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation("avg", MaintainsVariableSpace = true)>]
    //member this.Avg (state:QuerySource<'T>, colName, alias) = 
    //    let query = state |> getQueryOrDefault
    //    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Avg(colName, alias)] }, state.TableMappings)

    ///// AVG aggregate function for the selected column
    ////[<CustomOperation("avgBy", MaintainsVariableSpace = true)>]
    ////member this.AvgBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
    ////    let query = state |> getQueryOrDefault
    ////    let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
    ////    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Avg(propertyName, propertyName)] }, state.TableMappings)
    
    ///// SUM aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation("sum", MaintainsVariableSpace = true)>]
    //member this.Sum (state:QuerySource<'T>, colName, alias) = 
    //    let query = state |> getQueryOrDefault
    //    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Sum(colName, alias)] }, state.TableMappings)

    ///// SUM aggregate function for the selected column
    ////[<CustomOperation("sumBy", MaintainsVariableSpace = true)>]
    ////member this.SumBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
    ////    let query = state |> getQueryOrDefault
    ////    let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
    ////    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Sum(propertyName, propertyName)] }, state.TableMappings)
    
    ///// MIN aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation("min", MaintainsVariableSpace = true)>]
    //member this.Min (state:QuerySource<'T>, colName, alias) = 
    //    let query = state |> getQueryOrDefault
    //    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Min(colName, alias)] }, state.TableMappings)

    ///// MIN aggregate function for the selected column
    ////[<CustomOperation("minBy", MaintainsVariableSpace = true)>]
    ////member this.MinBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
    ////    let query = state |> getQueryOrDefault
    ////    let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
    ////    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Min(propertyName, propertyName)] }, state.TableMappings)
    
    ///// MIN aggregate function for COLNAME (or * symbol) and map it to ALIAS
    //[<CustomOperation("max", MaintainsVariableSpace = true)>]
    //member this.Max (state:QuerySource<'T>, colName, alias) = 
    //    let query = state |> getQueryOrDefault
    //    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Max(colName, alias)] }, state.TableMappings)

    ///// MIN aggregate function for the selected column
    ////[<CustomOperation("maxBy", MaintainsVariableSpace = true)>]
    ////member this.MaxBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
    ////    let query = state |> getQueryOrDefault
    ////    let propertyName = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector |> fullyQualifyColumn state.TableMappings
    ////    QuerySource<'T, Query>({ query with Aggregates = query.Aggregates @ [Aggregate.Max(propertyName, propertyName)] }, state.TableMappings)
    
    /// Sets query to return DISTINCT values
    [<CustomOperation("distinct", MaintainsVariableSpace = true)>]
    member this.Distinct (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault        
        QuerySource<'T, Query>(query.Distinct(), state.TableMappings)

    /// Unwraps the query
    member this.Run (state: QuerySource<'T>) =
        let query = state |> getQueryOrDefault
        TypedQuery<'T>(query)

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
        let query  = state |> getQueryOrDefault
        TypedQuery<'T>(query.AsDelete())

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

/// WHERE column is IN values
let isIn<'P> (prop: 'P) (values: 'P list) = true
/// WHERE column is IN values
let inline (|=|) (prop: 'P) (values: 'P list) = true

/// WHERE column is NOT IN values
let isNotIn<'P> (prop: 'P) (values: 'P list) = true
/// WHERE column is NOT IN values
let inline (|<>|) (prop: 'P) (values: 'P list) = true

/// WHERE column like value   
let like<'P> (prop: 'P) (pattern: string) = true
/// WHERE column like value   
let inline (=%) (prop: 'P) (pattern: string) = true

/// WHERE column not like value   
let notLike<'P> (prop: 'P) (pattern: string) = true
/// WHERE column not like value   
let inline (<>%) (prop: 'P) (pattern: string) = true

/// WHERE column IS NULL
let isNullValue<'P> (prop: 'P) = true
/// WHERE column IS NOT NULL
let isNotNullValue<'P> (prop: 'P) = true