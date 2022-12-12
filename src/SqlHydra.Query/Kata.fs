namespace SqlHydra.Query

open System.Reflection
open SqlKata
open System.Collections.Generic
open System

type TableMapping = { Name: string; Schema: string option }

module FQ = 
    /// Fully qualified entity type name
    type [<Struct>] FQName = private FQName of string
    
    let fqName (t: Type) = FQName t.FullName

    type TableMappingKey = 
        | Root
        | TableAliasKey of string

    /// Fully qualifies a column with: {?schema}.{table}.{column}
    let internal fullyQualifyColumn (tables: Map<TableMappingKey, TableMapping>) (tableAlias: string) (column: Reflection.MemberInfo) =
        let tbl = tables[TableAliasKey tableAlias]
        match tbl.Schema with
        | Some schema -> $"%s{schema}.%s{tbl.Name}.%s{column.Name}"
        | None -> $"%s{tbl.Name}.%s{column.Name}"

/// Represents a collection that must contain at least on item.
module AtLeastOne =
    type AtLeastOne<'T> = private { Items : 'T seq }

    /// Returns Some if seq contains at least one item, else returns None.
    let tryCreate<'T> (items: 'T seq) = 
        if items |> Seq.length > 0
        then Some { Items = items }
        else None

    let getSeq { Items = atLeastOne } = 
        atLeastOne

type QueryParameter = 
    {
        Value: obj
        ProviderDbType: string option
    }

type InsertType = 
    | Insert
    | InsertOrReplace
    | OnConflictDoUpdate of conflictFields: string list * updateFields: string list
    | OnConflictDoNothing of conflictFields: string list

type InsertQuerySpec<'T, 'Identity> =
    {
        Table: string
        Entities: 'T list
        Fields: string list
        IdentityField: string option
        InsertType: InsertType
    }
    static member Default : InsertQuerySpec<'T, 'Identity> = 
        { Table = ""; Entities = []; Fields = []; IdentityField = None; InsertType = Insert }

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

module TableMappings = 

    let getByRootOrAlias (tableAlias: string) (tableMappings: Map<FQ.TableMappingKey, TableMapping>) =
        match tableMappings.TryFind(FQ.Root) with
        | Some tbl -> 
            let updatedTableMappings = tableMappings.Remove(FQ.Root).Add(FQ.TableAliasKey tableAlias, tbl)  
            tbl, updatedTableMappings
        | None -> 
            let tbl = tableMappings[FQ.TableAliasKey tableAlias]
            tbl, tableMappings

    let getFirst (tableMappings: Map<FQ.TableMappingKey, TableMapping>) = 
        tableMappings |> Map.toList |> List.map snd |> List.head

type QuerySource<'T>(tableMappings) =
    interface IEnumerable<'T> with
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator() :> Collections.IEnumerator
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator()
    member this.TableMappings : Map<FQ.TableMappingKey, TableMapping> = tableMappings
    
type QuerySource<'T, 'Query>(query, tableMappings) = 
    inherit QuerySource<'T>(tableMappings)
    member this.Query : 'Query = query

module internal KataUtils = 

    // Manually convert DateOnly to DateTime and TimeOnly to TimeSpan (until Microsoft.Data.SqlClient handles)
    let convertIfDateOnlyTimeOnly (value: obj) =
        match value with
#if NET6_0_OR_GREATER
        | :? DateOnly as dateOnly -> box (dateOnly.ToDateTime(TimeOnly.MinValue))
        | :? TimeOnly as timeOnly -> box (timeOnly.ToTimeSpan())
#endif
        | _ -> value

    /// Boxes values (and option values)
    let private boxValueOrOption (value: obj) = 
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

    let private getProviderDbTypeName (p: MemberInfo) =
        match Attribute.GetCustomAttribute(p, typeof<SqlHydra.ProviderDbTypeAttribute>, false) with
        | :? SqlHydra.ProviderDbTypeAttribute as att -> Some att.ProviderDbTypeName
        | _ -> None

    let getQueryParameterForValue (p: MemberInfo) (value: obj) =
        { Value = value |> boxValueOrOption
        ; ProviderDbType = getProviderDbTypeName p } :> obj

    let getQueryParameterForEntity (entity: 'T) (p: PropertyInfo) =
        p.GetValue(entity) 
        |> getQueryParameterForValue p
        
    let fromUpdate (spec: UpdateQuerySpec<'T>) = 
        let kvps = 
            match spec.Entity, spec.SetValues with
            | Some entity, [] -> 
                match spec.Fields with 
                | [] -> 
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.map (fun p -> p.Name, getQueryParameterForEntity entity p)
                        
                | fields -> 
                    let included = fields |> Set.ofList
                    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                    |> Array.filter (fun p -> included.Contains(p.Name)) 
                    |> Array.map (fun p -> p.Name, getQueryParameterForEntity entity p)

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
                |> Array.map (fun p -> KeyValuePair(p.Name, getQueryParameterForEntity entity p))
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
                    |> Array.map (fun p -> getQueryParameterForEntity entity p)
                    |> Array.toSeq
                )
            Query(spec.Table).AsInsert(columns, rowsValues)

    /// Fails if `getId` identity field is used as an `onConflict` target.
    let failIfIdentityOnConflict spec = 
        match spec.IdentityField, spec.InsertType with
        | Some ident, OnConflictDoUpdate (conflictFields, _)
        | Some ident, OnConflictDoNothing conflictFields ->
            if conflictFields |> List.contains ident 
            then failwith $"Using identity column as a conflict target is not supported."
        | _ -> ()


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
