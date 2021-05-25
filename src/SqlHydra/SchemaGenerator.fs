module SqlHydra.SchemaGenerator
open FSharp.Compiler.SyntaxTree
open FsAst
open Fantomas
open Schema
    
let toRecord (tbl: Table) = 
    let recordCmpInfo = 
        let myRecordId = LongIdentWithDots.CreateString tbl.Name
        SynComponentInfoRcd.Create(myRecordId.Lid)

    let recordDef =
        let rcrd = 
            tbl.Columns
            |> Array.map (fun col -> 
                let field = 
                    if col.ClrType = "byte[]" then 
                        let b = SynType.Create("byte")
                        SynType.Array(0, b, FSharp.Compiler.Range.range.Zero)
                    else
                        SynType.Create(col.ClrType)
                    
                if col.IsNullable then                         
                    let opt = SynType.Option(field)
                    SynFieldRcd.Create(Ident.Create(col.Name), opt)
                else 
                    SynFieldRcd.Create(Ident.Create(col.Name), field)
            )
            |> Array.toList
            |> SynTypeDefnSimpleReprRecordRcd.Create

        SynTypeDefnSimpleReprRcd.Record rcrd
        
    SynModuleDecl.CreateSimpleType(recordCmpInfo, recordDef)

let generateModule (ns: string) (schema: Schema) = 
    let recordsBySchema = 
        schema.Tables
        |> Array.toList
        |> List.groupBy (fun t -> t.Schema)
        |> List.map (fun (schema, tables) -> schema, tables |> List.map toRecord)
    
    let nestedModules = 
        recordsBySchema
        |> List.map (fun (schema, records) -> 
            let moduleCmpInfo = SynComponentInfoRcd.Create [ Ident.Create schema ]
            SynModuleDecl.CreateNestedModule(moduleCmpInfo, records)
        )

    let namespaceOrModule =
        { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong ns)
            with Declarations = nestedModules }

    namespaceOrModule

let toFormattedCode (outputFilePath: string) (comment: string) (generatedModule: SynModuleOrNamespaceRcd) = 
        let parsedInput = 
            ParsedInput.CreateImplFile(
                ParsedImplFileInputRcd.CreateFs(outputFilePath).AddModule generatedModule)
    
        let cfg = { FormatConfig.FormatConfig.Default with StrictMode = true }
        let formattedCode = CodeFormatter.FormatASTAsync(parsedInput, "output.fs", [], None, cfg) |> Async.RunSynchronously
    
        let formattedCodeWithComment =
            [   comment
                formattedCode ]
            |> String.concat System.Environment.NewLine

        formattedCodeWithComment
