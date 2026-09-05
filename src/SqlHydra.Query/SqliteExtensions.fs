module SqlHydra.Query.SqliteExtensions

open System

/// Common SQLite functions for use in select expressions.
/// Use `open type SqlFn` to access functions without qualification.
[<SqlHydraFunction>]
type SqlFn =
    // String functions
    static member length(s: string) : int = sqlFn
    static member upper(s: string) : string = sqlFn
    static member lower(s: string) : string = sqlFn
    static member ltrim(s: string) : string = sqlFn
    static member rtrim(s: string) : string = sqlFn
    static member trim(s: string) : string = sqlFn
    static member substr(s: string, start: int) : string = sqlFn
    static member substr(s: string, start: int, length: int) : string = sqlFn
    static member substring(s: string, start: int, length: int) : string = sqlFn
    static member replace(s: string, from: string, ``to``: string) : string = sqlFn
    static member instr(s: string, substring: string) : int = sqlFn
    static member printf(format: string, value: 'T) : string = sqlFn
    static member char(code: int) : string = sqlFn
    static member unicode(s: string) : int = sqlFn
    static member hex(value: 'T) : string = sqlFn
    static member quote(value: 'T) : string = sqlFn
    static member zeroblob(n: int) : byte[] = sqlFn

    // Null handling - with overloads for Option and Nullable
    static member ifnull(expr: Option<'T>, replacement: 'T) : 'T = sqlFn
    static member ifnull(expr: Nullable<'T>, replacement: 'T) : 'T when 'T : struct = sqlFn
    static member ifnull(expr: 'T, replacement: 'T) : 'T = sqlFn
    static member coalesce(a: Option<'T>, b: 'T) : 'T = sqlFn
    static member coalesce(a: Nullable<'T>, b: 'T) : 'T when 'T : struct = sqlFn
    static member coalesce(a: 'T, b: 'T) : 'T = sqlFn
    static member coalesce(a: 'T, b: 'T, c: 'T) : 'T = sqlFn
    static member nullif(a: 'T, b: 'T) : Option<'T> = sqlFn
    static member iif(condition: bool, trueValue: 'T, falseValue: 'T) : 'T = sqlFn

    // Numeric functions
    static member abs(n: 'T) : 'T when 'T : struct = sqlFn
    static member round(n: 'T) : 'T when 'T : struct = sqlFn
    static member round(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn
    static member sign(n: 'T) : int when 'T : struct = sqlFn
    static member max(a: 'T, b: 'T) : 'T when 'T : struct = sqlFn
    static member min(a: 'T, b: 'T) : 'T when 'T : struct = sqlFn
    static member random() : int64 = sqlFn

    // Date/time functions
    static member date(value: string) : string = sqlFn
    static member date(value: string, modifier: string) : string = sqlFn
    static member time(value: string) : string = sqlFn
    static member time(value: string, modifier: string) : string = sqlFn
    static member datetime(value: string) : string = sqlFn
    static member datetime(value: string, modifier: string) : string = sqlFn
    static member julianday(value: string) : float = sqlFn
    static member unixepoch(value: string) : int64 = sqlFn
    static member strftime(format: string, value: string) : string = sqlFn
    static member strftime(format: string, value: string, modifier: string) : string = sqlFn

    // Type functions
    static member typeof'(value: 'T) : string = sqlFn

type InsertBuilder<'Inserted, 'InsertReturn, 'Write when 'Write :> SqlHydra.IWriteOf<'Inserted>> with

    /// Performs an update on one or more update fields if a conflict occurs.
    [<CustomOperation("onConflictDoUpdate", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdate(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFieldsSelector, 
        [<ProjectionParameter>] updateFieldsSelector) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFieldsSelector (fun tblAlias p -> p.Name)
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'UpdateProperties> updateFieldsSelector (fun tblAlias p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoUpdate (conflictFields, updateFields) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// `onConflictDoUpdate` with the update fields selected over the write record, so a read-only column cannot be named.
    [<CustomOperation("onConflictDoUpdateWrite", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdateWrite(state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>,
        [<ProjectionParameter>] conflictFieldsSelector: System.Linq.Expressions.Expression<Func<'Inserted, 'ConflictProperty>>,
        updateFieldsSelector: System.Linq.Expressions.Expression<Func<'Write, 'UpdateProperties>>) =
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'Inserted, 'ConflictProperty> conflictFieldsSelector (fun _ p -> p.Name)
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'UpdateProperties> updateFieldsSelector (fun _ p -> p.Name)
        QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>(
            { spec with InsertType = OnConflictDoUpdate (conflictFields, updateFields) }, state.TableMappings)

    /// Insert is ignored if a conflict occurs.
    [<CustomOperation("onConflictDoNothing", MaintainsVariableSpace = true)>]
    member this.OnConflictDoNothing(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFieldsSelector) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFieldsSelector (fun tblAlias p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoNothing conflictFields }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// Deletes and re-inserts a record if a primary key conflict occurs.
    [<CustomOperation("insertOrReplace", MaintainsVariableSpace = true)>]
    member this.InsertOrReplace(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>) =
        let spec = state.Query
        let newSpec = { spec with InsertType = InsertOrReplace }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

