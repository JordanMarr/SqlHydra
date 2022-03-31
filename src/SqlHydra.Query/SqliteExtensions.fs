module SqlHydra.Query.SqliteExtensions

type InsertBuilder<'Inserted, 'InsertReturn when 'InsertReturn : struct> with

    [<CustomOperation("onConflictDoUpdate", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdate(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFieldsSelector, 
        [<ProjectionParameter>] updateFieldsSelector) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitGroupBy<'T, 'ConflictProperty> conflictFieldsSelector (fun p -> p.Name)
        let updateFields = LinqExpressionVisitors.visitGroupBy<'T, 'UpdateProperties> updateFieldsSelector (fun p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoUpdate (conflictFields, updateFields) }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    [<CustomOperation("onConflictDoNothing", MaintainsVariableSpace = true)>]
    member this.OnConflictDoNothing(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFieldsSelector) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitGroupBy<'T, 'ConflictProperty> conflictFieldsSelector (fun p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoNothing conflictFields }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    [<CustomOperation("insertOrReplace", MaintainsVariableSpace = true)>]
    member this.InsertOrReplace(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>) =
        let spec = state.Query
        let newSpec = { spec with InsertType = InsertOrReplace }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)


type QueryContext with

    member this.InsertOrReplace (iq: InsertQuery<'T, 'ReturnValue>) = 
        OnConflict.insertOrReplace this iq
        
    /// Transforms a regular INSERT query into an UPSERT by appending "ON CONFLICT DO UPDATE".
    /// NOTE: This can only be called on one record at a time.
    member this.OnConflictDoUpdate (conflictColumns: string list) (columnsToUpdate: string list) (iq: InsertQuery<'T, 'ReturnValue>) =
        OnConflict.onConflictDoUpdate this conflictColumns columnsToUpdate iq

    /// Transforms a regular INSERT query into an INSERT or IGNORE by appending "ON CONFLICT DO NOTHING".
    /// NOTE: This can only be called on one record at a time.
    member this.OnConflictDoNothing (conflictColumns: string list) (iq: InsertQuery<'T, 'ReturnValue>) =
        OnConflict.onConflictDoNothing this conflictColumns iq