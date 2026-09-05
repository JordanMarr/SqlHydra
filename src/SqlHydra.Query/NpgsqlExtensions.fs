module SqlHydra.Query.NpgsqlExtensions

open System

/// Common PostgreSQL functions for use in select expressions.
/// Use `open type SqlFn` to access functions without qualification.
[<SqlHydraFunction>]
type SqlFn =
    // <generated> by codegen/NpgsqlSqlFn.fsx from pg_proc; edit the allowlist, not this block.
    static member char_length(s: string) : int = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member char_length(s: string option) : int option = sqlFn
    static member character_length(s: string) : int = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member character_length(s: string option) : int option = sqlFn
    static member length(s: string) : int = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member length(s: string option) : int option = sqlFn
    static member length(bytes: byte[]) : int = sqlFn
    /// NULL `bytes` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member length(bytes: byte[] option) : int option = sqlFn
    static member upper(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member upper(s: string option) : string option = sqlFn
    static member lower(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member lower(s: string option) : string option = sqlFn
    static member ltrim(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member ltrim(s: string option) : string option = sqlFn
    static member ltrim(s: string, chars: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member ltrim(s: string option, chars: string) : string option = sqlFn
    /// NULL `chars` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member ltrim(s: string, chars: string option) : string option = sqlFn
    static member rtrim(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rtrim(s: string option) : string option = sqlFn
    static member rtrim(s: string, chars: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rtrim(s: string option, chars: string) : string option = sqlFn
    /// NULL `chars` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rtrim(s: string, chars: string option) : string option = sqlFn
    static member btrim(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member btrim(s: string option) : string option = sqlFn
    static member btrim(s: string, chars: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member btrim(s: string option, chars: string) : string option = sqlFn
    /// NULL `chars` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member btrim(s: string, chars: string option) : string option = sqlFn
    static member trim(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member trim(s: string option) : string option = sqlFn
    static member trim(s: string, chars: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member trim(s: string option, chars: string) : string option = sqlFn
    /// NULL `chars` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member trim(s: string, chars: string option) : string option = sqlFn
    static member substring(s: string, start: int) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member substring(s: string option, start: int) : string option = sqlFn
    /// NULL `start` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member substring(s: string, start: int option) : string option = sqlFn
    static member substring(s: string, start: int, length: int) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member substring(s: string option, start: int, length: int) : string option = sqlFn
    /// NULL `start` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member substring(s: string, start: int option, length: int) : string option = sqlFn
    /// NULL `length` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member substring(s: string, start: int, length: int option) : string option = sqlFn
    static member substring(s: string, pattern: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member substring(s: string option, pattern: string) : string option = sqlFn
    /// NULL `pattern` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member substring(s: string, pattern: string option) : string option = sqlFn
    static member replace(s: string, from: string, ``to``: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member replace(s: string option, from: string, ``to``: string) : string option = sqlFn
    /// NULL `from` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member replace(s: string, from: string option, ``to``: string) : string option = sqlFn
    /// NULL ```to``` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member replace(s: string, from: string, ``to``: string option) : string option = sqlFn
    [<SqlHydraFunction("pg_catalog.position")>]
    static member position(s: string, substring: string) : int = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    [<SqlHydraFunction("pg_catalog.position")>]
    static member position(s: string option, substring: string) : int option = sqlFn
    /// NULL `substring` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    [<SqlHydraFunction("pg_catalog.position")>]
    static member position(s: string, substring: string option) : int option = sqlFn
    static member strpos(s: string, substring: string) : int = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member strpos(s: string option, substring: string) : int option = sqlFn
    /// NULL `substring` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member strpos(s: string, substring: string option) : int option = sqlFn
    static member left(s: string, length: int) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member left(s: string option, length: int) : string option = sqlFn
    /// NULL `length` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member left(s: string, length: int option) : string option = sqlFn
    static member right(s: string, length: int) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member right(s: string option, length: int) : string option = sqlFn
    /// NULL `length` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member right(s: string, length: int option) : string option = sqlFn
    static member reverse(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member reverse(s: string option) : string option = sqlFn
    static member repeat(s: string, count: int) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member repeat(s: string option, count: int) : string option = sqlFn
    /// NULL `count` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member repeat(s: string, count: int option) : string option = sqlFn
    static member lpad(s: string, length: int) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member lpad(s: string option, length: int) : string option = sqlFn
    /// NULL `length` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member lpad(s: string, length: int option) : string option = sqlFn
    static member lpad(s: string, length: int, fill: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member lpad(s: string option, length: int, fill: string) : string option = sqlFn
    /// NULL `length` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member lpad(s: string, length: int option, fill: string) : string option = sqlFn
    /// NULL `fill` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member lpad(s: string, length: int, fill: string option) : string option = sqlFn
    static member rpad(s: string, length: int) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rpad(s: string option, length: int) : string option = sqlFn
    /// NULL `length` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rpad(s: string, length: int option) : string option = sqlFn
    static member rpad(s: string, length: int, fill: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rpad(s: string option, length: int, fill: string) : string option = sqlFn
    /// NULL `length` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rpad(s: string, length: int option, fill: string) : string option = sqlFn
    /// NULL `fill` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member rpad(s: string, length: int, fill: string option) : string option = sqlFn
    static member initcap(s: string) : string = sqlFn
    /// NULL `s` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member initcap(s: string option) : string option = sqlFn
    static member concat(s1: string, s2: string) : string = sqlFn
    static member concat(s1: string, s2: string, s3: string) : string = sqlFn
    static member concat_ws(separator: string, s1: string, s2: string) : string = sqlFn
    static member concat_ws(separator: string, s1: string, s2: string, s3: string) : string = sqlFn
    static member now() : DateTime = sqlFn
    [<SqlHydraFunction("pg_catalog.extract")>]
    static member extract(field: string, source: DateTime) : decimal = sqlFn
    /// NULL `field` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    [<SqlHydraFunction("pg_catalog.extract")>]
    static member extract(field: string option, source: DateTime) : decimal option = sqlFn
    /// NULL `source` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    [<SqlHydraFunction("pg_catalog.extract")>]
    static member extract(field: string, source: DateTime option) : decimal option = sqlFn
    static member date_trunc(field: string, source: DateTime) : DateTime = sqlFn
    /// NULL `field` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member date_trunc(field: string option, source: DateTime) : DateTime option = sqlFn
    /// NULL `source` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member date_trunc(field: string, source: DateTime option) : DateTime option = sqlFn
    static member date_part(field: string, source: DateTime) : float = sqlFn
    /// NULL `field` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member date_part(field: string option, source: DateTime) : float option = sqlFn
    /// NULL `source` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member date_part(field: string, source: DateTime option) : float option = sqlFn
    static member age(timestamp: DateTime) : TimeSpan = sqlFn
    /// NULL `timestamp` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member age(timestamp: DateTime option) : TimeSpan option = sqlFn
    static member age(timestamp1: DateTime, timestamp2: DateTime) : TimeSpan = sqlFn
    /// NULL `timestamp1` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member age(timestamp1: DateTime option, timestamp2: DateTime) : TimeSpan option = sqlFn
    /// NULL `timestamp2` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member age(timestamp1: DateTime, timestamp2: DateTime option) : TimeSpan option = sqlFn
    static member make_date(year: int, month: int, day: int) : DateTime = sqlFn
    /// NULL `year` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member make_date(year: int option, month: int, day: int) : DateTime option = sqlFn
    /// NULL `month` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member make_date(year: int, month: int option, day: int) : DateTime option = sqlFn
    /// NULL `day` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member make_date(year: int, month: int, day: int option) : DateTime option = sqlFn
    static member make_time(hour: int, minute: int, second: float) : TimeSpan = sqlFn
    /// NULL `hour` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member make_time(hour: int option, minute: int, second: float) : TimeSpan option = sqlFn
    /// NULL `minute` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member make_time(hour: int, minute: int option, second: float) : TimeSpan option = sqlFn
    /// NULL `second` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`.
    static member make_time(hour: int, minute: int, second: float option) : TimeSpan option = sqlFn
    // </generated>

    // Expression nodes, not catalog functions: hand-written.
    // Null handling - with overloads for Option and Nullable
    static member coalesce(a: Option<'T>, b: 'T) : 'T = sqlFn
    static member coalesce(a: Nullable<'T>, b: 'T) : 'T when 'T : struct = sqlFn
    static member coalesce(a: 'T, b: 'T) : 'T = sqlFn
    static member coalesce(a: 'T, b: 'T, c: 'T) : 'T = sqlFn
    static member nullif(a: 'T, b: 'T) : Option<'T> = sqlFn

    // Numeric functions
    static member abs(n: 'T) : 'T when 'T : struct = sqlFn
    static member round(n: 'T) : 'T when 'T : struct = sqlFn
    static member round(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn
    static member ceil(n: 'T) : 'T when 'T : struct = sqlFn
    static member ceiling(n: 'T) : 'T when 'T : struct = sqlFn
    static member floor(n: 'T) : 'T when 'T : struct = sqlFn
    static member sign(n: 'T) : int when 'T : struct = sqlFn
    static member power(n: 'T, exponent: 'T) : 'T when 'T : struct = sqlFn
    static member sqrt(n: 'T) : float when 'T : struct = sqlFn
    static member mod'(n: 'T, divisor: 'T) : 'T when 'T : struct = sqlFn
    static member trunc(n: 'T) : 'T when 'T : struct = sqlFn
    static member trunc(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn

    // Date/time functions
    static member current_date() : DateTime = sqlFn
    static member current_time() : TimeSpan = sqlFn
    static member current_timestamp() : DateTime = sqlFn

    // GREATEST / LEAST — variadic standard SQL functions
    static member greatest(a: 'T, b: 'T) : 'T = sqlFn
    static member greatest(a: 'T, b: 'T, c: 'T) : 'T = sqlFn
    static member greatest(a: 'T, b: 'T, c: 'T, d: 'T) : 'T = sqlFn
    static member least(a: 'T, b: 'T) : 'T = sqlFn
    static member least(a: 'T, b: 'T, c: 'T) : 'T = sqlFn
    static member least(a: 'T, b: 'T, c: 'T, d: 'T) : 'T = sqlFn

/// PostgreSQL-specific functions.
[<SqlHydraFunction>]
type PgSqlFn =
    /// Renders a PostgreSQL `INTERVAL '<value>'` literal.
    /// Example: `interval "7 days"` → `INTERVAL '7 days'`
    static member interval(value: string) : TimeSpan = sqlFn

/// The columns `onConflict` left pending, for `keyword` to close into a DO UPDATE action.
let private pendingConflictFields (keyword: string) (spec: InsertQuerySpec<'T, 'InsertReturn>) =
    match spec.PendingConflict with
    | Some (TypedConflictColumns (fields, None)) -> fields
    | Some (TypedConflictColumns _) -> failwith $"{keyword} does not currently support a partial-index WHERE clause"
    | Some (RawConflictTarget _) -> failwith $"{keyword} requires a typed conflict target (use onConflict, not onConflictRaw)"
    | None -> failwith $"{keyword} requires onConflict to be called first"

type InsertBuilder<'Inserted, 'InsertReturn, 'Write when 'Write :> SqlHydra.IWriteOf<'Inserted>> with
    
    /// Performs an update on one or more update fields if a conflict occurs.
    [<CustomOperation("onConflictDoUpdate", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdate(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFields, 
        [<ProjectionParameter>] updateFields) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFields (fun tblAlias p -> p.Name)
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'UpdateProperties> updateFields (fun tblAlias p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoUpdate (conflictFields, updateFields) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// `onConflictDoUpdate` with the update fields selected over the write record, so a read-only column cannot be named.
    [<CustomOperation("onConflictDoUpdateWrite", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdateWrite(state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>,
        [<ProjectionParameter>] conflictFields: System.Linq.Expressions.Expression<Func<'Inserted, 'ConflictProperty>>,
        updateFields: System.Linq.Expressions.Expression<Func<'Write, 'UpdateProperties>>) =
        let spec = state.Query
        let conflictColumns = LinqExpressionVisitors.visitPropertiesSelector<'Inserted, 'ConflictProperty> conflictFields (fun _ p -> p.Name)
        let updateColumns = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'UpdateProperties> updateFields (fun _ p -> p.Name)
        QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>(
            { spec with InsertType = OnConflictDoUpdate (conflictColumns, updateColumns) }, state.TableMappings)

    /// Insert is ignored if a conflict occurs.
    [<CustomOperation("onConflictDoNothing", MaintainsVariableSpace = true)>]
    member this.OnConflictDoNothing(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        [<ProjectionParameter>] conflictFields) =

        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFields (fun tblAlias p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoNothing conflictFields }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// ON CONFLICT (cols) DO UPDATE SET — fields in `coalesceFields` get
    /// `col = COALESCE(EXCLUDED.col, table.col)` (preserves existing non-null values
    /// when the new value is NULL). Other fields use `col = EXCLUDED.col`.
    /// `coalesceFields` should be a subset of `updateFields`.
    [<CustomOperation("onConflictDoUpdateCoalesce", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdateCoalesce(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        [<ProjectionParameter>] conflictFields,
        [<ProjectionParameter>] updateFields,
        [<ProjectionParameter>] coalesceFields) =
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFields (fun _ p -> p.Name)
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'UpdateProperties> updateFields (fun _ p -> p.Name)
        let coalesceFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'CoalesceProperties> coalesceFields (fun _ p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoUpdateCoalesce (conflictFields, updateFields, coalesceFields) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// `onConflictDoUpdateCoalesce` with the update and coalesce fields selected over the write record,
    /// so a read-only column cannot be named.
    [<CustomOperation("onConflictDoUpdateCoalesceWrite", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdateCoalesceWrite(state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>,
        [<ProjectionParameter>] conflictFields: System.Linq.Expressions.Expression<Func<'Inserted, 'ConflictProperty>>,
        updateFields: System.Linq.Expressions.Expression<Func<'Write, 'UpdateProperties>>,
        coalesceFields: System.Linq.Expressions.Expression<Func<'Write, 'CoalesceProperties>>) =
        let spec = state.Query
        let conflictColumns = LinqExpressionVisitors.visitPropertiesSelector<'Inserted, 'ConflictProperty> conflictFields (fun _ p -> p.Name)
        let updateColumns = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'UpdateProperties> updateFields (fun _ p -> p.Name)
        let coalesceColumns = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'CoalesceProperties> coalesceFields (fun _ p -> p.Name)
        QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>(
            { spec with InsertType = OnConflictDoUpdateCoalesce (conflictColumns, updateColumns, coalesceColumns) }, state.TableMappings)

    /// ON CONFLICT (cols) WHERE <whereFragment> DO NOTHING — for partial-index conflicts.
    /// Use ? as parameter placeholders in the where fragment.
    [<CustomOperation("onConflictDoNothingWhereRaw", MaintainsVariableSpace = true)>]
    member this.OnConflictDoNothingWhereRaw(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        [<ProjectionParameter>] conflictFields,
        whereFragment: string,
        parameters: obj[]) =
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFields (fun _ p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoNothingWhereRaw (conflictFields, whereFragment, parameters) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// ON CONFLICT (<rawTargetExpr>) DO NOTHING — for expression indexes (e.g., lower(email)).
    [<CustomOperation("onConflictDoNothingRawTarget", MaintainsVariableSpace = true)>]
    member this.OnConflictDoNothingRawTarget(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        rawTargetExpr: string) =
        let spec = state.Query
        let newSpec = { spec with InsertType = OnConflictDoNothingRawTarget rawTargetExpr }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    // ─── Composable ON CONFLICT API ──────────────────────────────────────────────
    // Usage:
    //   onConflict col [whereRawConflict "..."]  (sets target)
    //   doNothing | doUpdate cols | doUpdateCoalesce cols  (sets action)
    //
    // The target is held in PendingConflict until an action operation finalizes
    // the spec into a closed `InsertType` value.

    /// Sets the conflict target to typed column(s). Followed by `doNothing` / `doUpdate` / `doUpdateCoalesce`.
    [<CustomOperation("onConflict", MaintainsVariableSpace = true)>]
    member this.OnConflict(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        [<ProjectionParameter>] conflictFields) =
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFields (fun _ p -> p.Name)
        let newSpec = { spec with PendingConflict = Some (TypedConflictColumns (conflictFields, None)) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// Sets a raw-expression conflict target (for expression indexes like `lower(email)`).
    /// Followed by `doNothing` / `doUpdate` / `doUpdateCoalesce`.
    [<CustomOperation("onConflictRaw", MaintainsVariableSpace = true)>]
    member this.OnConflictRaw(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        rawTargetExpr: string) =
        let spec = state.Query
        let newSpec = { spec with PendingConflict = Some (RawConflictTarget (rawTargetExpr, None)) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// Adds a partial-index WHERE clause to the conflict target (must follow `onConflict`).
    [<CustomOperation("whereRawConflict", MaintainsVariableSpace = true)>]
    member this.WhereRawConflict(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        whereClause: string) =
        let spec = state.Query
        let updated =
            match spec.PendingConflict with
            | Some (TypedConflictColumns (fields, _)) -> TypedConflictColumns (fields, Some whereClause)
            | Some (RawConflictTarget (raw, _)) -> RawConflictTarget (raw, Some whereClause)
            | None -> failwith "whereRawConflict requires onConflict (or onConflictRaw) to be called first"
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with PendingConflict = Some updated }, state.TableMappings)

    /// Conflict action: DO NOTHING. Closes the pending conflict target into InsertType.
    [<CustomOperation("doNothing", MaintainsVariableSpace = true)>]
    member this.DoNothing(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>) =
        let spec = state.Query
        let newType =
            match spec.PendingConflict with
            | Some (TypedConflictColumns (fields, None)) ->
                OnConflictDoNothing fields
            | Some (TypedConflictColumns (fields, Some whereRaw)) ->
                OnConflictDoNothingWhereRaw (fields, whereRaw, [||])
            | Some (RawConflictTarget (raw, None)) ->
                OnConflictDoNothingRawTarget raw
            | Some (RawConflictTarget (_, Some _)) ->
                failwith "ON CONFLICT (raw target) WHERE clause is not currently supported; use onConflict <columns> with whereRawConflict instead"
            | None ->
                failwith "doNothing requires onConflict (or onConflictRaw) to be called first"
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with InsertType = newType; PendingConflict = None },
            state.TableMappings)

    /// Conflict action: DO UPDATE SET col=EXCLUDED.col for each update field.
    [<CustomOperation("doUpdate", MaintainsVariableSpace = true)>]
    member this.DoUpdate(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        [<ProjectionParameter>] updateFields) =
        let spec = state.Query
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'UpdateProperties> updateFields (fun _ p -> p.Name)
        let conflictFields = pendingConflictFields "doUpdate" spec
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with InsertType = OnConflictDoUpdate (conflictFields, updateFields); PendingConflict = None },
            state.TableMappings)

    /// `doUpdate` with the update fields selected over the write record, so a read-only column cannot be named.
    [<CustomOperation("doUpdateWrite", MaintainsVariableSpace = true)>]
    member this.DoUpdateWrite(state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>,
        updateFields: System.Linq.Expressions.Expression<Func<'Write, 'UpdateProperties>>) =
        let spec = state.Query
        let updateColumns = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'UpdateProperties> updateFields (fun _ p -> p.Name)
        let conflictColumns = pendingConflictFields "doUpdateWrite" spec
        QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>(
            { spec with InsertType = OnConflictDoUpdate (conflictColumns, updateColumns); PendingConflict = None },
            state.TableMappings)

    /// Conflict action: DO UPDATE SET — `updateFields` are updated as `col = EXCLUDED.col`,
    /// except those listed in `coalesceFields` which become `col = COALESCE(EXCLUDED.col, col)`.
    /// `coalesceFields` should be a subset of `updateFields`.
    [<CustomOperation("doUpdateCoalesce", MaintainsVariableSpace = true)>]
    member this.DoUpdateCoalesce(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        [<ProjectionParameter>] updateFields,
        [<ProjectionParameter>] coalesceFields) =
        let spec = state.Query
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'UpdateProperties> updateFields (fun _ p -> p.Name)
        let coalesceFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'CoalesceProperties> coalesceFields (fun _ p -> p.Name)
        let conflictFields = pendingConflictFields "doUpdateCoalesce" spec
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with InsertType = OnConflictDoUpdateCoalesce (conflictFields, updateFields, coalesceFields); PendingConflict = None },
            state.TableMappings)

    /// `doUpdateCoalesce` with the update and coalesce fields selected over the write record,
    /// so a read-only column cannot be named.
    [<CustomOperation("doUpdateCoalesceWrite", MaintainsVariableSpace = true)>]
    member this.DoUpdateCoalesceWrite(state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>,
        updateFields: System.Linq.Expressions.Expression<Func<'Write, 'UpdateProperties>>,
        coalesceFields: System.Linq.Expressions.Expression<Func<'Write, 'CoalesceProperties>>) =
        let spec = state.Query
        let updateColumns = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'UpdateProperties> updateFields (fun _ p -> p.Name)
        let coalesceColumns = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'CoalesceProperties> coalesceFields (fun _ p -> p.Name)
        let conflictColumns = pendingConflictFields "doUpdateCoalesceWrite" spec
        QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>(
            { spec with InsertType = OnConflictDoUpdateCoalesce (conflictColumns, updateColumns, coalesceColumns); PendingConflict = None },
            state.TableMappings)

type SelectBuilder<'Selected, 'Mapped> with

    /// Adds DISTINCT ON (col) — PostgreSQL-only. Mutually exclusive with `distinct`.
    /// Multiple `distinctOn` calls accumulate columns.
    [<CustomOperation("distinctOn", MaintainsVariableSpace = true)>]
    member this.DistinctOn (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] propertySelector: System.Linq.Expressions.Expression<Func<'T, 'Prop>>) =
        let ir = state.Query
        let column =
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function
                | LinqExpressionVisitors.OrderByColumn (alias, p) -> $"{alias}.{p.Name}"
                | _ -> failwith "distinctOn requires a simple column selector."
        QuerySource<'T, SelectQueryIR>({ ir with DistinctOn = ir.DistinctOn @ [column] }, state.TableMappings)

    /// LEFT JOIN LATERAL (subquery) AS alias ON true. PostgreSQL-only.
    /// The subquery is built with its own select { ... } and may correlate to outer columns.
    [<CustomOperation("lateralJoin", MaintainsVariableSpace = true)>]
    member this.LateralJoin (state: QuerySource<'T, SelectQueryIR>, subquery: SelectQuery, alias: string) =
        let ir = state.Query
        let joinClause =
            { Kind = LeftJoinLateral
              Table = alias
              Subquery = Some subquery.SelectIR
              Condition = WhereClause.RawWhere("true", [||]) }
        QuerySource<'T, SelectQueryIR>({ ir with Joins = ir.Joins @ [joinClause] }, state.TableMappings)

