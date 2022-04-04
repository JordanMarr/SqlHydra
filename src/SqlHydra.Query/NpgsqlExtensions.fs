module SqlHydra.Query.NpgsqlExtensions

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

