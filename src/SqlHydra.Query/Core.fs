namespace SqlHydra.Query

open System.Reflection
open System.Collections.Generic
open System

type TableMapping =
    {
        Name: string
        Schema: string
    }
    member this.IsInTable (m: Linq.Expressions.MemberExpression) =
        m.Member.ReflectedType.DeclaringType <> null &&
        m.Member.ReflectedType.DeclaringType.Name = this.Schema &&
        m.Member.ReflectedType.Name = this.Name

type TableMappingKey =
    | Root
    | TableAliasKey of string

module TableMappings =

    /// Tries to get TableMapping by Root, then by Alias.
    /// If found by Root, replaces with a TableAliasKey.
    let tryGetByRootOrAlias (tableAlias: string) (tableMappings: Map<TableMappingKey, TableMapping>) =
        match tableMappings.TryFind(Root) with
        | Some tbl ->
            let updatedTableMappings = tableMappings.Remove(Root).Add(TableAliasKey tableAlias, tbl)
            Some tbl, updatedTableMappings
        | None ->
            match tableMappings.TryFind(TableAliasKey tableAlias) with
            | Some tbl -> Some tbl, tableMappings
            | None -> None, tableMappings

    /// Gets the first TableMapping.
    let getFirst (tableMappings: Map<TableMappingKey, TableMapping>) =
        tableMappings |> Map.toList |> List.map snd |> List.head

module FQ =

    /// Fully qualifies a column with: {?schema}.{table}.{column}
    let internal fullyQualifyColumn (tables: Map<TableMappingKey, TableMapping>) (tableAlias: string) (column: Reflection.MemberInfo) =
        let tbl = tables[TableAliasKey tableAlias]
        $"%s{tbl.Schema}.%s{tbl.Name}.%s{column.Name}"

    /// Renders a table as `schema.name` or just `name` when schema is empty (CTE refs).
    let qualifiedTable (tbl: TableMapping) =
        if tbl.Schema = "" then tbl.Name
        else $"%s{tbl.Schema}.%s{tbl.Name}"

/// Represents a collection that must contain at least on item.
module AtLeastOne =
    [<NoComparison>]
    type AtLeastOne<'T> = private { Items : 'T seq }

    /// Returns Some if seq contains at least one item, else returns None.
    let tryCreate<'T> (items: 'T seq) =
        if items |> Seq.length > 0
        then Some { Items = items }
        else None

    let getSeq { Items = atLeastOne } =
        atLeastOne

/// Wraps a query parameter to provide the generated ProviderDbType attribute value.
[<NoComparison>]
type QueryParameter =
    {
        Value: obj
        ProviderDbType: string option
    }
    /// Provides a more compact representation of the QueryParameter when logging queries.
    override this.ToString() =
        match this.ProviderDbType with
        | Some providerDbType -> $"%s{providerDbType}: {this.Value}"
        | None -> $"obj: {this.Value}"

/// Pending conflict target accumulated by the composable `onConflict ...` CE op,
/// awaiting a `doNothing` / `doUpdate` / `doUpdateCoalesce` action to finalize.
[<NoComparison>]
type PendingConflictTarget =
    /// `onConflict col1 col2 ...` — typed columns
    | TypedConflictColumns of fields: string list * whereRaw: string option
    /// `onConflictRaw "lower(email)"` — expression-index target
    | RawConflictTarget of rawTargetExpr: string * whereRaw: string option

[<NoComparison>]
type InsertQuerySpec<'T, 'Identity> =
    {
        Table: string
        /// One list per row: column name and its `QueryParameter`, as `set` stores them.
        Entities: (string * obj) list list
        Fields: string list
        IdentityField: string option
        OutputFields: OutputField list
        InsertType: InsertType
        Returning: string list
        FromSelect: SelectQueryIR option
        /// Pending conflict target while building a composable `onConflict ... doNothing` chain.
        /// Cleared once a conflict action finalizes the spec into `InsertType`.
        PendingConflict: PendingConflictTarget option
        CommandOptions: CommandOptions
    }
    static member Default : InsertQuerySpec<'T, 'Identity> =
        { Table = ""; Entities = []; Fields = []; IdentityField = None; OutputFields = []
          InsertType = Insert; Returning = []; FromSelect = None; PendingConflict = None
          CommandOptions = CommandOptions.Default }

