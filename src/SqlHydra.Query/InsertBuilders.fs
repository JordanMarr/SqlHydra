﻿/// Linq insert query builders
[<AutoOpen>]
module SqlHydra.Query.InsertBuilders

open System
open System.Linq.Expressions
open System.Data.Common
open System.Threading.Tasks
open SqlKata

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


/// Builds and returns an insert query.
let insert<'Inserted, 'InsertReturn when 'InsertReturn : struct> = 
    InsertBuilder<'Inserted, 'InsertReturn>()

/// Builds and returns an insert query that returns an Async result.
let insertAsync<'Inserted, 'InsertReturn when 'InsertReturn : struct> ct = 
    InsertAsyncBuilder<'Inserted, 'InsertReturn>(ct)

    /// Builds and returns an insert query that returns a Task result.
let insertTask<'Inserted, 'InsertReturn when 'InsertReturn : struct> ct = 
    InsertTaskBuilder<'Inserted, 'InsertReturn>(ct)