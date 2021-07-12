module SqlHydra.SchemaGenerator
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler
open FsAst
open Fantomas
open Schema
open System.Data

let range0 = Range.range.Zero

let cliMutableAttribute = 
    let attr =
        { TypeName = LongIdentWithDots.CreateString "CLIMutable"
        ; ArgExpr = SynExpr.CreateUnit
        ; Target = None
        ; AppliesToGetterAndSetter = false
        ; Range = range0 } : SynAttribute

    let atts = [ SynAttributeList.Create(attr) ]
    SynModuleDecl.CreateAttributes(atts)
    
let tableRecord (tbl: Table) = 
    let myRecordId = LongIdentWithDots.CreateString tbl.Name
    let recordCmpInfo = SynComponentInfoRcd.Create(myRecordId.Lid)
    
    let recordDef =
        tbl.Columns
        |> Array.map (fun col -> 
            let field = 
                if col.TypeMapping.ClrType = "byte[]" then 
                    let b = SynType.Create("byte")
                    SynType.Array(0, b, range0)
                else
                    SynType.Create(col.TypeMapping.ClrType)
                    
            if col.IsNullable then                         
                let opt = SynType.Option(field)
                SynFieldRcd.Create(Ident.Create(col.Name), opt)
            else 
                SynFieldRcd.Create(Ident.Create(col.Name), field)
        )
        |> Array.toList
        |> SynTypeDefnSimpleReprRecordRcd.Create
        |> SynTypeDefnSimpleReprRcd.Record
        
    SynModuleDecl.CreateSimpleType(recordCmpInfo, recordDef)

let tableReaderClass (tbl: Table) = 
    let classId = Ident.CreateLong(tbl.Name + "Reader")
    let classCmpInfo = SynComponentInfo.ComponentInfo(SynAttributes.Empty, [], [], classId, XmlDoc.PreXmlDocEmpty, false, None, range0)

    let ctor = SynMemberDefn.CreateImplicitCtor([ 
        SynSimplePat.CreateTyped(Ident.Create("reader"), SynType.CreateLongIdent("System.Data.IDataReader"))
    ])

    let downcastToBytes expr = SynExpr.Downcast(expr, SynType.Array(0, SynType.Byte(), range0), range0)

    let props =
        tbl.Columns
        |> Array.toList
        |> List.map (fun col ->
            let readerCall = 
                SynExpr.App(
                    ExprAtomicFlag.Atomic
                    , false
                    , SynExpr.LongIdent(
                        false
                        , LongIdentWithDots.CreateString($"reader.{col.TypeMapping.ReaderMethod}")
                        , None
                        , range0)
                    , SynExpr.CreateParen(
                        SynExpr.App(
                            ExprAtomicFlag.Atomic
                            , false
                            , SynExpr.LongIdent(false, LongIdentWithDots.CreateString("reader.GetOrdinal"), None, range0)
                            , SynExpr.CreateConstString(col.Name)
                            , range0 
                        )
                    )
                    , range0 
                )

            SynMemberDefn.AutoProperty(
                []
                , false
                , Ident.Create(col.Name)
                , None
                , MemberKind.PropertyGet
                , (fun mk -> 
                    {
                        MemberFlags.IsInstance = true
                        MemberFlags.IsDispatchSlot = false
                        MemberFlags.IsFinal = false
                        MemberFlags.IsOverrideOrExplicitImpl = false
                        MemberFlags.MemberKind = MemberKind.PropertySet
                    })
                , XmlDoc.PreXmlDocEmpty
                , None
                , if col.TypeMapping.DbType = DbType.Binary
                  then downcastToBytes readerCall
                  else readerCall
                , None
                , range0)
        )

    let members =  ctor :: props

    let typeRepr = SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconUnspecified, members, range0)

    let readerClass = 
        SynTypeDefn.TypeDefn(
            classCmpInfo,
            typeRepr,
            SynMemberDefns.Empty,
            range0)
    
    SynModuleDecl.Types([ readerClass ], range0)

let generateModule (cfg: Config) (db: Schema) = 
    let schemas = db.Tables |> Array.map (fun t -> t.Schema) |> Array.distinct
    
    let nestedSchemaModules = 
        schemas
        |> Array.toList
        |> List.map (fun schema -> 
            let schemaNestedModule = SynComponentInfoRcd.Create [ Ident.Create schema ]

            let tables = db.Tables |> Array.filter (fun t -> t.Schema = schema)
            let tableRecords = tables |> Array.map tableRecord
            let readerClasses = tables |> Array.map tableReaderClass
            let zip = Array.zip tableRecords readerClasses

            let tableRecordDeclarations = 
                [ 
                    for (record, reader) in zip do 
                        if cfg.IsCLIMutable then yield cliMutableAttribute
                        yield record
                        
                        yield reader
                ]

            SynModuleDecl.CreateNestedModule(schemaNestedModule, tableRecordDeclarations)
        )

    let namespaceOrModule =
        { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong cfg.Namespace)
            with Declarations = nestedSchemaModules }

    namespaceOrModule

let toFormattedCode (cfg: Config) (comment: string) (generatedModule: SynModuleOrNamespaceRcd) = 
        let parsedInput = 
            ParsedInput.CreateImplFile(
                ParsedImplFileInputRcd.CreateFs(cfg.OutputFile).AddModule generatedModule)
    
        let cfg = { FormatConfig.FormatConfig.Default with StrictMode = true }
        let formattedCode = CodeFormatter.FormatASTAsync(parsedInput, "output.fs", [], None, cfg) |> Async.RunSynchronously
    
        let formattedCodeWithComment =
            [   comment
                formattedCode ]
            |> String.concat System.Environment.NewLine

        formattedCodeWithComment
