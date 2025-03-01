module SqlHydra.Query.NpgsqlExtensions

type InsertBuilder<'Inserted, 'InsertReturn> with
    
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

    /// Insert is ignored if a conflict occurs.
    [<CustomOperation("onConflictDoNothing", MaintainsVariableSpace = true)>]
    member this.OnConflictDoNothing(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFields) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitPropertiesSelector<'T, 'ConflictProperty> conflictFields (fun tblAlias p -> p.Name)
        let newSpec = { spec with InsertType = OnConflictDoNothing conflictFields }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

