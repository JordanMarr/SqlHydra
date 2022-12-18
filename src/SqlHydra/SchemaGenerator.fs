module SqlHydra.SchemaGenerator

open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.XmlDoc
open FsAst
open Fantomas
open Domain
open System.Data
open SqlHydra.SchemaFilters

#if NET5_0
let range0 = FSharp.Compiler.Range.range.Zero
#endif
#if NET6_0_OR_GREATER
let range0 = FSharp.Compiler.Text.range.Zero
#endif

type SynExpr with
    static member FailWith msg = SynExpr.CreateApp(SynExpr.Ident(Ident.Create("failwith")), SynExpr.CreateConstString(msg))

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
    
let createProviderDbTypeAttribute (mapping: TypeMapping) =
    mapping.ProviderDbType
    |> Option.map (fun type' ->
        let attributeFullName = typeof<ProviderDbTypeAttribute>.FullName

        let attr = 
            { TypeName = LongIdentWithDots.Create (attributeFullName.Replace("Attribute", "").Split(".") |> List.ofArray) 
            ; ArgExpr = SynExpr.CreateParenedTuple [ SynExpr.CreateConst (SynConst.String(type', range0)) ]
            ; Target = None
            ; AppliesToGetterAndSetter = false
            ; Range = range0 } : SynAttribute
   
        SynAttributes.Cons (SynAttributeList.Create attr, SynAttributes.Empty)
    ) 
    |> Option.defaultValue SynAttributes.Empty
    
/// Creates a record definition named after a table.
let createTableRecord (cfg: Config) (tbl: Table) = 
    let recordCmpInfo = 
        let myRecordId = LongIdentWithDots.CreateString tbl.Name
        SynComponentInfoRcd.Create(myRecordId.Lid)
    
    let recordDef =
        tbl.Columns
        |> List.map (fun col ->
            let field = 
                // Handles array types: "byte[]", "string[]", "int[]", "int []", "int array"
                if col.TypeMapping.ClrType.EndsWith "[]" || col.TypeMapping.ClrType.EndsWith "array" then
                    let baseTypeNm = col.TypeMapping.ClrType.Split([| "[]"; " []"; " array" |], System.StringSplitOptions.RemoveEmptyEntries) |> Array.head
                    let baseType = SynType.Create(baseTypeNm)
                    SynType.Array(0, baseType, range0)
                else
                    SynType.Create(col.TypeMapping.ClrType)

            let attributes = 
                if cfg.ProviderDbTypeAttributes
                then createProviderDbTypeAttribute col.TypeMapping
                else []

            let type' =
                if col.IsNullable then
                    SynType.Option(field)
                else
                    field
            
            {   
                Attributes = attributes
                IsStatic = false
                Id = Some (Ident.Create(col.Name))
                Type = type'
                IsMutable = false
                XmlDoc = PreXmlDoc.Empty
                Access = None
                Range = range0
            }
        )
        |> SynTypeDefnSimpleReprRecordRcd.Create
        |> SynTypeDefnSimpleReprRcd.Record
        
    SynModuleDecl.CreateSimpleType(recordCmpInfo, recordDef)

/// Creates an enum definition.
let createEnum (enum: Enum) = 
    let cmpInfo = 
        let myRecordId = LongIdentWithDots.CreateString enum.Name    
        SynComponentInfoRcd.Create(myRecordId.Lid)
    
    let enumDef = 
        enum.Labels
        |> List.sortBy (fun lbl -> lbl.SortOrder)
        |> List.map (fun lbl -> 
            {
                SynEnumCaseRcd.Id = Ident.Create(lbl.Name)
                SynEnumCaseRcd.Constant = SynConst.Int32(lbl.SortOrder)
                SynEnumCaseRcd.Range = range0
                SynEnumCaseRcd.Attributes = []
                SynEnumCaseRcd.XmlDoc = PreXmlDoc.Empty
            }
        )
        |> SynTypeDefnSimpleReprEnumRcd.Create
        |> SynTypeDefnSimpleReprRcd.Enum
    
    SynModuleDecl.CreateSimpleType(cmpInfo, enumDef)

/// Creates a "{tbl.Name}Reader" class that reads columns for a given table/record.
let createTableReaderClass (rdrCfg: ReadersConfig) (tbl: Table) = 
    let classId = Ident.CreateLong(tbl.Name + "Reader")
    let classCmpInfo = SynComponentInfo.ComponentInfo(SynAttributes.Empty, [], [], classId, PreXmlDoc.Empty, false, None, range0)

    let ctor = SynMemberDefn.CreateImplicitCtor([ 
        // Ex: (reader: Microsoft.Data.SqlClient.SqlDataReader)
        SynSimplePat.CreateTyped(Ident.Create("reader"), SynType.CreateLongIdent(rdrCfg.ReaderType)) 
        SynSimplePat.Id(Ident.Create("getOrdinal"), None, false, false, false, range0)
    ])

    let readerProperties =
        tbl.Columns
        // Only create reader properties for columns that have a ReaderMethod specified
        |> List.map (fun col ->
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
                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString("getOrdinal"), None)
                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString($"reader.%s{col.TypeMapping.ReaderMethod}"), None)
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
    let readMethod = 
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
                        |> List.map (fun col -> 
                            RecordFieldName(LongIdentWithDots.Create([col.Name]), false)
                            , SynExpr.CreateInstanceMethodCall(LongIdentWithDots.Create([ "__"; col.Name; "Read" ])) |> Some
                        )
                    )
            }
        )

    /// Initializes an optional table record (based on the existence of a PK or user supplied column).
    let readIfNotNullMethod = 

        let firstRequiredField = tbl.Columns |> Seq.tryFind (fun c -> c.IsNullable = false)
        let firstOptionalField = tbl.Columns |> Seq.tryFind (fun c -> c.IsNullable = true)

        // Try to get the first PK, or else the first required field, or else the first optional field (as a last resort)
        let firstPkOrFirstRequiredField = 
            tbl.Columns 
            |> List.tryFind (fun c -> c.IsPK)
            |> Option.orElse firstRequiredField
            |> Option.orElse firstOptionalField
            |> Option.map (fun c -> c.Name)

        SynMemberDefn.CreateMember(            
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("__.ReadIfNotNull")
                            , SynArgPats.Pats([ 
                                SynPat.Const(SynConst.Unit, range0)
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
                                , 
                                // If at least one PK column exists, check first PK for null; else check user supplied column arg for null.
                                match firstPkOrFirstRequiredField with
                                | Some col -> LongIdentWithDots.Create([ "__"; col; "IsNull" ])
                                | None -> LongIdentWithDots.Create([ "column"; "IsNull" ]) 
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
            readMethod 
            readIfNotNullMethod
        ]

    let typeRepr = SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconUnspecified, members, range0)

    let readerClass = 
        SynTypeDefn.TypeDefn(
            classCmpInfo,
            typeRepr,
            SynMemberDefns.Empty,
            range0)
    
    SynModuleDecl.Types([ readerClass ], range0)

/// Creates a "HydraReader" class with properties for each table in a given schema.
let createHydraReaderClass (db: Schema) (rdrCfg: ReadersConfig) (app: AppInfo) (tbls: Table seq) = 
    let classId = Ident.CreateLong("HydraReader")
    let classCmpInfo = SynComponentInfo.ComponentInfo(SynAttributes.Empty, [], [], classId, PreXmlDoc.Empty, false, None, range0)

    let ctor = SynMemberDefn.CreateImplicitCtor([ 
        // Ex: (reader: Microsoft.Data.SqlClient.SqlDataReader)
        SynSimplePat.CreateTyped(Ident.Create("reader"), SynType.CreateLongIdent(rdrCfg.ReaderType)) 
    ])

    let utilPlaceholder = 
        SynMemberDefn.CreateMember(
            { SynBindingRcd.Let with 
                Pattern = SynPatRcd.CreateLongIdent(LongIdentWithDots.CreateString("HydraReader"), [])
                ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                Expr = SynExpr.CreateConstString("placeholder")
            }
        )
    
    let lazyReaders =
        [ for tbl in tbls do
            SynMemberDefn.LetBindings(
                [ 
                    SynBinding.Binding(
                        None
                        , SynBindingKind.NormalBinding
                        , false
                        , false
                        , []
                        , PreXmlDoc.Empty
                        , SynValData.SynValData(None, SynValInfo.Empty, None)
                        , SynPat.LongIdent(LongIdentWithDots.CreateString($"lazy{tbl.Schema}{tbl.Name}"), None, None, SynArgPats.Empty, None, range0)
                        , None
                        , SynExpr.Lazy(
                            SynExpr.CreateApp(
                                // Function:
                                SynExpr.CreateLongIdent(
                                    false
                                    , LongIdentWithDots.CreateString($"{tbl.Schema}.Readers.{tbl.Name}Reader")
                                    , None
                                )
                                // Args:
                                , SynExpr.CreateParenedTuple([
                                    SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString("reader"), None)
                                    SynExpr.CreateApp(
                                        // Func
                                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString("buildGetOrdinal"), None)
                                        // Args
                                        , SynExpr.CreateConst(SynConst.Int32(tbl.TotalColumns)) // The total number of columns (includes unsupported columns)
                                    )
                                ])
                            )
                            , range0
                        )
                        , range0
                        , DebugPointAtBinding(range0)
                    )
                ]
                , false
                , false
                , range0
            )
        ]

    let readerProperties =
        // Only create reader properties for columns that have a ReaderMethod specified
        [ for tbl in tbls do
            SynMemberDefn.CreateMember(
                { SynBindingRcd.Let with 
                    Pattern = SynPatRcd.LongIdent(SynPatLongIdentRcd.Create(LongIdentWithDots.Create([ "__"; $"{tbl.Schema}.{tbl.Name}"]), SynArgPats.Empty))
                    ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                    Expr = SynExpr.CreateLongIdent(LongIdentWithDots.Create([$"lazy{tbl.Schema}{tbl.Name}"; "Value"]))
                }
            )
        ]

    let accFieldCountProperty = 
        SynMemberDefn.CreateMember(
            { SynBindingRcd.Let with 
                Pattern = SynPatRcd.LongIdent(SynPatLongIdentRcd.Create(LongIdentWithDots.Create(["__"; "AccFieldCount"]), SynArgPats.Empty, access=SynAccess.Private))
                ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                Expr = SynExpr.CreateUnit
            }
        )

    let getReaderByNameMethod = 
        SynMemberDefn.CreateMember(
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("__.GetReaderByName")
                            , SynArgPats.Pats(
                                [
                                    SynPat.Paren(
                                        SynPat.Tuple(
                                            false
                                            , [
                                                SynPat.Typed(
                                                    SynPat.LongIdent(LongIdentWithDots.CreateString("entity"), None, None, SynArgPats.Empty, None, range0)
                                                    , SynType.Create("string")
                                                    , range0
                                                )
                                                SynPat.Typed(
                                                    SynPat.LongIdent(LongIdentWithDots.CreateString("isOption"), None, None, SynArgPats.Empty, None, range0)
                                                    , SynType.Create("bool")
                                                    , range0
                                                )
                                            ]
                                            , range0
                                        )
                                        , range0
                                    )
                                ]
                            )
                            , access = SynAccess.Private
                        )
                    )
                ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                Expr = 
                    SynExpr.CreateMatch(
                        SynExpr.CreateTuple([
                            SynExpr.CreateIdent(Ident.Create("entity"))
                            SynExpr.CreateIdent(Ident.Create("isOption"))
                        ]), 
                        [
                            for tbl in tbls do
                                
                                // match case: isOption = false
                                SynMatchClause.Clause(
                                    SynPat.Tuple(false, [ 
                                        SynPat.Const(SynConst.String($"{tbl.Schema}.{tbl.Name}", range0), range0)
                                        SynPat.Const(SynConst.Bool(false), range0) 
                                    ], range0)
                                    , None
                                    , 
                                    SynExpr.CreateAppInfix(
                                        SynExpr.CreateLongIdent(false, LongIdentWithDots.Create([ "__"; $"{tbl.Schema}.{tbl.Name}"; "Read" ]), None), 
                                        SynExpr.CreateIdent(Ident.Create(">> box"))
                                    )
                                    , range0
                                    , DebugPointForTarget.No
                                )
                                
                                // match case: isOption = true
                                SynMatchClause.Clause(
                                    SynPat.Tuple(false, [ 
                                        SynPat.Const(SynConst.String($"{tbl.Schema}.{tbl.Name}", range0), range0)
                                        SynPat.Const(SynConst.Bool(true), range0) 
                                    ], range0)
                                    , None
                                    ,
                                    
                                    SynExpr.CreateAppInfix(
                                        SynExpr.CreateLongIdent(false, LongIdentWithDots.Create([ "__"; $"{tbl.Schema}.{tbl.Name}"; "ReadIfNotNull" ]), None), 
                                        SynExpr.CreateIdent(Ident.Create(">> box"))
                                    )
                                    , range0
                                    , DebugPointForTarget.No
                                )
                            
                            // match case: wildcard
                            SynMatchClause.Clause(
                                SynPat.Wild(range0)
                                , None
                                , SynExpr.CreateApp(
                                    SynExpr.Ident(Ident.Create("failwith"))
                                    , SynExpr.InterpolatedString([
                                        SynInterpolatedStringPart.String("Could not read type '", range0)
                                        SynInterpolatedStringPart.FillExpr(SynExpr.Ident(Ident.Create("entity")), None)
                                        SynInterpolatedStringPart.String("' because no generated reader exists.", range0)
                                    ]
                                    , range0)
                                )
                                , range0
                                , DebugPointForTarget.No
                            )
                        ]
                    )
            }
        )

    let staticReadMethod = 
        SynMemberDefn.CreateMember(
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("Read")
                            , SynArgPats.Pats(
                                [
                                    SynPat.Paren(
                                        SynPat.Typed(
                                            SynPat.LongIdent(LongIdentWithDots.CreateString("reader"), None, None, SynArgPats.Empty, None, range0)
                                            , SynType.Create(rdrCfg.ReaderType)
                                            , range0
                                        )
                                        , range0
                                    )
                                ]
                            )
                        )
                    )
                ValData = 
                    SynValData.SynValData(
                        Some (MemberFlags.StaticMember)
                        , SynValInfo.SynValInfo(
                            [
                                [ SynArgInfo.SynArgInfo(SynAttributes.Empty, false, Some(Ident.Create("reader"))) ]
                            ]
                            , SynArgInfo.Empty
                        )
                        , None
                    )
                Expr = SynExpr.Ident(Ident.Create("// ReadMethodBodyPlaceholder"))
            }
        )

    let staticGetPrimitiveReaderMethod =         
        SynMemberDefn.CreateMember(
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("GetPrimitiveReader")
                            , SynArgPats.Pats(
                                [
                                    SynPat.Paren(
                                        SynPat.Tuple(
                                            false, 
                                            [
                                                SynPat.Typed(
                                                    SynPat.LongIdent(LongIdentWithDots.CreateString("t"), None, None, SynArgPats.Empty, None, range0)
                                                    , SynType.Create("System.Type")
                                                    , range0
                                                )
                                                SynPat.Typed(
                                                    SynPat.LongIdent(LongIdentWithDots.CreateString("reader"), None, None, SynArgPats.Empty, None, range0)
                                                    , SynType.Create(rdrCfg.ReaderType)
                                                    , range0
                                                )
                                                SynPat.Typed(
                                                    SynPat.LongIdent(LongIdentWithDots.CreateString("isOpt"), None, None, SynArgPats.Empty, None, range0)
                                                    , SynType.Create("bool")
                                                    , range0
                                                )
                                            ], range0
                                        ), range0
                                    )
                                ]
                            )
                            , access = SynAccess.Private
                        )
                    )
                ValData = 
                    SynValData.SynValData(
                        Some (MemberFlags.StaticMember)
                        , SynValInfo.SynValInfo(
                            [
                                [ SynArgInfo.SynArgInfo(SynAttributes.Empty, false, Some(Ident.Create("reader"))) ]
                            ]
                            , SynArgInfo.Empty
                        )
                        , None
                    )
                Expr = 
                    let t = SynExpr.Ident(Ident.Create("t"))
                    let eq = SynExpr.Ident(Ident.Create("="))
                    let typeDef (typeNm: string) = 
                        let synType = 
                            if typeNm.EndsWith("[]") then 
                                // Ex: "byte[]"
                                let tn = typeNm.Replace("[]", "").Trim()
                                SynType.Array(0, SynType.Create(tn), range0)
                            else
                                SynType.Create(typeNm)
                        SynExpr.TypeApp(SynExpr.Ident(Ident.Create("typedefof")), range0, [ synType ], [], None, range0, range0)

                    let buildIf elseClause (ptr: PrimitiveTypeReader) = 
                        SynExpr.IfThenElse(
                            SynExpr.CreateApp(t, SynExpr.CreateApp(eq, typeDef ptr.ClrType))
                            , SynExpr.CreateApp(
                                SynExpr.CreateIdentString("Some")
                                , SynExpr.CreateParen(
                                    SynExpr.CreateApp(
                                        SynExpr.CreateIdent(Ident.Create("wrap")),
                                        SynExpr.CreateLongIdent(false, LongIdentWithDots.Create([ "reader"; ptr.ReaderMethod ]), None)
                                    )
                                )
                            )
                            , Some elseClause
                            , DebugPointForBinding.NoDebugPointAtDoBinding
                            , false
                            , range0
                            , range0
                        )

                    // Recursively build if..elif..elif..else
                    let ifExpression = 
                        db.PrimitiveTypeReaders 
                        |> Seq.rev
                        |> Seq.fold (fun elifClause ptr -> buildIf elifClause ptr) (SynExpr.CreateIdentString("None"))

                    let wrapFnPlaceholderBinding = 
                        SynBinding.Binding(
                            None
                            , SynBindingKind.NormalBinding
                            , false
                            , false
                            , []
                            , PreXmlDoc.Empty
                            , SynValData.SynValData(None, SynValInfo.Empty, None)
                            , SynPat.LongIdent(LongIdentWithDots.CreateString("wrap"), None, None, SynArgPats.Empty, None, range0)
                            , None
                            , SynExpr.LetOrUse(false, false, [], SynExpr.CreateConstString("wrap-placeholder"), range0)
                            , range0
                            , DebugPointForBinding.NoDebugPointAtDoBinding
                        )
                    
                    SynExpr.LetOrUse(
                        false
                        , false
                        , [ 
                            wrapFnPlaceholderBinding
                        ]
                        , ifExpression
                        , range0
                    )
                    
            }
        )

    let members = 
        [ 
            ctor
            utilPlaceholder
            yield! lazyReaders
            yield! readerProperties
            accFieldCountProperty
            getReaderByNameMethod
            staticGetPrimitiveReaderMethod
            staticReadMethod
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
let generateModule (cfg: Config) (app: AppInfo) (db: Schema) = 
    let filteredTables = 
        db.Tables 
        |> filterTables cfg.Filters
        |> List.sortBy (fun tbl -> tbl.Schema, tbl.Name)

    let schemas = 
        let enumSchemas = db.Enums |> List.map (fun e -> e.Schema)
        let tableSchemas = filteredTables |> List.map (fun t -> t.Schema) 
        enumSchemas @ tableSchemas |> List.distinct
    
    let nestedSchemaModules_hasEnums = 
        schemas
        |> List.map (fun schema -> 
            let schemaNestedModule = SynComponentInfoRcd.Create [ Ident.Create schema ]

            let tables = filteredTables |> List.filter (fun t -> t.Schema = schema)

            // Postgres enums
            let enumDeclarations = 
                db.Enums
                |> List.filter (fun enum -> enum.Schema = schema)
                |> List.map createEnum

            let tableRecordDeclarations = 
                [ 
                    // List each table record (and optionally, reader) in this db schema...
                    for tbl in tables do
                        if cfg.IsCLIMutable then 
                            cliMutableAttribute
                        
                        createTableRecord cfg tbl
                ]

            let readersModule = 
                match cfg.Readers with
                | Some readers -> 
                    let rm = SynComponentInfoRcd.Create [ Ident.Create "Readers" ]
                    let tableReaderClasses = tables |> List.map (createTableReaderClass readers)
                    [ SynModuleDecl.CreateNestedModule(rm, tableReaderClasses) ]
                | None -> []

            let memberDeclarations = enumDeclarations @ tableRecordDeclarations @ readersModule

            let hasEnumDefinitions = db.Enums.Length > 0

            SynModuleDecl.CreateNestedModule(schemaNestedModule, memberDeclarations), hasEnumDefinitions
        )

    // Sort schemas with enum definitions first.
    // (Tables with Postgres enum columns may depend on an enum in a different schema.)
    // NOTE: This is the most basic approach to sorting dependencies and could fail.
    // A more robust approach would be to recursively sort.
    let nestedSchemaModules = 
        nestedSchemaModules_hasEnums
        |> List.sortBy (fun (schemaModule, hasEnums) -> hasEnums)
        |> List.map (fun (schemaModule, hasEnums) -> schemaModule)

    let readerExtensionsPlaceholder = SynModuleDecl.CreateOpen("Substitute.Extensions")

    let allTables = schemas |> List.collect (fun schema -> filteredTables |> List.filter (fun t -> t.Schema = schema))
    // TODO: Handle duplicate table names between schemas

    let declarations = 
        [ 
            if cfg.Readers.IsSome then
                readerExtensionsPlaceholder 

            yield! nestedSchemaModules

            // Create "HydraReader" below all generated tables/readers...
            if cfg.Readers.IsSome then
                createHydraReaderClass db cfg.Readers.Value app allTables
        ]

    let parentNamespace =
        { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong cfg.Namespace)
            with Declarations = declarations }

    parentNamespace

