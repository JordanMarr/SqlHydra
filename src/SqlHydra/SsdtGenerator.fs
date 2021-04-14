namespace Plugin

open Myriad.Core
open FSharp.Compiler.SyntaxTree
open FsAst
open DacpacParser

module Gen = 
    let toRecord (tbl: SsdtTable) = 
        let recordCmpInfo = 
            let myRecordId = LongIdentWithDots.CreateString tbl.Name
            SynComponentInfoRcd.Create(myRecordId.Lid)

        let recordDef =
            let rcrd = 
                tbl.Columns
                |> List.choose (fun col -> SqlServerDataTypes.tryFindMapping col.DataType |> Option.map (fun mapping -> col, mapping))
                |> List.map (fun (col, mapping) -> 
                    let field = 
                        if mapping.ClrType = "byte[]" then 
                            let b = SynType.Create("byte")
                            SynType.Array(0, b, FSharp.Compiler.Range.range.Zero)
                        else
                            SynType.Create(mapping.ClrType)
                    
                    if col.AllowNulls then                         
                        let opt = SynType.Option(field)
                        SynFieldRcd.Create(Ident.Create(col.Name), opt)
                    else 
                        SynFieldRcd.Create(Ident.Create(col.Name), field)
                )
                |> SynTypeDefnSimpleReprRecordRcd.Create

            SynTypeDefnSimpleReprRcd.Record rcrd
        
        SynModuleDecl.CreateSimpleType(recordCmpInfo, recordDef)

[<MyriadGenerator("ssdt")>]
type SsdtGenerator() =
    interface IMyriadGenerator with

        member __.ValidInputExtensions = 
            seq { ".dacpac" }

        member __.Generate(ctx: GeneratorContext) =

            let schema = 
                ctx.InputFileName
                |> extractModelXml
                |> parseXml
            
            let recordsBySchema = 
                schema.Tables
                |> List.groupBy (fun t -> t.Schema)
                |> List.map (fun (schema, tables) -> 
                    schema, tables |> List.map Gen.toRecord
                )
            
            let nestedModules = 
                recordsBySchema
                |> List.map (fun (schema, records) -> 
                    let moduleCmpInfo = SynComponentInfoRcd.Create [ Ident.Create schema ]
                    SynModuleDecl.CreateNestedModule(moduleCmpInfo, records)
                )

            // Get ssdt config
            let config = ctx.ConfigGetter "ssdt"
            // Get namespace
            let ns = 
                config 
                |> Seq.tryFind(fun (key, value) -> key = "namespace")
                |> Option.map (fun (key, value) -> string value)
                |> Option.defaultValue "Ssdt"

            let namespaceOrModule =
                { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong ns)
                    with Declarations = nestedModules }

            [ namespaceOrModule ]

