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

    let props =
        tbl.Columns
        |> Array.toList
        |> List.map (fun col ->
            let readerCall = 
                SynExpr.App(
                    ExprAtomicFlag.Atomic
                    , false
                    
                    // Function:
                    , SynExpr.LongIdent(
                        false
                        , LongIdentWithDots.CreateString(
                            match col.TypeMapping.DbType, col.IsNullable with
                            | DbType.Binary, true -> "reader.OptionalBinary"
                            | DbType.Binary, false -> "reader.RequiredBinary"
                            | _, true -> "reader.Optional"
                            | _, false -> "reader.Required"
                        )
                        , None
                        , range0)
                    
                    // Args:
                    , SynExpr.CreateParenedTuple([
                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString($"reader.{col.TypeMapping.ReaderMethod}"), None)
                        SynExpr.CreateConstString(col.Name)
                    ])

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
                        MemberFlags.MemberKind = MemberKind.PropertyGet
                    })
                , XmlDoc.PreXmlDocEmpty
                , None
                , readerCall
                , None
                , range0)
        )

    let memberFlags : MemberFlags = {IsInstance = true; IsDispatchSlot = false; IsOverrideOrExplicitImpl = false; IsFinal = false; MemberKind = MemberKind.Member}
    let readMethod = 
        SynMemberDefn.CreateMember(
            {   
                SynBindingRcd.Access = None
                SynBindingRcd.Kind = SynBindingKind.NormalBinding
                SynBindingRcd.IsInline = false
                SynBindingRcd.IsMutable = false
                SynBindingRcd.Attributes = SynAttributes.Empty
                SynBindingRcd.XmlDoc = XmlDoc.PreXmlDocEmpty
                SynBindingRcd.ValData = SynValData.SynValData(Some memberFlags, SynValInfo.Empty, None)
                SynBindingRcd.Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("__.Read")
                            , SynArgPats.Pats([ SynPat.Paren(SynPat.Const(SynConst.Unit, range0), range0) ])
                        )
                    )
                SynBindingRcd.ReturnInfo = None
                SynBindingRcd.Expr = 
                    // TODO: Return {tbl.Name} record with each property set to __.{PropertyName}
                    SynExpr.Const(SynConst.Int32 3, range0)
                SynBindingRcd.Range = range0
                SynBindingRcd.Bind = DebugPointForBinding.NoDebugPointAtInvisibleBinding
            }
        )

    let members = ctor :: (props @ [ readMethod ])

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
                    for (record, recordReader) in zip do 
                        if cfg.IsCLIMutable then yield cliMutableAttribute
                        yield record
                        yield recordReader
                ]

            SynModuleDecl.CreateNestedModule(schemaNestedModule, tableRecordDeclarations)
        )

    //let openDataProvider = SynModuleDecl.CreateOpen("System.Data.SqlClient")
    let openPlaceholder = SynModuleDecl.CreateOpen("Substitute.Extensions")
    let declarations = openPlaceholder :: nestedSchemaModules

    let parentNamespace =
        { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong cfg.Namespace)
            with Declarations = declarations }

    parentNamespace

let substitutions = 
    [
        "open Substitute.Extensions",
        """
[<AutoOpen>]
module Extensions = 
    type System.Data.IDataReader with
        member this.Required (getter: int -> 'T, col: string) =
            this.GetOrdinal col |> getter

        member this.Optional (getter: int -> 'T, col: string) = 
            match this.GetOrdinal col with
            | o when this.IsDBNull o -> None
            | o -> Some (getter o)

        member this.RequiredBinary (getValue: int -> obj, col: string) =
            this.GetOrdinal col |> getValue :?> byte[]

        member this.OptionalBinary (getValue: int -> obj, col: string) = 
            match this.GetOrdinal col with
            | o when this.IsDBNull o -> None
            | o -> Some (getValue o :?> byte[])
        """
    ]

let toFormattedCode (cfg: Config) (comment: string) (generatedModule: SynModuleOrNamespaceRcd) = 
        let parsedInput = 
            ParsedInput.CreateImplFile(
                ParsedImplFileInputRcd.CreateFs(cfg.OutputFile).AddModule generatedModule)
    
        let cfg = { FormatConfig.FormatConfig.Default with StrictMode = true }
        let formattedCode = CodeFormatter.FormatASTAsync(parsedInput, "output.fs", [], None, cfg) |> Async.RunSynchronously
        let finalCode = substitutions |> List.fold (fun (code: string) (placeholder, sub) -> code.Replace(placeholder, sub)) formattedCode

        let formattedCodeWithComment =
            [   
                comment
                finalCode
            ]
            |> String.concat System.Environment.NewLine

        formattedCodeWithComment
