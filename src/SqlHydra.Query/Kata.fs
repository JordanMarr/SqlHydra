namespace SqlHydra.Query

open System.Reflection
open SqlHydra.ProviderDbTypeAttribute
open SqlHydra.Domain
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

type QueryParameter = 
    {
        Value: obj
        ProviderDbType: string option
    }

type InsertQuerySpec<'T, 'Identity> =
    {
        Table: string
        Entities: 'T list
        Fields: string list
        IdentityField: string option
    }
    static member Default : InsertQuerySpec<'T, 'Identity> = 
        { Table = ""; Entities = []; Fields = []; IdentityField = None }

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

    /// Boxes values (and option values)
    let boxValueOrOption (value: obj) = 
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

    let getProviderDbTypeName (p: PropertyInfo) =
        let attrs = p.GetCustomAttributes(true)
        (attrs
        |> Seq.choose (function | :? ProviderDbTypeAttribute as attr -> Some attr.ProviderDbTypeName | _ -> None))
        |> Seq.tryHead
    
    let fromUpdate (spec: UpdateQuerySpec<'T>) = 
        let kvps = 
            match spec.Entity, spec.SetValues with
            | Some entity, [] -> 
                match spec.Fields with 
                | [] -> 
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.map (fun p -> p.Name, { Value = p.GetValue(entity) |> boxValueOrOption; ProviderDbType = getProviderDbTypeName p } :> obj)
                        
                | fields -> 
                    let included = fields |> Set.ofList
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.filter (fun p -> included.Contains(p.Name)) 
                    |> Array.map (fun p -> p.Name, { Value = p.GetValue(entity) |> boxValueOrOption; ProviderDbType = getProviderDbTypeName p } :> obj)

            | Some _, _ -> failwith "Cannot have both `entity` and `set` operations in an `update` expression."
            | None, [] -> failwith "Either an `entity` or `set` operations must be present in an `update` expression."
            | None, setValues -> setValues |> List.toArray
                    
        let preparedKvps = 
            kvps 
            |> Seq.map (fun (key,value) -> key, value)
            |> dict
            |> Seq.map id

        let q = Query(spec.Table).AsUpdate(preparedKvps)

        // Apply `where` clause
        match spec.Where with
        | Some where -> q.Where(fun w -> where)
        | None -> q

    let fromInsert (spec: InsertQuerySpec<'T, 'Identity>) =
        let includedProperties = 
            match spec.Fields with
            | [] -> 
                FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
            | fields ->
                let included = fields |> Set.ofList
                FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                |> Array.filter (fun p -> included.Contains(p.Name)) 
    
        match spec.Entities with
        | [] -> 
            failwith "At least one `entity` or `entities` must be set in the `insert` builder."

        | [ entity ] -> 
            let keyValuePairs =
                includedProperties
                |> Array.map (fun p -> KeyValuePair(p.Name, { Value = p.GetValue(entity) |> boxValueOrOption; ProviderDbType = getProviderDbTypeName p } :> obj))
                |> Array.toList
            Query(spec.Table).AsInsert(keyValuePairs, returnId = spec.IdentityField.IsSome)

        | entities -> 
            if spec.IdentityField.IsSome 
            then failwith "`getId` is not currently supported for multiple inserts via the `entities` operation."
            let columns = includedProperties |> Array.map (fun p -> p.Name)
            let rowsValues =
                entities
                |> List.map (fun entity ->
                    includedProperties
                    |> Array.map (fun p -> { Value = p.GetValue(entity) |> boxValueOrOption; ProviderDbType = getProviderDbTypeName p } :> obj)
                    |> Array.toSeq
                )
            Query(spec.Table).AsInsert(columns, rowsValues)


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
