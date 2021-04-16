module SqlHydra.SqlServerGenrator
open Myriad.Core
open FSharp.Compiler.SyntaxTree
open FsAst

module Schema = 
    open Microsoft.Data.SqlClient
    open System.Data
    open Microsoft.Data.SqlClient
    open System
    open System.Data
    
    type Table = {
        Catalog: string
        Schema: string
        Name: string
        Type: string
    }
    
    type Column = {
        TableCatalog: string
        TableSchema: string
        TableName: string
        ColumnName: string
        DataType: string
        IsNullable: bool
    }
    
    let getSchema(connectionString: string) =         
        use conn = new SqlConnection(connectionString)
        conn.Open()
        let sTables = conn.GetSchema("Tables")
        let sColumns = conn.GetSchema("Columns")
    
        let tables = 
            sTables.Rows
            |> Seq.cast<DataRow>
            |> Seq.map (fun r -> 
                { Catalog = r.["TABLE_CATALOG"] :?> string
                  Schema = r.["TABLE_SCHEMA"] :?> string
                  Name = r.["TABLE_NAME"] :?> string
                  Type = r.["TABLE_TYPE"] :?> string }
            )
            |> Seq.toList
    
        let columns = 
            sColumns.Rows
            |> Seq.cast<DataRow>
            |> Seq.map (fun r -> 
                { TableCatalog = r.["TABLE_CATALOG"] :?> string
                  TableSchema = r.["TABLE_SCHEMA"] :?> string
                  TableName = r.["TABLE_NAME"] :?> string
                  ColumnName = r.["COLUMN_NAME"] :?> string
                  DataType = r.["DATA_TYPE"] :?> string
                  IsNullable = 
                    match r.["IS_NULLABLE"] :?> string with 
                    | "YES" -> true
                    | _ -> false
                }
            )
            |> Seq.toList

        tables, columns

module Gen = 
    let toRecord (tbl: Schema.Table, columns: Schema.Column list) = 
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
            let let42 =
                SynModuleDecl.CreateLet
                    [ { SynBindingRcd.Let with
                            Pattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString "fourtyTwo", [])
                            Expr = SynExpr.CreateConst(SynConst.Int32 42) } ]

            let componentInfo = SynComponentInfoRcd.Create [ Ident.Create "example1" ]
            let nestedModule = SynModuleDecl.CreateNestedModule(componentInfo, [ let42 ])

            let namespaceOrModule =
                { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong "TestNS")
                    with Declarations = [ nestedModule ] }

            [ namespaceOrModule ]

        //member __.Generate(ctx: GeneratorContext) =
            //let cs = "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
            //let tables, columns = Schema.getSchema cs
            
            //let columnsByTable =
            //    columns
            //    |> List.groupBy (fun c -> c.TableCatalog, c.TableSchema, c.TableName)
            //    |> Map.ofList

            //let recordsBySchema = 
            //    tables
            //    |> List.groupBy (fun t -> t.Schema)
            //    |> List.map (fun (schema, tables) -> 
            //        schema, 
            //            tables 
            //            |> List.map (fun table ->
            //                let tableColumns = columnsByTable.[table.Catalog, table.Schema, table.Name]
            //                Gen.toRecord (table, tableColumns)
            //            )
            //    )
            
            //let nestedModules = 
            //    recordsBySchema
            //    |> List.map (fun (schema, records) -> 
            //        let moduleCmpInfo = SynComponentInfoRcd.Create [ Ident.Create schema ]
            //        SynModuleDecl.CreateNestedModule(moduleCmpInfo, records)
            //    )

            //// Get ssdt config
            //let config = ctx.ConfigGetter "ssdt"
            //// Get namespace
            //let ns = 
            //    config 
            //    |> Seq.tryFind(fun (key, value) -> key = "namespace")
            //    |> Option.map (fun (key, value) -> string value)
            //    |> Option.defaultValue "Ssdt"

            //let namespaceOrModule =
            //    { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong ns)
            //        with Declarations = nestedModules }

            //[ namespaceOrModule ]