[<NoComparison>]
type UpdateQuerySpec<'T, 'UpdateReturn> =
    {
        Table: string
        /// Boxed: a `'T` row, or an `IWriteOf<'T>` row that has no field for a read-only column.
        /// Column name and its `QueryParameter` per field, as `SetValues` stores them.
        Entity: (string * obj) list option
        Fields: string list
        SetValues: (string * obj) list
        RawSetValues: (string * string * obj[]) list
        Where: WhereClause
        OutputFields: OutputField list
        UpdateAll: bool
        Returning: string list
        CommandOptions: CommandOptions
    }
    static member Default : UpdateQuerySpec<'T, 'UpdateReturn> =
        { Table = ""; Entity = None; Fields = []; SetValues = []; RawSetValues = []
          Where = WhereClause.Empty; OutputFields = []; UpdateAll = false; Returning = []
          CommandOptions = CommandOptions.Default }

[<NoComparison>]
type DeleteQuerySpec<'T> =
    {
        Table: string
        Where: WhereClause
        DeleteAll: bool
        Returning: string list
        CommandOptions: CommandOptions
    }
    static member Default : DeleteQuerySpec<'T> =
        { Table = ""; Where = WhereClause.Empty; DeleteAll = false; Returning = []
          CommandOptions = CommandOptions.Default }

type QuerySource<'T>(tableMappings) =
    interface IEnumerable<'T> with
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator() :> Collections.IEnumerator
        member this.GetEnumerator() = Seq.empty<'T>.GetEnumerator()
    member this.TableMappings : Map<TableMappingKey, TableMapping> = tableMappings

type QuerySource<'T, 'Query>(query, tableMappings) =
    inherit QuerySource<'T>(tableMappings)
    member this.Query : 'Query = query

/// The type of join for predicate-style joins
type JoinType =
    | Inner
    | Left

/// Information about a pending join that will be completed with an `on'` clause
type PendingJoin = {
    JoinType: JoinType
    TableName: string     // e.g., "Sales.SalesOrderDetail"
    TableAlias: string    // e.g., "d"
}

/// Module to store pending join info for queries using predicate-style joins.
/// Uses a ConditionalWeakTable keyed on a boxed reference cell for GC-safe association.
module PendingJoins =
    open System.Runtime.CompilerServices

    // Use a boxed ref cell as a unique identity key per query IR
    let private pendingJoins = ConditionalWeakTable<obj, PendingJoin>()

    /// Associates a pending join with a query key object
    let set (key: obj) (pendingJoin: PendingJoin) =
        pendingJoins.Remove(key) |> ignore
        pendingJoins.Add(key, pendingJoin)

    /// Gets and removes the pending join for a query key
    let tryTake (key: obj) =
        match pendingJoins.TryGetValue(key) with
        | true, pj ->
            pendingJoins.Remove(key) |> ignore
            Some pj
        | false, _ -> None

module internal QueryUtils =

    // Manually convert DateOnly to DateTime and TimeOnly to TimeSpan (until Microsoft.Data.SqlClient handles)
    let convertIfDateOnlyTimeOnly (value: obj) =
        match value with
#if NET6_0_OR_GREATER
        | :? DateOnly as dateOnly -> box (dateOnly.ToDateTime(TimeOnly.MinValue))
        | :? TimeOnly as timeOnly -> box (timeOnly.ToTimeSpan())
