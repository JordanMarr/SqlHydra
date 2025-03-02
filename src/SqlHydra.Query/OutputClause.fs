/// Provides support for the SQL Server OUTPUT clause , which allows you to return values from inserted or deleted rows in an INSERT, UPDATE, or DELETE statement.
module internal SqlHydra.Query.OutputClause

open System
open System.Data.Common
open System.Threading

let inserted (outputFields: OutputField list) (cmdText: string) = 
    let valuesIndex = cmdText.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase)
    let outputCsv = 
        outputFields
        |> List.map (fun f -> $"INSERTED.{f.ColumnName}")
        |> String.concat ", "
    let outputClause = $"\nOUTPUT {outputCsv}\n"
    cmdText.Insert(valuesIndex, outputClause)

let updated (outputFields: OutputField list) (cmdText: string) = 
    let whereIndex = cmdText.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase)
    let outputCsv = 
        outputFields
        |> List.map (fun f -> $"INSERTED.{f.ColumnName}")
        |> String.concat ", "
    let outputClause = $"\nOUTPUT {outputCsv}\n"
    if whereIndex > -1 
    then cmdText.Insert(whereIndex, outputClause)
    else cmdText + outputClause

let readValues<'InsertReturn> (cmd: DbCommand) (cancel: CancellationToken) (outputFields: OutputField list) =
    task {
        use! reader = cmd.ExecuteReaderAsync(cancel)
        let! _ = reader.ReadAsync(cancel)

        let outputValues = 
            outputFields
            |> List.map (fun f -> 
                let ord = reader.GetOrdinal(f.ColumnName)
                match f.Nullability with
                | NotNullable -> 
                    reader[ord]
                | IsOptional -> 
                    if reader.IsDBNull(ord) 
                    then None
                    else Activator.CreateInstance(f.PropertyType, [| reader[ord] |])
                | IsNullable ->
                    if reader.IsDBNull(ord) 
                    then Nullable() |> box
                    else Activator.CreateInstance(f.PropertyType, [| reader[ord] |])
            )
            |> List.toArray

        let outputTypes = 
            outputFields
            |> List.map _.PropertyType
            |> List.toArray

        match outputValues with 
        | [| outputValue |] -> 
            return outputValue :?> 'InsertReturn
        | outputValues -> 
            // Convert array to a tuple
            let outputTupleType = FSharp.Reflection.FSharpType.MakeTupleType(outputTypes)
            let outputTuple = FSharp.Reflection.FSharpValue.MakeTuple(outputValues, outputTupleType)
            return outputTuple :?> 'InsertReturn
    }