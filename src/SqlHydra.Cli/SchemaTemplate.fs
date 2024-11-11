module SqlHydra.SchemaTemplate

open Domain

let backticks = Fantomas.FCS.Syntax.PrettyNaming.NormalizeIdentifierBackticks

let newLine = "\n"

let mkIndent (tabs: int) (text: string) = 
    let spaces = tabs * 4
    let indent = String.replicate spaces " "
    text.Split('\n') |> Array.map (fun line -> indent + line) |> String.concat "\n"

let columnReadersModule = $"""
[<AutoOpen>]
module ColumnReaders =
    type Column(reader: System.Data.IDataReader, getOrdinal: string -> int, column) =
            member __.Name = column
            member __.IsNull() = getOrdinal column |> reader.IsDBNull
            override __.ToString() = __.Name

    type RequiredColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getter: int -> 'T, column) =
            inherit Column(reader, getOrdinal, column)
            member __.Read(?alias) = alias |> Option.defaultValue __.Name |> getOrdinal |> getter

    type OptionColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getter: int -> 'T, column) =
            inherit Column(reader, getOrdinal, column)
            member __.Read(?alias) = 
                match alias |> Option.defaultValue __.Name |> getOrdinal with
                | o when reader.IsDBNull o -> None
                | o -> Some (getter o)

    type NullableObjectColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getter: int -> 'T, column) =
            inherit Column(reader, getOrdinal, column)
            member __.Read(?alias) = 
                match alias |> Option.defaultValue __.Name |> getOrdinal with
                | o when reader.IsDBNull o -> null
                | o -> (getter o) |> unbox

    type NullableValueColumn<'T, 'Reader when 'T : struct and 'T : (new : unit -> 'T) and 'T :> System.ValueType and 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getter: int -> 'T, column) =
            inherit Column(reader, getOrdinal, column)
            member __.Read(?alias) = 
                match alias |> Option.defaultValue __.Name |> getOrdinal with
                | o when reader.IsDBNull o -> System.Nullable<'T>()
                | o -> System.Nullable<'T> (getter o)

[<AutoOpen>]
module private DataReaderExtensions =
    type System.Data.IDataReader with
        member reader.GetDateOnly(ordinal: int) = 
            reader.GetDateTime(ordinal) |> System.DateOnly.FromDateTime
    
    type System.Data.Common.DbDataReader with
        member reader.GetTimeOnly(ordinal: int) = 
            reader.GetFieldValue(ordinal) |> System.TimeOnly.FromTimeSpan
        """

let mkEnum db schema enum = stringBuffer {
    let enumType = 
        db.Enums 
        |> List.find (fun e -> e.Schema = schema && e.Name = enum)

    let labels = 
        enumType.Labels 
        |> List.sortBy _.SortOrder

    $"type {backticks enumType.Name} ="
    indent {
        for label in labels do
            $"| {backticks label.Name} = {label.SortOrder}"
    }
}

let mkTable cfg db (table: Table) schema = stringBuffer {
    let tableType = 
        db.Tables 
        |> List.find (fun t -> t.Schema = schema && t.Name = table.Name)

    if cfg.IsCLIMutable then "[<CLIMutable>]"

    $"type {backticks table.Name} ="
    indent {
        "{"
        indent {
            for col in tableType.Columns do
                let baseType = 
                    // Handles array types: "byte[]", "string[]", "int[]", "int []", "int array"
                    if col.TypeMapping.ClrType.EndsWith "[]" || col.TypeMapping.ClrType.EndsWith "array" then
                        let baseTypeNm = col.TypeMapping.ClrType.Split([| "[]"; " []"; " array" |], System.StringSplitOptions.RemoveEmptyEntries) |> Array.head
                        $"{baseTypeNm} []"
                    else
                        col.TypeMapping.ClrType

                let columnPropertyType =
                    if col.IsNullable then
                        match cfg.NullablePropertyType with
                        | NullablePropertyType.Option ->
                            $"Option<{baseType}>"
                        | NullablePropertyType.Nullable ->
                            $"System.Nullable<{baseType}>"
                    else 
                        baseType

                let providerDbTypeAttribute =
                    match col.TypeMapping.ProviderDbType with
                    | Some providerDbType when cfg.ProviderDbTypeAttributes -> 
                        Some $"[<ProviderDbType(\"{providerDbType}\")>]"
                    | _ -> 
                        None

                if providerDbTypeAttribute.IsSome then providerDbTypeAttribute.Value
                $"""{if cfg.IsMutableProperties then "mutable " else ""}{backticks col.Name}: {columnPropertyType}"""
        }
        "}"
    }
}

let generate (cfg: Config) (app: AppInfo) (db: Schema) (version: string) = stringBuffer {
    let filteredTables = 
        db.Tables 
        |> List.sortBy (fun tbl -> tbl.Schema, tbl.Name)

    let schemas = 
        let enumSchemas = db.Enums |> List.map (fun e -> e.Schema)
        let tableSchemas = filteredTables |> List.map (fun t -> t.Schema) 
        enumSchemas @ tableSchemas |> List.distinct

    $$"""
// This code was generated by `{{app.Name}}` -- v{{version}}.
namespace SqlServer.AdventureWorksNet6

open SqlHydra
open SqlHydra.Query.Table
"""

    if cfg.Readers.IsSome then 
        columnReadersModule

    for schema in schemas do
        $"module {backticks schema} ="

        let enums = 
            db.Enums 
            |> List.filter (fun e -> e.Schema = schema)
            |> List.map _.Name

        indent {
            for enum in enums do 
                mkEnum db schema enum
                newLine
        }

        let tables = 
            filteredTables 
            |> List.filter (fun t -> t.Schema = schema)

        indent {
            for table in tables do
                mkTable cfg db table schema
                newLine

                if cfg.TableDeclarations then 
                    $"let {backticks table.Name} = table<{backticks table.Name}>"
                    newLine
        }

        if cfg.Readers.IsSome then 
            indent {
                let reader = cfg.Readers.Value
                $"module Readers ="
                indent {
                    for table in tables do 
                        let readerClassName = $"{table.Name}Reader"
                        $"type {backticks readerClassName}(reader: {reader.ReaderType}, getOrdinal) ="
                        indent {
                            for col in table.Columns do 
                                let columnReaderType =
                                    if col.IsNullable then 
                                        match cfg.NullablePropertyType with
                                        | NullablePropertyType.Option ->
                                            "OptionColumn"                  // Returns None for DBNull.Value
                                        | NullablePropertyType.Nullable ->
                                            if col.TypeMapping.IsValueType() 
                                            then "NullableValueColumn"      // Returns System.Nullable<> for DBNull.Value
                                            else "NullableObjectColumn"     // Returns null for DBNull.Value
                                    else 
                                        "RequiredColumn"

                                $"member __.{backticks col.Name} = {columnReaderType}(reader, getOrdinal, reader.{col.TypeMapping.ReaderMethod}, \"{col.Name}\")"

                            "member __.Read() ="
                            indent {
                                "{"
                                indent {
                                    for col in table.Columns do 
                                        $"let {backticks col.Name} = __.{backticks col.Name}.Read()"
                                }
                                $$"""} : {{backticks table.Name}}"""
                            }

                            "member __.ReadIfNotNull() ="
                            indent {
                                $"if __.{backticks table.Columns.Head.Name}.IsNull() then None else Some(__.Read())"
                            }
                        }
                        newLine
                }
            }
}