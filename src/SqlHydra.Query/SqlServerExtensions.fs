module SqlHydra.Query.SqlServerExtensions

type InsertBuilder<'Inserted, 'InsertReturn> with

    /// Selects columns to "output" and sets the 'InsertReturn type accordingly. 
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

