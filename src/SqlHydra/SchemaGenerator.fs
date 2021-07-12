module SqlHydra.SchemaGenerator
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler
open FsAst
open Fantomas
open Schema
open Range

let cliMutableAttribute = 
    let attr =
        { TypeName = LongIdentWithDots.CreateString "CLIMutable"
        ; ArgExpr = SynExpr.CreateUnit
        ; Target = None
        ; AppliesToGetterAndSetter = false
        ; Range = range.Zero } : SynAttribute

    let atts = [ SynAttributeList.Create(attr) ]
    SynModuleDecl.CreateAttributes(atts)
    
let tableRecord (tbl: Table) = 
    let myRecordId = LongIdentWithDots.CreateString tbl.Name
    let recordCmpInfo = SynComponentInfoRcd.Create(myRecordId.Lid)
    
    let recordDef =
        tbl.Columns
        |> Array.map (fun col -> 
            let field = 
                if col.ClrType = "byte[]" then 
                    let b = SynType.Create("byte")
                    SynType.Array(0, b, range.Zero)
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
        |> SynTypeDefnSimpleReprRcd.Record
        
    SynModuleDecl.CreateSimpleType(recordCmpInfo, recordDef)

let tableReaderClass (tbl: Table) = 
    let classId = Ident.CreateLong(tbl.Name + "Reader")
    let classCmpInfo = SynComponentInfo.ComponentInfo(SynAttributes.Empty, [], [], classId, XmlDoc.PreXmlDocEmpty, false, None, range.Zero)

    let ctor = SynMemberDefn.CreateImplicitCtor([ 
        SynSimplePat.CreateTyped(Ident.Create("reader"), SynType.CreateLongIdent("System.Data.IDataReader"))
    ])

    let props =
        tbl.Columns
        |> Array.toList
        |> List.map (fun col -> 
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
                , SynExpr.App(
                    ExprAtomicFlag.Atomic
                    , false
                    , SynExpr.LongIdent(false, LongIdentWithDots.CreateString("reader.GetString"), None, range.Zero)
                    , SynExpr.CreateParen(
                        SynExpr.App(
                            ExprAtomicFlag.Atomic
                            , false
                            , SynExpr.LongIdent(false, LongIdentWithDots.CreateString("reader.GetOrdinal"), None, range.Zero)
                            , SynExpr.CreateConstString(col.Name)
                            , range.Zero 
                        )
                    )
                    , range.Zero 
                )
                , None
                , range.Zero)
        )

    let members =  ctor :: props

    let typeRepr = SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconUnspecified, members, range.Zero)

    let readerClass = 
        SynTypeDefn.TypeDefn(
            classCmpInfo,
            typeRepr,
            SynMemberDefns.Empty,
            range.Zero)
    
    SynModuleDecl.Types([ readerClass ], range.Zero)

let generateModule (cfg: Config) (db: Schema) = 
    let schemas = db.Tables |> Array.map (fun t -> t.Schema) |> Array.distinct
    
    let nestedModules = 
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
            with Declarations = nestedModules }

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
