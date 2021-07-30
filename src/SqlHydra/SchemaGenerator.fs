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
    let classId = Ident.CreateLong(tbl.Name + "Reader")
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
                SynExpr.CreateApp(
                    // Function:
                    SynExpr.CreateLongIdent(
                        false
                        , LongIdentWithDots.CreateString(
                            match col.TypeMapping.DbType, col.IsNullable with
                            | DbType.Binary, true -> "OptionalBinaryColumn"
                            | DbType.Binary, false -> "RequiredBinaryColumn"
                            | _, true -> "OptionalColumn"
                            | _, false -> "RequiredColumn"
                        )
                        , None
                    )
                    // Args:
                    , SynExpr.CreateParenedTuple([
                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString("reader"), None)
                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString($"reader.%s{readerMethod}"), None)
                        SynExpr.CreateConstString(col.Name)
                    ])
                )

            SynMemberDefn.CreateMember(
                { SynBindingRcd.Let with 
                    Pattern = SynPatRcd.LongIdent(SynPatLongIdentRcd.Create(LongIdentWithDots.Create(["__"; col.Name]), SynArgPats.Empty))
                    ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                    Expr = readerCall
                }
            )

        )
    
    /// Initializes a table record using the reader column properties.
    let toRecordMethod = 
        SynMemberDefn.CreateMember(
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("__.Read")
                            , SynArgPats.Pats([ SynPat.Paren(SynPat.Const(SynConst.Unit, range0), range0) ])
                        )
                    )
                ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                Expr = 
                    SynExpr.CreateRecord (
                        tbl.Columns
                        |> Array.map (fun col -> 
                            RecordFieldName(LongIdentWithDots.CreateString(col.Name), false)
                            , SynExpr.CreateInstanceMethodCall(LongIdentWithDots.Create([ "__"; col.Name; "Read" ])) |> Some
                        )
                        |> Array.toList
                    )
            }
        )

    /// Initializes an optional table record (Some if the given column is not null).
    let toRecordIfMethod = 
        SynMemberDefn.CreateMember(            
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("__.ReadIfNotNull")
                            , SynArgPats.Pats([ 
                                SynPat.Paren(
                                    SynPat.Typed(
                                        SynPat.LongIdent(LongIdentWithDots.CreateString("column"), None, None, SynArgPats.Empty, None, range0)
                                        , SynType.Create("Column")
                                        , range0
                                    )
                                    , range0
                                )
                            ])
                        )
                    )
                ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                Expr = 
                    SynExpr.IfThenElse(
                        SynExpr.CreateApp(
                            // Function:
                            SynExpr.LongIdent(
                                false
                                , LongIdentWithDots.Create([ "column"; "IsNull" ])
                                , None
                                , range0)
                            // Args:
                            , SynExpr.CreateParenedTuple([])
                        )
                        , SynExpr.CreateIdentString("None")
                        ,   SynExpr.CreateApp(
                                // Function:
                                SynExpr.LongIdent(
                                    false
                                    , LongIdentWithDots.CreateString("Some")
                                    , None
                                    , range0)
                                // Args:
                                , SynExpr.CreateParenedTuple([
                                    SynExpr.App(
                                        ExprAtomicFlag.Atomic
                                        , false
                                        
                                        // Function:
                                        , SynExpr.LongIdent(
                                            false
                                            , LongIdentWithDots.Create([ "__"; "Read" ])
                                            , None
                                            , range0)
                                        
                                        // Args:
                                        , SynExpr.CreateParenedTuple([])
                                        , range0 
                                    )
                                ])
                            ) 
                            |> Some
                        , DebugPointForBinding.DebugPointAtBinding(range0)
                        , false
                        , range0
                        , range0
                    )
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
                toRecordIfMethod
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
        """type Column(reader: System.Data.IDataReader, column) =
        member val Name = column with get,set
        member __.IsNull() = reader.GetOrdinal column |> reader.IsDBNull
        member __.As(alias) = __.Name <- alias
        override __.ToString() = __.Name

type RequiredColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getter: int -> 'T, column) =
        inherit Column(reader, column)
        member __.Read(?alias) = alias |> Option.defaultValue __.Name |> reader.GetOrdinal |> getter

type OptionalColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getter: int -> 'T, column) =
        inherit Column(reader, column)
        member __.Read(?alias) = 
            match alias |> Option.defaultValue __.Name |> reader.GetOrdinal with
            | o when reader.IsDBNull o -> None
            | o -> Some (getter o)

type RequiredBinaryColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getValue: int -> obj, column) =
        inherit Column(reader, column)
        member __.Read(?alias) = alias |> Option.defaultValue __.Name |> reader.GetOrdinal |> getValue :?> byte[]

type OptionalBinaryColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getValue: int -> obj, column) =
        inherit Column(reader, column)
        member __.Read(?alias) = 
            match alias |> Option.defaultValue __.Name |> reader.GetOrdinal with
            | o when reader.IsDBNull o -> None
            | o -> Some (getValue o :?> byte[])
        """
    ]

/// Formats the generated code using Fantomas.
let toFormattedCode (cfg: Config) (comment: string) (generatedModule: SynModuleOrNamespaceRcd) = 
        let parsedInput = 
            ParsedInput.CreateImplFile(
                ParsedImplFileInputRcd.CreateFs(cfg.OutputFile).AddModule generatedModule)
    
        let cfg = { 
                FormatConfig.FormatConfig.Default with 
                    StrictMode = true
                    MaxIfThenElseShortWidth = 400   // Forces ReadIfNotNull if/then to be on a single line
                    MaxValueBindingWidth = 400      // Ensure reader property/column bindings stay on one line
            }
        let formattedCode = CodeFormatter.FormatASTAsync(parsedInput, "output.fs", [], None, cfg) |> Async.RunSynchronously
        let finalCode = substitutions |> List.fold (fun (code: string) (placeholder, sub) -> code.Replace(placeholder, sub)) formattedCode

        let formattedCodeWithComment =
            [   
                comment
                finalCode
            ]
            |> String.concat System.Environment.NewLine

        formattedCodeWithComment
