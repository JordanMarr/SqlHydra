module SqlHydra.SchemaGeneratorFab

open Domain

open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.XmlDoc
open FsAst
open Fantomas
open Domain
open System.Data
open SqlHydra.SchemaFilters

open Fantomas.Core
open Fantomas.Core.SyntaxOak
open Fantomas.FCS.Text

open Fabulous.AST
open type Ast

let range0 = range.Zero

let backticks = Fantomas.FCS.Syntax.PrettyNaming.NormalizeIdentifierBackticks

/// Generates the outer module and table records.
let generateModule (cfg: Config) (app: AppInfo) (db: Schema) = 
    let filteredTables = 
        db.Tables 
        |> List.sortBy (fun tbl -> tbl.Schema, tbl.Name)

    let schemas = 
        let enumSchemas = db.Enums |> List.map (fun e -> e.Schema)
        let tableSchemas = filteredTables |> List.map (fun t -> t.Schema) 
        enumSchemas @ tableSchemas |> List.distinct
    
    Namespace(cfg.Namespace) {

        if cfg.Readers.IsSome then 
            Open("Substitue.ColumnReadersModule")

        for schema in schemas do
            let tables = 
                filteredTables 
                |> List.filter (fun t -> t.Schema = schema)
                |> List.map (fun t -> t.Name)

            let enums = 
                db.Enums 
                |> List.filter (fun e -> e.Schema = schema)
                |> List.map (fun e -> e.Name)

            NestedModule(schema) {
                for enum in enums do
                    let enumType = 
                        db.Enums 
                        |> List.find (fun e -> e.Schema = schema && e.Name = enum)

                    let labels = 
                        enumType.Labels 
                        |> List.sortBy _.SortOrder
                                            
                    Enum(backticks enum) {
                        for label in labels do
                            EnumCase(backticks label.Name, string label.SortOrder)
                    }

                for table in tables do
                    let tableType = 
                        db.Tables 
                        |> List.find (fun t -> t.Schema = schema && t.Name = table)

                    
                    let tableRecord = 
                        Record(table) {
                        
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

                                let field = Field(col.Name, columnPropertyType)
                                match col.TypeMapping.ProviderDbType with
                                | Some providerDbType -> 
                                    field.attribute(Attribute($"SqlHydra.ProviderDbType(\"{providerDbType}\")"))
                                | _ -> 
                                    field

                        }

                    if cfg.IsCLIMutable 
                    then tableRecord.attribute(Attribute("CLIMutable"))
                    else tableRecord

                    if cfg.TableDeclarations then
                        Value(table, $"SqlHydra.Query.Table.table<{backticks table}>", false)
            }
    }

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

/// A list of static code text substitutions to the generated file.
let substitutions (app: AppInfo) : (string * string) list = 
    [
        "open Substitue.ColumnReadersModule", columnReadersModule
    ]

open Fantomas
open Fantomas.Core

/// Formats the generated code using Fantomas.
let toFormattedCode (cfg: Config) (app: AppInfo) (version: string) (ast: WidgetBuilder<SyntaxOak.Oak>) = 
    let comment = $"// This code was generated by `{app.Name}` -- v{version}."

    //let cfg = 
    //    { FormatConfig.FormatConfig.Default with 
    //        StrictMode = true
    //        MaxIfThenElseShortWidth = 400   // Forces ReadIfNotNull if/then to be on a single line
    //        MaxValueBindingWidth = 400      // Ensure reader property/column bindings stay on one line
    //        MaxLineLength = 400             // Ensure reader property/column bindings stay on one line
    //    }

    let formattedCode = 
        ast
        |> Gen.mkOak
        |> CodeFormatter.FormatOakAsync
        |> Async.RunSynchronously

    let finalCode = substitutions app |> List.fold (fun (code: string) (placeholder, sub) -> code.Replace(placeholder, sub)) formattedCode

    let formattedCodeWithComment =
        [   
            comment
            
            //if cfg.Readers.IsSome then
            //    columnReadersModule

            finalCode
        ]
        |> String.concat System.Environment.NewLine

    formattedCodeWithComment
