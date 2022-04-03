/// Linq insert query builders
[<AutoOpen>]
module SqlHydra.Query.InsertBuilders

open System

/// The base insert builder that contains all common operations
type InsertBuilder<'Inserted, 'InsertReturn when 'InsertReturn : struct>() =

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
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = [ value ] }
            , state.TableMappings)

    /// Sets multiple values for INSERT
    [<CustomOperation("entities", MaintainsVariableSpace = true)>]
    member this.Entities (state:QuerySource<'T>, entities: AtLeastOne.AtLeastOne<'T>) = 
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = entities |> AtLeastOne.getSeq |> Seq.toList }
            , state.TableMappings)

    /// Includes a column in the insert query.
    [<CustomOperation("includeColumn", MaintainsVariableSpace = true)>]
    member this.IncludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let spec = state |> getQueryOrDefault
        let prop = (propertySelector |> LinqExpressionVisitors.visitPropertySelector<'T, 'Prop>).Name
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>({ spec with Fields = spec.Fields @ [ prop ] }, state.TableMappings)

    /// Excludes a column from the insert query.
    [<CustomOperation("excludeColumn", MaintainsVariableSpace = true)>]
    member this.ExcludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let spec = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector
        let newSpec =
            spec.Fields
            |> function
                | [] -> FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) |> Array.map (fun x -> x.Name) |> Array.toList
                | fields -> fields
            |> List.filter (fun f -> f <> prop.Name)
            |> (fun x -> { spec with Fields = x })
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)
    
    /// Sets the identity field that should be returned from the insert and excludes it from the insert columns.
    [<CustomOperation("getId", MaintainsVariableSpace = true)>]
    member this.GetId (state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, [<ProjectionParameter>] idProperty) = 
        // Exclude the identity column
        let spec = this.ExcludeColumn(state, idProperty).Query
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'InsertReturn> idProperty :?> Reflection.PropertyInfo
        
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>({ spec with IdentityField = Some prop.Name }, state.TableMappings)

    member this.Run (state: QuerySource<'Inserted>) =
        let spec = getQueryOrDefault state
        InsertQuery<'Inserted, 'InsertReturn>(spec)


/// An insert builder that returns a Task result.
type InsertAsyncBuilder<'Inserted, 'InsertReturn when 'InsertReturn : struct>(ct: ContextType) =
    inherit InsertBuilder<'Inserted, 'InsertReturn>()

    member this.Run (state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>) = 
        async {
            let ctx = ContextUtils.getContext ct            
            try 
                let insertQuery = InsertQuery<'Inserted, 'InsertReturn>(state.Query)
                let! insertReturn = insertQuery |> ctx.InsertAsync |> Async.AwaitTask
                return insertReturn
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }


/// An insert builder that returns an Async result.
type InsertTaskBuilder<'Inserted, 'InsertReturn when 'InsertReturn : struct>(ct: ContextType) =
    inherit InsertBuilder<'Inserted, 'InsertReturn>()

    member this.Run (state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>) = 
        async {
            let ctx = ContextUtils.getContext ct
            try 
                let insertQuery = InsertQuery<'Inserted, 'InsertReturn>(state.Query)
                let! insertReturn = insertQuery |> ctx.InsertAsync |> Async.AwaitTask
                return insertReturn
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }
        |> Async.StartImmediateAsTask


/// Builds an insert query that can be manually run by piping into QueryContext insert methods
let insert<'Inserted, 'InsertReturn when 'InsertReturn : struct> = 
    InsertBuilder<'Inserted, 'InsertReturn>()

/// Builds an insert query that returns an Async result
let insertAsync<'Inserted, 'InsertReturn when 'InsertReturn : struct> ct = 
    InsertAsyncBuilder<'Inserted, 'InsertReturn>(ct)

/// Builds an insert query that returns a Task result
let insertTask<'Inserted, 'InsertReturn when 'InsertReturn : struct> ct = 
    InsertTaskBuilder<'Inserted, 'InsertReturn>(ct)