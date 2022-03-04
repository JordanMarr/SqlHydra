/// LINQ builders for SqlKata.Query
[<AutoOpen>]
module SqlHydra.Query.KataBuilders

open System
open System.Linq.Expressions
open SqlKata

type DeleteExpressionBuilder<'Deleted>() =

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
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.Where(fun w -> where), state.TableMappings)

    /// Deletes all records in the table (only when there are is no where clause)
    [<CustomOperation("deleteAll", MaintainsVariableSpace = true)>]
    member this.DeleteAll (state:QuerySource<'T>) = 
        state :?> QuerySource<'T, Query>

    /// Unwraps the query
    member this.Run (state: QuerySource<'Deleted>) =
        let query = state |> getQueryOrDefault
        DeleteQuery<'Deleted>(query.AsDelete())

type InsertExpressionBuilder<'Inserted, 'InsertReturn when 'InsertReturn : struct>() =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, InsertQuerySpec<'T, 'IdentityReturn>> as qs -> qs.Query
        | _ -> InsertQuerySpec.Default

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let tbl = state.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { query with Table = match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name }
            , state.TableMappings)

    /// Sets the TABLE name for query.
    [<CustomOperation("into")>]
    member this.Into (state: QuerySource<'T>, table: QuerySource<'T>) =
        let tbl = table.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { query with Table = match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name }
            , state.TableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets a single value for INSERT
    [<CustomOperation("entity", MaintainsVariableSpace = true)>]
    member this.Entity (state:QuerySource<'T>, value: 'T) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { query with Entities = [ value ] }
            , state.TableMappings)

    /// Sets multiple values for INSERT
    [<CustomOperation("entities", MaintainsVariableSpace = true)>]
    member this.Entities (state:QuerySource<'T>, entities: AtLeastOne.AtLeastOne<'T>) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { query with Entities = entities |> AtLeastOne.getSeq |> Seq.toList }
            , state.TableMappings)

    /// Includes a column in the insert query.
    [<CustomOperation("includeColumn", MaintainsVariableSpace = true)>]
    member this.IncludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let prop = (propertySelector |> LinqExpressionVisitors.visitPropertySelector<'T, 'Prop>).Name
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>({ query with Fields = query.Fields @ [ prop ] }, state.TableMappings)

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
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newQuery, state.TableMappings)
    
    /// Sets the identity field that should be returned from the insert and excludes it from the insert columns.
    [<CustomOperation("getId", MaintainsVariableSpace = true)>]
    member this.GetId (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        // Exclude the identity column from the query
        let state = this.ExcludeColumn(state, propertySelector)
        
        // Set the identity property and the 'InsertReturn type
        let spec = state.Query
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'InsertReturn> propertySelector :?> Reflection.PropertyInfo
        let identitySpec = { Table = spec.Table; Entities = spec.Entities; Fields = spec.Fields; IdentityField = Some prop.Name }
        
        // Sets both the identity field name (prop.Name) and its type ('InsertReturn)
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(identitySpec, state.TableMappings)

    member this.Run (state: QuerySource<'Inserted>) =
        let spec = getQueryOrDefault state
        InsertQuery<'Inserted, 'InsertReturn>(spec)

type UpdateExpressionBuilder<'Updated>() =
    
    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, UpdateQuerySpec<'T>> as qs -> qs.Query
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
        
        let value = KataUtils.getQueryParameterForValue prop value :> obj
        QuerySource<'T, UpdateQuerySpec<'T>>(
            { query with SetValues = query.SetValues @ [ prop.Name, value ] }
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
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, UpdateQuerySpec<'T>>({ query with Where = Some where }, state.TableMappings)

    /// A safeguard that verifies that all records in the table should be updated.
    [<CustomOperation("updateAll", MaintainsVariableSpace = true)>]
    member this.UpdateAll (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, UpdateQuerySpec<'T>>({ query with UpdateAll = true }, state.TableMappings)

    /// Unwraps the query
    member this.Run (state: QuerySource<'Updated>) =
        let spec = state |> getQueryOrDefault
        if spec.Where = None && spec.UpdateAll = false
        then failwith "An `update` expression must either contain a `where` clause or `updateAll`."
        UpdateQuery<'Updated>(spec)
        

let delete<'Deleted> = DeleteExpressionBuilder<'Deleted>()
let insert<'Inserted, 'InsertReturn when 'InsertReturn : struct> = InsertExpressionBuilder<'Inserted, 'InsertReturn>()
let update<'Updated> = UpdateExpressionBuilder<'Updated>()
