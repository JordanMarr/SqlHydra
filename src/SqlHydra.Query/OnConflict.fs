/// Implementations for OnConflictDoUpdate, OnConflictDoNothing and InsertOrReplace.
module internal SqlHydra.Query.OnConflict

open System

/// Modifies an insert query to "INSERT OR REPLACE"
let insertOrReplace (cmdText: string) =
    cmdText.Replace("INSERT", "INSERT OR REPLACE")

/// Modifies an insert query to "ON CONFLICT TO UPDATE"
let onConflictDoUpdate (conflictColumns: string list) (updateColumns: string list) (query: SqlKata.Query) (cmdText: string) =
    // Separate insert query from optional identity query
    let insertQuery, identityQuery = 
        match cmdText.Split([| ";" |], StringSplitOptions.RemoveEmptyEntries) with
        | [| insertQuery; identityQuery |] -> insertQuery, identityQuery
        | _ -> cmdText, ""

    // Get insert clause from the SqlKata query
    let insertClause = 
        query.Clauses 
        |> Seq.choose (function | :? SqlKata.InsertClause as ic -> Some ic | _ -> None)
        |> Seq.head

    // Create a lookup of SqlKata insert column indexes by column name
    let getColumnIdxByName = 
        insertClause.Columns
        |> Seq.mapi (fun idx colNm -> colNm, idx)
        |> Map.ofSeq

    // Build upsert clause
    let setLinesStatement = 
        updateColumns
        |> List.map (fun colNm -> $"{colNm}=@p%i{getColumnIdxByName.[colNm]}\n")
        |> (fun lines -> String.Join(",", lines))
            
    let conflictColumnsCsv = String.Join(",", conflictColumns)

    Text.StringBuilder()
        .AppendLine(insertQuery)
        .AppendLine($"ON CONFLICT({conflictColumnsCsv}) DO UPDATE SET")
        .AppendLine(setLinesStatement).Append(";")
        .AppendLine(identityQuery)
        .ToString()

/// Modifies an insert query to "ON CONFLICT TO NOTHING"
let onConflictDoNothing (conflictColumns: string list) (cmdText: string) =
    // Separate insert query from optional identity query
    let insertQuery, identityQuery = 
        match cmdText.Split([| ";" |], StringSplitOptions.RemoveEmptyEntries) with
        | [| insertQuery; identityQuery |] -> insertQuery, identityQuery
        | _ -> cmdText, ""

    // Build upsert clause            
    let conflictColumnsCsv = String.Join(",", conflictColumns)
        
    Text.StringBuilder()
        .AppendLine(insertQuery)
        .AppendLine($"ON CONFLICT({conflictColumnsCsv})")
        .AppendLine("DO NOTHING;")
        .AppendLine(identityQuery)
        .ToString()


