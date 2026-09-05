module SqlHydra.Query.SqlServerExtensions

open System

/// Common SQL Server functions for use in select expressions.
/// Use `open type SqlFn` to access functions without qualification.
[<SqlHydraFunction>]
type SqlFn =
    // String functions
    static member LEN(s: string) : int = sqlFn
    static member DATALENGTH(s: string) : int = sqlFn
    static member UPPER(s: string) : string = sqlFn
    static member LOWER(s: string) : string = sqlFn
    static member LTRIM(s: string) : string = sqlFn
    static member RTRIM(s: string) : string = sqlFn
    static member TRIM(s: string) : string = sqlFn
    static member SUBSTRING(s: string, start: int, length: int) : string = sqlFn
    static member REPLACE(s: string, find: string, replacement: string) : string = sqlFn
    static member CHARINDEX(find: string, s: string) : int = sqlFn
    static member CHARINDEX(find: string, s: string, start: int) : int = sqlFn
    static member CONCAT(s1: string, s2: string) : string = sqlFn
    static member CONCAT(s1: string, s2: string, s3: string) : string = sqlFn
    static member CONCAT(s1: string, s2: string, s3: string, s4: string) : string = sqlFn
    static member CONCAT_WS(separator: string, s1: string, s2: string) : string = sqlFn
    static member CONCAT_WS(separator: string, s1: string, s2: string, s3: string) : string = sqlFn
    static member LEFT(s: string, length: int) : string = sqlFn
    static member RIGHT(s: string, length: int) : string = sqlFn
    static member REVERSE(s: string) : string = sqlFn
    static member REPLICATE(s: string, count: int) : string = sqlFn

    // Null handling - with overloads for Option and Nullable
    static member ISNULL(expr: Option<'T>, replacement: 'T) : 'T = sqlFn
    static member ISNULL(expr: Nullable<'T>, replacement: 'T) : 'T when 'T : struct = sqlFn
    static member ISNULL(expr: 'T, replacement: 'T) : 'T = sqlFn
    static member COALESCE(a: Option<'T>, b: 'T) : 'T = sqlFn
    static member COALESCE(a: Nullable<'T>, b: 'T) : 'T when 'T : struct = sqlFn
    static member COALESCE(a: 'T, b: 'T) : 'T = sqlFn
    static member COALESCE(a: 'T, b: 'T, c: 'T) : 'T = sqlFn
    static member NULLIF(a: 'T, b: 'T) : Option<'T> = sqlFn

    // Numeric functions
    static member ABS(n: 'T) : 'T when 'T : struct = sqlFn
    static member ROUND(n: 'T, decimals: int) : 'T when 'T : struct = sqlFn
    static member CEILING(n: 'T) : 'T when 'T : struct = sqlFn
    static member FLOOR(n: 'T) : 'T when 'T : struct = sqlFn
    static member SIGN(n: 'T) : int when 'T : struct = sqlFn
    static member POWER(n: 'T, exponent: 'T) : 'T when 'T : struct = sqlFn
    static member SQRT(n: 'T) : float when 'T : struct = sqlFn

    // Date/time functions
    static member GETDATE() : DateTime = sqlFn
    static member GETUTCDATE() : DateTime = sqlFn
    static member SYSDATETIME() : DateTime = sqlFn
    static member SYSUTCDATETIME() : DateTime = sqlFn
    static member DATEPART(part: string, date: DateTime) : int = sqlFn
    static member DATEADD(part: string, number: int, date: DateTime) : DateTime = sqlFn
    static member DATEDIFF(part: string, startDate: DateTime, endDate: DateTime) : int = sqlFn
    static member YEAR(date: DateTime) : int = sqlFn
    static member MONTH(date: DateTime) : int = sqlFn
    static member DAY(date: DateTime) : int = sqlFn
    static member EOMONTH(date: DateTime) : DateTime = sqlFn
    static member EOMONTH(date: DateTime, months: int) : DateTime = sqlFn

/// SQL Server specific extensions for the insert builder.
type InsertBuilder<'Inserted, 'InsertReturn, 'Write when 'Write :> SqlHydra.IWriteOf<'Inserted>> with

    /// Performs an insert-first upsert. On duplicate key (PK/UNIQUE violation), updates the specified columns.
    [<CustomOperation("insertOrUpdateOnUnique", MaintainsVariableSpace = true)>]
    member this.InsertOrUpdateOnUnique(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>,
        [<ProjectionParameter>] keyFieldsSelector,
        [<ProjectionParameter>] updateFieldsSelector) =

        let spec = state.Query
        let keyFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'KeyProperty> keyFieldsSelector (fun tblAlias p -> p.Name)
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'UpdateProperties> updateFieldsSelector (fun tblAlias p -> p.Name)
        let newSpec = { spec with InsertType = InsertOrUpdateOnUnique (keyFields, updateFields) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    /// `insertOrUpdateOnUnique` with the update fields selected over the write record, so a read-only column cannot be named.
    [<CustomOperation("insertOrUpdateOnUniqueWrite", MaintainsVariableSpace = true)>]
    member this.InsertOrUpdateOnUniqueWrite(state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>,
        [<ProjectionParameter>] keyFieldsSelector: System.Linq.Expressions.Expression<Func<'Inserted, 'KeyProperty>>,
        updateFieldsSelector: System.Linq.Expressions.Expression<Func<'Write, 'UpdateProperties>>) =
        let spec = state.Query
        let keyFields = LinqExpressionVisitors.visitPropertiesSelector<'Inserted, 'KeyProperty> keyFieldsSelector (fun _ p -> p.Name)
        let updateFields = LinqExpressionVisitors.visitPropertiesSelector<'Write, 'UpdateProperties> updateFieldsSelector (fun _ p -> p.Name)
        QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>(
            { spec with InsertType = InsertOrUpdateOnUnique (keyFields, updateFields) }, state.TableMappings)

    /// Selects columns to output from the insert statement.
    [<CustomOperation("output", MaintainsVariableSpace = true)>]
    member this.Output (state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, [<ProjectionParameter>] selectExpression) = 
        let spec = state.Query

        let selections = LinqExpressionVisitors.visitSelect<'T,'InsertReturn> selectExpression
        let newSpec =
            selections
            |> List.choose (function 
                | LinqExpressionVisitors.SelectedColumn (tableAlias, column, columnType, isOpt, isNullable) -> 
                    Some (tableAlias, column, columnType, isOpt, isNullable)
                | _ ->
                    None
            )
            |> List.fold (fun (spec: InsertQuerySpec<'T, 'InsertReturn>) (_, column, propertyType, isOptional, isNullable) -> 
                let nullability = if isOptional then IsOptional elif isNullable then IsNullable else NotNullable
                let outputField = { ColumnName = column; PropertyType = propertyType; Nullability = nullability }
                { spec with OutputFields = spec.OutputFields @ [outputField ] }
            ) spec
              
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

/// SQL Server specific extensions for the update builder.
type UpdateBuilder<'Updated, 'UpdateReturn> with

    /// Selects columns to output from the update statement.
    [<CustomOperation("output", MaintainsVariableSpace = true)>]
    member this.Output (state: QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>, [<ProjectionParameter>] selectExpression) = 
        let spec = state.Query

        let selections = LinqExpressionVisitors.visitSelect<'T, 'UpdateReturn> selectExpression
        let newSpec =
            selections
            |> List.choose (function 
                | LinqExpressionVisitors.SelectedColumn (tableAlias, column, columnType, isOpt, isNullable) -> 
                    Some (tableAlias, column, columnType, isOpt, isNullable)
                | _ ->
                    None
            )
            |> List.fold (fun (spec: UpdateQuerySpec<'T, 'UpdateReturn>) (_, column, propertyType, isOptional, isNullable) -> 
                let nullability = if isOptional then IsOptional elif isNullable then IsNullable else NotNullable
                let outputField = { ColumnName = column; PropertyType = propertyType; Nullability = nullability }
                { spec with OutputFields = spec.OutputFields @ [outputField ] }
            ) spec
              
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>(newSpec, state.TableMappings)
