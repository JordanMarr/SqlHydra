module SqlHydra.SchemaGenerator
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler
open FsAst
open Fantomas
open Schema
open System.Data

let range0 = Range.range.Zero

/// Generates a CLIMutable attribute.
let cliMutableAttribute = 
    let attr =
        { TypeName = LongIdentWithDots.CreateString "CLIMutable"
        ; ArgExpr = SynExpr.CreateUnit
        ; Target = None
        ; AppliesToGetterAndSetter = false
        ; Range = range0 } : SynAttribute

    let atts = [ SynAttributeList.Create(attr) ]
    SynModuleDecl.CreateAttributes(atts)
    
/// Creates a record definition named after a table.
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

/// Creates a "Reader" class that reads columns for a given table/record.
let tableReaderClass (cfg: Config) (tbl: Table) = 
    let classId = Ident.CreateLong(tbl.Name + "DataReader")
    let classCmpInfo = SynComponentInfo.ComponentInfo(SynAttributes.Empty, [], [], classId, XmlDoc.PreXmlDocEmpty, false, None, range0)

    let ctor = SynMemberDefn.CreateImplicitCtor([ 
        // Ex: (reader: Microsoft.Data.SqlClient.SqlDataReader)
        SynSimplePat.CreateTyped(Ident.Create("reader"), SynType.CreateLongIdent(cfg.Readers.ReaderType)) 
    ])

    let memberFlags : MemberFlags = {IsInstance = true; IsDispatchSlot = false; IsOverrideOrExplicitImpl = false; IsFinal = false; MemberKind = MemberKind.Member}

    let readerProperties =
        tbl.Columns
        |> Array.toList
        // Only create reader properties for columns that have a ReaderMethod specified
        |> List.choose (fun col -> 
            match col.TypeMapping.ReaderMethod with
            | Some readerMethod -> Some (col, readerMethod)
            | None -> None
        )
        |> List.map (fun (col, readerMethod) ->
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
                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString($"reader.%s{readerMethod}"), None)
                        SynExpr.CreateConstString(col.Name)
                    ])

                    , range0 
                )

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
                                LongIdentWithDots.Create([ "__"; col.Name ]) // One method per column name
                                , SynArgPats.Pats([ SynPat.Paren(SynPat.Const(SynConst.Unit, range0), range0) ])
                            )
                        )
                    SynBindingRcd.ReturnInfo = None
                    SynBindingRcd.Expr = readerCall
                    SynBindingRcd.Range = range0
                    SynBindingRcd.Bind = DebugPointForBinding.NoDebugPointAtInvisibleBinding
                }
            )
        )

    
    /// Initializes a table record using the reader column properties.
    let toRecordMethod = 
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
                            LongIdentWithDots.CreateString("__.ToRecord")
                            , SynArgPats.Pats([ SynPat.Paren(SynPat.Const(SynConst.Unit, range0), range0) ])
                        )
                    )
                SynBindingRcd.ReturnInfo = None
                SynBindingRcd.Expr = 
                    SynExpr.CreateRecord (
                        tbl.Columns
                        |> Array.map (fun col -> 
                            RecordFieldName(LongIdentWithDots.CreateString(col.Name), false)
                            , SynExpr.CreateInstanceMethodCall(LongIdentWithDots.Create(["__"; col.Name ])) |> Some
                        )
                        |> Array.toList
                    )
                SynBindingRcd.Range = range0
                SynBindingRcd.Bind = DebugPointForBinding.NoDebugPointAtInvisibleBinding
            }
        )

    let members = 
        [ 
            ctor
            yield! readerProperties

            // Generate Read method only if all column types have a ReaderMethod specified;
            // otherwise, the record will be partially initialized and break the build.
            if tbl.Columns |> Array.forall(fun c -> c.TypeMapping.ReaderMethod.IsSome) then 
                toRecordMethod 
        ]

    let typeRepr = SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconUnspecified, members, range0)

    let readerClass = 
        SynTypeDefn.TypeDefn(
            classCmpInfo,
            typeRepr,
            SynMemberDefns.Empty,
            range0)
    
    SynModuleDecl.Types([ readerClass ], range0)

/// Generates the outer module and table records.
let generateModule (cfg: Config) (db: Schema) = 
    let schemas = db.Tables |> Array.map (fun t -> t.Schema) |> Array.distinct
    
    let nestedSchemaModules = 
        schemas
        |> Array.toList
        |> List.map (fun schema -> 
            let schemaNestedModule = SynComponentInfoRcd.Create [ Ident.Create schema ]

            let tables = db.Tables |> Array.filter (fun t -> t.Schema = schema)
            let tableRecords = tables |> Array.map tableRecord
            let readerClasses = tables |> Array.map (tableReaderClass cfg)
            let zip = Array.zip tableRecords readerClasses

            let tableRecordDeclarations = 
                [ 
                    for (record, recordReader) in zip do 
                        if cfg.IsCLIMutable then 
                            yield cliMutableAttribute
                        
                        yield record
                        
                        if cfg.Readers.IsEnabled then 
                            yield recordReader
                ]

            SynModuleDecl.CreateNestedModule(schemaNestedModule, tableRecordDeclarations)
        )

    let readerExtensionsPlaceholder = SynModuleDecl.CreateOpen("Substitute.Extensions")

    let declarations = 
        [ 
            if cfg.Readers.IsEnabled then
                readerExtensionsPlaceholder 

            yield! nestedSchemaModules
        ]

    let parentNamespace =
        { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong cfg.Namespace)
            with Declarations = declarations }

    parentNamespace

/// A list of text substitutions to the generated file.
let substitutions = 
    [
        "open Substitute.Extensions",
        """[<AutoOpen>]
module private Extensions = 
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

/// Formats the generated code using Fantomas.
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
