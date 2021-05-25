open SqlHydra
open SqlHydra.SqlServer

open FSharp.Compiler.XmlDoc
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Range
open FsAst
open Fantomas

[<EntryPoint>]
let main argv =
    match argv with
    | [| connectionString; nmspace; outputFilePath |] -> 
        let schema = SqlServerSchemaProvider.getSchema connectionString

        let declarations = 
            schema.Tables
            |> Array.toList
            |> List.map SchemaGenerator.toRecord

        let xmlDoc = PreXmlDoc.Create [ ]
        let sm = SynModuleOrNamespace.SynModuleOrNamespace([ nmspace |> Ident.Create ], true, SynModuleOrNamespaceKind.DeclaredNamespace,declarations,  xmlDoc, [ ], None, range.Zero)
        let file = CodeGen.createFile [sm]
        let contents = CodeGen.formatAst file
        System.IO.File.WriteAllText(outputFilePath, contents)
        0

    | _ ->
        failwithf "Expected 'connectionString' and 'outputFilePath' args"
