namespace SqlHydra.Query

open SqlKata
open System.Collections.Generic
open System

type TableMapping = { Name: string; Schema: string option }

module FQ = 
    /// Fully qualified entity type name
    type [<Struct>] FQName = private FQName of string
    let fqName (t: Type) = FQName t.FullName

    /// Fully qualifies a column with: {?schema}.{table}.{column}
    let internal fullyQualifyColumn (tables: Map<FQName, TableMapping>) (property: Reflection.MemberInfo) =
        let tbl = tables.[fqName property.DeclaringType]
        match tbl.Schema with
        | Some schema -> $"%s{schema}.%s{tbl.Name}.%s{property.Name}"
        | None -> $"%s{tbl.Name}.%s{property.Name}"

    /// Tries to find a table mapping for a given table record type. 
    let internal fullyQualifyTable (tables: Map<FQName, TableMapping>) (tableRecord: Type) =
        let tbl = tables.[fqName tableRecord]
        match tbl.Schema with
        | Some schema -> $"{schema}.{tbl.Name}"
        | None -> tbl.Name

type InsertQuerySpec<'T, 'Identity> = 
    {
        Table: string
        Entity: 'T option
        Fields: string list
        IdentityField: string option
    }
    static member Default : InsertQuerySpec<'T, 'Identity> = 
        { Table = ""; Entity = Option<'T>.None; Fields = []; IdentityField = None }

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

type QuerySource<'T>(tableMappings) =
    interface IEnumerable<'T> with
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator() :> Collections.IEnumerator
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator()
    
    member this.TableMappings : Map<FQ.FQName, TableMapping> = tableMappings
    member this.GetOuterTableMapping() = 
        let outerEntity = typeof<'T>
        let fqn = 
            if outerEntity.Name.StartsWith "Tuple" // True for joined tables
            then outerEntity.GetGenericArguments() |> Array.head |> FQ.fqName
            else outerEntity |> FQ.fqName
        this.TableMappings.[fqn]

type QuerySource<'T, 'Query>(query, tableMappings) = 
    inherit QuerySource<'T>(tableMappings)
    member this.Query : 'Query = query

module private KataUtils = 

    let boxValue (value: obj) = 
        if isNull value then 
            box System.DBNull.Value
        else
            match value.GetType() with
            | t when t.IsGenericType && t.Name.StartsWith("FSharpOption") -> 
                t.GetProperty("Value").GetValue(value)
            | _ -> value
            |> function 
                | null -> box System.DBNull.Value 
                | o -> o

    let fromUpdate (spec: UpdateQuerySpec<'T>) = 
        let kvps = 
            match spec.Entity, spec.SetValues with
            | Some entity, [] -> 
                match spec.Fields with 
                | [] -> 
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.map (fun p -> p.Name, p.GetValue(entity))
                        
                | fields -> 
                    let included = fields |> Set.ofList
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.filter (fun p -> included.Contains(p.Name)) 
                    |> Array.map (fun p -> p.Name, p.GetValue(entity))

            | Some _, _ -> failwith "Cannot have both `entity` and `set` operations in an `update` expression."
            | None, [] -> failwith "Either an `entity` or `set` operations must be present in an `update` expression."
            | None, setValues -> setValues |> List.toArray
                    
        // Handle option values
        let preparedKvps = 
            kvps 
            |> Seq.map (fun (key,value) -> key, boxValue value)
            |> dict
            |> Seq.map id

        let q = Query(spec.Table).AsUpdate(preparedKvps)

        // Apply `where` clause
        match spec.Where with
        | Some where -> q.Where(fun w -> where)
        | None -> q

    let fromInsert (spec: InsertQuerySpec<'T, 'InsertReturn>) =
        let kvps = 
            match spec.Entity with
            | Some entity -> 
                match spec.Fields with 
                | [] -> 
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.map (fun p -> p.Name, p.GetValue(entity))
                        
                | fields -> 
                    let included = fields |> Set.ofList
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.filter (fun p -> included.Contains(p.Name)) 
                    |> Array.map (fun p -> p.Name, p.GetValue(entity))
            | None -> 
                failwith "Value not set"

        // Handle option values
        let preparedKvps = 
            kvps 
            |> Seq.map (fun (key,value) -> key, boxValue value)
            |> dict
            |> Seq.map id

        Query(spec.Table).AsInsert(preparedKvps, returnId = spec.IdentityField.IsSome)


[<AbstractClass>]
type SelectQuery() = 
    abstract member ToKataQuery : unit -> SqlKata.Query

type SelectQuery<'T>(query: SqlKata.Query) = 
    inherit SelectQuery()
    override this.ToKataQuery() = query

type DeleteQuery<'T>(query: SqlKata.Query) = 
    member this.ToKataQuery() = query

type UpdateQuery<'T>(spec: UpdateQuerySpec<'T>) =
    member internal this.Spec = spec
    member this.ToKataQuery() = spec |> KataUtils.fromUpdate

type InsertQuery<'T, 'Identity>(spec: InsertQuerySpec<'T, 'Identity>) =
    member internal this.Spec = spec
    member this.ToKataQuery() = spec |> KataUtils.fromInsert