module SqlHydra.Query.SqlServerExtensions

/// SQL Server specific extensions for the insert builder.
type InsertBuilder<'Inserted, 'InsertReturn> with

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
