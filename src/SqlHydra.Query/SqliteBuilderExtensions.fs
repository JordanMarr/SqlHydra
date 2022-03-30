[<AutoOpen>]
module SqlHydra.Query.SqliteExtensions.SqliteBuilderExtensions

open SqlHydra.Query

type InsertBuilder<'Inserted, 'InsertReturn when 'InsertReturn : struct> with
    
    [<CustomOperation("onConflictDoUpdate", MaintainsVariableSpace = true)>]
    member this.OnConflictDoUpdate(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFieldsSelector, 
        [<ProjectionParameter>] updateFieldsSelector) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitGroupBy<'T, 'ConflictProperty> conflictFieldsSelector (FQ.fullyQualifyColumn state.TableMappings)
        let updateFields = LinqExpressionVisitors.visitGroupBy<'T, 'UpdateProperties> updateFieldsSelector (FQ.fullyQualifyColumn state.TableMappings)
        let newSpec = { spec with OnConflictDoUpdate = Some { ConflictFields = conflictFields; UpdateFields = updateFields } }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    [<CustomOperation("onConflictDoNothing", MaintainsVariableSpace = true)>]
    member this.OnConflictDoNothing(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, 
        [<ProjectionParameter>] conflictFieldsSelector) = 
        
        let spec = state.Query
        let conflictFields = LinqExpressionVisitors.visitGroupBy<'T, 'ConflictProperty> conflictFieldsSelector (FQ.fullyQualifyColumn state.TableMappings)
        let newSpec = { spec with OnConflictDoNothing = Some { ConflictFields = conflictFields } }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

    [<CustomOperation("insertOrReplace", MaintainsVariableSpace = true)>]
    member this.InsertOrReplace(state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>) =
        let spec = state.Query
        let newSpec = { spec with InsertOrReplace = true }
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)

