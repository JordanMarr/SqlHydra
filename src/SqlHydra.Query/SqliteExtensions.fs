module SqlHydra.Query.SqliteExtensions

type InsertBuilder<'Inserted, 'InsertReturn> with

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