#endif
        | _ -> value

    /// Boxes values (and option values)
    let boxValueOrOption (value: obj) =
        if isNull value then
            box System.DBNull.Value
        else
            match value.GetType() with
            | t when t.IsGenericType && t.Name.StartsWith("FSharpOption") ->
                t.GetProperty("Value").GetValue(value)
            | t when t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Nullable<_>> ->
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

    /// The columns a row writes, each with its `QueryParameter`. A generated record says so
    /// through `IWriteColumns`; any other record is reflected.
    let writeColumns (row: obj) : (string * obj) list =
        match row with
        | :? SqlHydra.IWriteColumns as generated ->
            generated.WriteColumns
            |> List.map (fun c -> c.Name, box { Value = boxValueOrOption c.Value; ProviderDbType = c.ProviderDbType })
        | _ ->
            FSharp.Reflection.FSharpType.GetRecordFields(row.GetType())
            |> Array.map (fun p -> p.Name, getQueryParameterForEntity row p)
            |> Array.toList

    /// `spec.Fields` narrows a row to the columns `includeColumn`/`excludeColumn` selected.
    let private selectedColumns (fields: string list) (row: (string * obj) list) =
        match fields with
        | [] -> row
        | fields -> row |> List.filter (fun (name, _) -> List.contains name fields)

    let fromUpdate (spec: UpdateQuerySpec<'T, 'UpdateReturn>) : UpdateQueryIR =
        let kvps =
            match spec.Entity, spec.SetValues with
            | Some row, [] -> selectedColumns spec.Fields row
            | Some _, _ -> failwith "Cannot have both `entity` and `set` operations in an `update` expression."
            | None, [] when spec.RawSetValues.IsEmpty ->
                failwith "Either an `entity`, `set`, or `setRaw` operation must be present in an `update` expression."
            | None, setValues -> setValues

        {
            Table = spec.Table
            SetColumns = kvps
            SetRaws = spec.RawSetValues
            Where = spec.Where
            OutputFields = spec.OutputFields
            Returning = spec.Returning
            CommandOptions = spec.CommandOptions
        }

    let fromInsert (spec: InsertQuerySpec<'T, 'InsertReturn>) : InsertQueryIR =
        // The rows say which columns they write; with no rows (`insert ... select`) 'T's fields do.
        let rows = spec.Entities |> List.map (selectedColumns spec.Fields)
        let columns =
            match rows with
            | first :: _ -> first |> List.map fst
            | [] ->
                FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>)
                |> Array.map (fun p -> p.Name)
                |> Array.toList
                |> List.filter (fun name -> spec.Fields.IsEmpty || List.contains name spec.Fields)

        match spec.FromSelect, spec.Entities with
        | Some selectIR, _ ->
            // INSERT INTO ... (cols) <select-subquery>
            {
                Table = spec.Table
                Columns = columns
                Rows = []
                FromSelect = Some selectIR
                IdentityField = spec.IdentityField
                InsertType = spec.InsertType
                OutputFields = spec.OutputFields
                Returning = spec.Returning
                CommandOptions = spec.CommandOptions
            }
        | None, [] ->
            failwith "At least one `entity` or `entities` must be set in the `insert` builder."
        | None, entities ->
            if spec.IdentityField.IsSome && entities.Length > 1
            then failwith "`getId` is not currently supported for multiple inserts via the `entities` operation."
            {
                Table = spec.Table
                Columns = columns
                Rows = rows |> List.map (List.map snd >> List.toArray)
                FromSelect = None
                IdentityField = spec.IdentityField
                InsertType = spec.InsertType
                OutputFields = spec.OutputFields
                Returning = spec.Returning
                CommandOptions = spec.CommandOptions
            }

    /// Fails if `getId` identity field is used as an `onConflict` target.
    let failIfIdentityOnConflict spec =
        match spec.IdentityField, spec.InsertType with
        | Some ident, OnConflictDoUpdate (conflictFields, _)
        | Some ident, OnConflictDoNothing conflictFields
        | Some ident, InsertOrUpdateOnUnique (conflictFields, _) ->
            if conflictFields |> List.contains ident
            then failwith $"Using identity column as a conflict target is not supported."
        | _ -> ()


[<AbstractClass>]
type SelectQuery() =
    /// Returns the underlying SelectQueryIR. Used by subquery expressions.
    abstract member SelectIR: SelectQueryIR
    /// Compiles the query using the given emitter. Used by toSql test helpers.
    abstract member CompileWith: ISqlEmitter -> CompiledQuery

type SelectQuery<'T>(ir: SelectQueryIR) =
    inherit SelectQuery()
    member this.IR = ir
    override this.SelectIR = ir
    override this.CompileWith(emitter) = emitter.EmitSelect(ir)

type DeleteQuery<'T>(ir: DeleteQueryIR) =
    inherit SelectQuery()
    member this.IR = ir
    override this.SelectIR = { SelectQueryIR.empty with From = Some ir.Table; Where = ir.Where }
    override this.CompileWith(emitter) = emitter.EmitDelete(ir)

type UpdateQuery<'T, 'UpdateReturn>(spec: UpdateQuerySpec<'T, 'UpdateReturn>) =
    let ir = lazy QueryUtils.fromUpdate spec
    member this.Spec = spec
    /// Returns the underlying UpdateQueryIR. Raises for invalid specs (e.g. missing entity/set, or both combined).
    member this.IR = ir.Value

type InsertQuery<'T, 'Identity>(spec: InsertQuerySpec<'T, 'Identity>) =
    let ir = lazy QueryUtils.fromInsert spec
    member this.Spec = spec
    /// Returns the underlying InsertQueryIR. Raises for invalid specs (e.g. no entities, or getId with multiple rows).
    member this.IR = ir.Value
