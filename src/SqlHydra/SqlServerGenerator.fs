module SqlHydra.SqlServerGenrator
open Myriad.Core
open FSharp.Compiler.SyntaxTree
open FsAst
open System.Diagnostics
open Schema
open System.IO
    
module Gen = 
    let toRecord (tbl: Table, columns: Column list) = 
        let recordCmpInfo = 
            let myRecordId = LongIdentWithDots.CreateString tbl.Name
            SynComponentInfoRcd.Create(myRecordId.Lid)

        let recordDef =
            let rcrd = 
                columns
                |> List.choose (fun col -> SqlServerDataTypes.tryFindMapping col.DataType |> Option.map (fun mapping -> col, mapping))
                |> List.map (fun (col, mapping) -> 
                    let field = 
                        if mapping.ClrType = "byte[]" then 
                            let b = SynType.Create("byte")
                            SynType.Array(0, b, FSharp.Compiler.Range.range.Zero)
                        else
                            SynType.Create(mapping.ClrType)
                    
                    if col.IsNullable then                         
                        let opt = SynType.Option(field)
                        SynFieldRcd.Create(Ident.Create(col.ColumnName), opt)
                    else 
                        SynFieldRcd.Create(Ident.Create(col.ColumnName), field)
                )
                |> SynTypeDefnSimpleReprRecordRcd.Create

            SynTypeDefnSimpleReprRcd.Record rcrd
        
        SynModuleDecl.CreateSimpleType(recordCmpInfo, recordDef)


[<MyriadGenerator("sqlserver")>]
type SqlServerGenerator() =
    interface IMyriadGenerator with

        member __.ValidInputExtensions = 
            seq { ".toml" }

        member __.Generate(ctx: GeneratorContext) =

            // Get ssdt config
            let config = ctx.ConfigGetter "sqlserver" |> Map.ofSeq
            
            let connStr =
                config.TryFind("connection")
                |> Option.map string
                |> Option.defaultWith (fun () -> failwith "Unable to find 'sqlserver' 'connection' in myriad.toml.")

            let inputFile = FileInfo(ctx.InputFilename)
            let schemaPath = Path.Combine(inputFile.DirectoryName, "schema.json")

            let assembly = System.Reflection.Assembly.GetExecutingAssembly()
            let assemblyDir = FileInfo(assembly.Location).DirectoryName
            let exePath = Path.Combine(assemblyDir, "SqlHydra.exe")
            if not (File.Exists exePath) then failwithf "Unable to find provider: '%s'." exePath

            // Write json file
            let createSchemaFile() = 
                let msSqlProvider = ProcessStartInfo(exePath, sprintf "\"%s\" \"%s\"" connStr schemaPath)
                msSqlProvider.UseShellExecute <- false
                msSqlProvider.CreateNoWindow <- true
                msSqlProvider.WindowStyle <- ProcessWindowStyle.Hidden
                use exeProcess = Process.Start(msSqlProvider)
                exeProcess.WaitForExit()

            createSchemaFile()
            
            // Read schema
            let schema = Schema.deserialize (schemaPath)

            let columnsByTable =
                schema.Columns
                |> Array.toList
                |> List.groupBy (fun c -> c.TableCatalog, c.TableSchema, c.TableName)
                |> Map.ofList

            let recordsBySchema = 
                schema.Tables
                |> Array.toList
                |> List.groupBy (fun t -> t.Schema)
                |> List.map (fun (schema, tables) -> 
                    schema, 
                        tables 
                        |> List.map (fun table ->
                            let tableColumns = columnsByTable.[table.Catalog, table.Schema, table.Name]
                            Gen.toRecord (table, tableColumns)
                        )
                )
            
            let nestedModules = 
                recordsBySchema
                |> List.map (fun (schema, records) -> 
                    let moduleCmpInfo = SynComponentInfoRcd.Create [ Ident.Create schema ]
                    SynModuleDecl.CreateNestedModule(moduleCmpInfo, records)
                )

            
            // Get namespace
            let ns = 
                config.TryFind("namespace")
                |> Option.map string
                |> Option.defaultWith (fun () -> failwith "Unable to find 'sqlserver' 'namespace' in myriad.toml.")

            let namespaceOrModule =
                { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong ns)
                    with Declarations = nestedModules }

            [ namespaceOrModule ]