/// A list of static code text substitutions to the generated file.
let substitutions (app: AppInfo) = 

    let utilsGetDateOnly = """
[<AutoOpen>]        
module Utils =
    type System.Data.IDataReader with
        member reader.GetDateOnly(ordinal: int) = 
            reader.GetDateTime(ordinal) |> System.DateOnly.FromDateTime
    
    type System.Data.Common.DbDataReader with
        member reader.GetTimeOnly(ordinal: int) = 
            reader.GetFieldValue(ordinal) |> System.TimeOnly.FromTimeSpan
        """

    [
        /// Reader classes at top of namespace
        "open Substitute.Extensions",
        $"""type Column(reader: System.Data.IDataReader, getOrdinal: string -> int, column) =
        member __.Name = column
        member __.IsNull() = getOrdinal column |> reader.IsDBNull
        override __.ToString() = __.Name

type RequiredColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getter: int -> 'T, column) =
        inherit Column(reader, getOrdinal, column)
        member __.Read(?alias) = alias |> Option.defaultValue __.Name |> getOrdinal |> getter

type OptionalColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getter: int -> 'T, column) =
        inherit Column(reader, getOrdinal, column)
        member __.Read(?alias) = 
            match alias |> Option.defaultValue __.Name |> getOrdinal with
            | o when reader.IsDBNull o -> None
            | o -> Some (getter o)

type RequiredBinaryColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getValue: int -> obj, column) =
        inherit Column(reader, getOrdinal, column)
        member __.Read(?alias) = alias |> Option.defaultValue __.Name |> getOrdinal |> getValue :?> byte[]

type OptionalBinaryColumn<'T, 'Reader when 'Reader :> System.Data.IDataReader>(reader: 'Reader, getOrdinal, getValue: int -> obj, column) =
        inherit Column(reader, getOrdinal, column)
        member __.Read(?alias) = 
            match alias |> Option.defaultValue __.Name |> getOrdinal with
            | o when reader.IsDBNull o -> None
            | o -> Some (getValue o :?> byte[])
        {
#if NET6_0_OR_GREATER
            utilsGetDateOnly
#else
            ""
#endif
        }
        """


        // HydraReader utility functions
        "member HydraReader = \"placeholder\"",
        """let mutable accFieldCount = 0
    let buildGetOrdinal fieldCount =
        let dictionary = 
            [0..reader.FieldCount-1] 
            |> List.map (fun i -> reader.GetName(i), i)
            |> List.sortBy snd
            |> List.skip accFieldCount
            |> List.take fieldCount
            |> dict
        accFieldCount <- accFieldCount + fieldCount
        fun col -> dictionary.Item col
        """

        // "wrap" fn in GetPrimitiveReader
        "let wrap = \"wrap-placeholder\"",
        """let wrap get (ord: int) = 
            if isOpt 
            then (if reader.IsDBNull ord then None else get ord |> Some) |> box 
            else get ord |> box 
        """

        // HydraReader Read Method Body
        "// ReadMethodBodyPlaceholder",
        $"""
        let hydra = HydraReader(reader)
        {if app.Name = "SqlHydra.Oracle" then "reader.SuppressGetDecimalInvalidCastException <- true" else ""}            
        let getOrdinalAndIncrement() = 
            let ordinal = hydra.AccFieldCount
            hydra.AccFieldCount <- hydra.AccFieldCount + 1
            ordinal
            
        let buildEntityReadFn (t: System.Type) = 
            let t, isOpt = 
                if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>> 
                then t.GenericTypeArguments.[0], true
                else t, false
            
            match HydraReader.GetPrimitiveReader(t, reader, isOpt) with
            | Some primitiveReader -> 
                let ord = getOrdinalAndIncrement()
                fun () -> primitiveReader ord
            | None ->
                let nameParts = t.FullName.Split([| '.'; '+' |])
                let schemaAndType = nameParts |> Array.skip (nameParts.Length - 2) |> fun parts -> System.String.Join(".", parts)
                hydra.GetReaderByName(schemaAndType, isOpt)
            
        // Return a fn that will hydrate 'T (which may be a tuple)
        // This fn will be called once per each record returned by the data reader.
        let t = typeof<'T>
        if FSharp.Reflection.FSharpType.IsTuple(t) then
            let readEntityFns = FSharp.Reflection.FSharpType.GetTupleElements(t) |> Array.map buildEntityReadFn
            fun () ->
                let entities = readEntityFns |> Array.map (fun read -> read())
                Microsoft.FSharp.Reflection.FSharpValue.MakeTuple(entities, t) :?> 'T
        else
            let readEntityFn = t |> buildEntityReadFn
            fun () -> 
                readEntityFn() :?> 'T
        """

        /// AccFieldCount property
        "member private __.AccFieldCount = ()",
        "member private __.AccFieldCount with get () = accFieldCount and set (value) = accFieldCount <- value"
    ]

/// Formats the generated code using Fantomas.
let toFormattedCode (cfg: Config) (app: AppInfo) (generatedModule: SynModuleOrNamespaceRcd) = 
    let comment = $"// This code was generated by `{app.Name}` -- v{app.Version}."
    let parsedInput = 
        ParsedInput.CreateImplFile(
            ParsedImplFileInputRcd.CreateFs(cfg.OutputFile).AddModule generatedModule)
    
    let cfg = { 
            FormatConfig.FormatConfig.Default with 
                StrictMode = true
                MaxIfThenElseShortWidth = 400   // Forces ReadIfNotNull if/then to be on a single line
                MaxValueBindingWidth = 400      // Ensure reader property/column bindings stay on one line
                MaxLineLength = 400             // Ensure reader property/column bindings stay on one line
        }
    let formattedCode = CodeFormatter.FormatASTAsync(parsedInput, "output.fs", [], None, cfg) |> Async.RunSynchronously    
    let finalCode = substitutions app |> List.fold (fun (code: string) (placeholder, sub) -> code.Replace(placeholder, sub)) formattedCode

    let formattedCodeWithComment =
        [   
            comment
            finalCode
        ]
        |> String.concat System.Environment.NewLine

    formattedCodeWithComment
