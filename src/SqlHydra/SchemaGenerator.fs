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
let createTableRecord (tbl: Table) = 
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

/// Creates a "{tbl.Name}Reader" class that reads columns for a given table/record.
let createTableReaderClass (rdrCfg: ReadersConfig) (tbl: Table) = 
    let classId = Ident.CreateLong(tbl.Name + "Reader")
    let classCmpInfo = SynComponentInfo.ComponentInfo(SynAttributes.Empty, [], [], classId, XmlDoc.PreXmlDocEmpty, false, None, range0)

    let ctor = SynMemberDefn.CreateImplicitCtor([ 
        // Ex: (reader: Microsoft.Data.SqlClient.SqlDataReader)
        SynSimplePat.CreateTyped(Ident.Create("reader"), SynType.CreateLongIdent(rdrCfg.ReaderType)) 
        SynSimplePat.Id(Ident.Create("getOrdinal"), None, false, false, false, range0)
    ])

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
                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString("getOrdinal"), None)
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
                        |> Array.map (fun col -> 
                            RecordFieldName(LongIdentWithDots.CreateString(col.Name), false)
                            , SynExpr.CreateInstanceMethodCall(LongIdentWithDots.Create([ "__"; col.Name; "Read" ])) |> Some
                        )
                        |> Array.toList
                    )
            }
        )

    /// Initializes an optional table record (based on the existence of a PK or user supplied column).
    let readIfNotNullMethod = 

        // Try to get the first PK...
        let firstPK = tbl.Columns |> Array.tryFind (fun c -> c.IsPK) |> Option.map (fun c -> c.Name)

        SynMemberDefn.CreateMember(            
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("__.ReadIfNotNull")
                            , SynArgPats.Pats([ 
                                // If at least one PK column exists, no arg required; else allow caller to pass a `column: Column` arg
                                match firstPK with
                                | Some _ -> 
                                    SynPat.Const(SynConst.Unit, range0)
                                | None -> 
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
                                , 
                                // If at least one PK column exists, check first PK for null; else check user supplied column arg for null.
                                match firstPK with
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

            // Generate Read method only if all column types have a ReaderMethod specified;
            // otherwise, the record will be partially initialized and break the build.
            if tbl.Columns |> Array.forall(fun c -> c.TypeMapping.ReaderMethod.IsSome) then 
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
let createHydraReaderClass (rdrCfg: ReadersConfig) (tbls: Table seq) = 
    let classId = Ident.CreateLong("HydraReader")
    let classCmpInfo = SynComponentInfo.ComponentInfo(SynAttributes.Empty, [], [], classId, XmlDoc.PreXmlDocEmpty, false, None, range0)

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
                        , XmlDoc.PreXmlDocEmpty
                        , SynValData.SynValData(None, SynValInfo.Empty, None)
                        , SynPat.LongIdent(LongIdentWithDots.CreateString($"lazy{tbl.Name}"), None, None, SynArgPats.Empty, None, range0)
                        , None
                        , SynExpr.Lazy(
                            SynExpr.CreateApp(
                                // Function:
                                SynExpr.CreateLongIdent(
                                    false
                                    , LongIdentWithDots.CreateString($"{tbl.Name}Reader")
                                    , None
                                )
                                // Args:
                                , SynExpr.CreateParenedTuple([
                                    SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString("reader"), None)
                                    SynExpr.CreateApp(
                                        // Func
                                        SynExpr.CreateLongIdent(false, LongIdentWithDots.CreateString("buildGetOrdinal"), None)
                                        // Args
                                        , SynExpr.CreateConstString(tbl.Name)
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
            let readerCall = 
                SynExpr.CreateLongIdent(LongIdentWithDots.Create([$"lazy{tbl.Name}"; "Value"]))

            SynMemberDefn.CreateMember(
                { SynBindingRcd.Let with 
                    Pattern = SynPatRcd.LongIdent(SynPatLongIdentRcd.Create(LongIdentWithDots.Create(["__"; tbl.Name]), SynArgPats.Empty))
                    ValData = SynValData.SynValData(Some (MemberFlags.InstanceMember), SynValInfo.Empty, None)
                    Expr = readerCall
                }
            )
        ]

    let readByNameMethod = 
        SynMemberDefn.CreateMember(
            { SynBindingRcd.Let with 
                Pattern = 
                    SynPatRcd.LongIdent(
                        SynPatLongIdentRcd.Create(
                            LongIdentWithDots.CreateString("__.ReadByName")
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
                    let matchExpr = 
                        SynExpr.CreateMatch(
                            SynExpr.CreateTuple([
                                SynExpr.CreateIdent(Ident.Create("entity"))
                                SynExpr.CreateIdent(Ident.Create("isOption"))
                            ]), [
                                // Only match tables where all column types have a ReaderMethod specified;
                                // otherwise, the record will be partially initialized and break the build.
                                // Also ensure that a PK exists.
                                let validTables = 
                                    tbls 
                                    |> Seq.filter(fun tbl -> 
                                        tbl.Columns |> Array.forall(fun c -> c.TypeMapping.ReaderMethod.IsSome) &&
                                        tbl.Columns |> Array.exists(fun c -> c.IsPK)
                                    )

                                for tbl in validTables do
                                    SynMatchClause.Clause(
                                        SynPat.Tuple(false, [ SynPat.Const(SynConst.String(tbl.Name, range0), range0); SynPat.Const(SynConst.Bool(false), range0) ], range0)
                                        , None
                                        , SynExpr.Upcast(
                                            SynExpr.CreateInstanceMethodCall(LongIdentWithDots.Create([ "__"; tbl.Name; "Read" ]))
                                            , SynType.Create("obj")
                                            , range0
                                        )
                                        , range0
                                        , DebugPointForTarget.No
                                    )
                                    SynMatchClause.Clause(
                                        SynPat.Tuple(false, [ SynPat.Const(SynConst.String(tbl.Name, range0), range0); SynPat.Const(SynConst.Bool(true), range0) ], range0)
                                        , None
                                        , SynExpr.Upcast(
                                            SynExpr.CreateInstanceMethodCall(LongIdentWithDots.Create([ "__"; tbl.Name; "ReadIfNotNull" ]))
                                            , SynType.Create("obj")
                                            , range0
                                        )
                                        , range0
                                        , DebugPointForTarget.No
                                    )
                            
                                // Wildcard match
                                SynMatchClause.Clause(
                                    SynPat.Wild(range0)
                                    , None
                                    , SynExpr.CreateApp(
                                        SynExpr.Ident(Ident.Create("failwith"))
                                        , SynExpr.InterpolatedString([
                                            SynInterpolatedStringPart.String("Invalid entity: ", range0)
                                            SynInterpolatedStringPart.FillExpr(SynExpr.Ident(Ident.Create("entity")), None)
                                        ]
                                        , range0)
                                    )
                                    , range0
                                    , DebugPointForTarget.No
                                )
                            ]
                        )

                    SynExpr.Downcast(
                        matchExpr
                        , SynType.Anon(range0) // _
                        , range0
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

    let members = 
        [ 
            ctor
            utilPlaceholder
            yield! lazyReaders
            yield! readerProperties
            readByNameMethod
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
let generateModule (cfg: Config) (db: Schema) = 
    let schemas = db.Tables |> Array.map (fun t -> t.Schema) |> Array.distinct
    
    let nestedSchemaModules = 
        schemas
        |> Array.toList
        |> List.map (fun schema -> 
            let schemaNestedModule = SynComponentInfoRcd.Create [ Ident.Create schema ]

            let tables = db.Tables |> Array.filter (fun t -> t.Schema = schema)

            let tableRecordDeclarations = 
                [ 
                    // List each table record (and optionally, reader) in this db schema...
                    for tbl in tables do
                        if cfg.IsCLIMutable then 
                            cliMutableAttribute
                        
                        createTableRecord tbl
                        
                        if cfg.Readers.IsSome then 
                            createTableReaderClass cfg.Readers.Value tbl

                    // Create "HydraReader" below all generated tables/readers...
                    if cfg.Readers.IsSome then
                        createHydraReaderClass cfg.Readers.Value tables
                ]

            SynModuleDecl.CreateNestedModule(schemaNestedModule, tableRecordDeclarations)
        )

    let readerExtensionsPlaceholder = SynModuleDecl.CreateOpen("Substitute.Extensions")

    let declarations = 
        [ 
            if cfg.Readers.IsSome then
                readerExtensionsPlaceholder 

            yield! nestedSchemaModules
        ]

    let parentNamespace =
        { SynModuleOrNamespaceRcd.CreateNamespace(Ident.CreateLong cfg.Namespace)
            with Declarations = declarations }

    parentNamespace

/// A list of static code text substitutions to the generated file.
let substitutions = 
    [
        /// Reader classes at top of namespace
        "open Substitute.Extensions",
        """type Column(reader: System.Data.IDataReader, getOrdinal: string -> int, column) =
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
        """

        // HydraReader utility functions
        "member HydraReader = \"placeholder\"",
        """let entities = System.Collections.Generic.Dictionary<string, string -> int>()
        let buildGetOrdinal entity= 
            if not (entities.ContainsKey(entity)) then 
                let dictionary = 
                    [0..reader.FieldCount-1] 
                    |> List.mapi (fun i fieldIdx -> reader.GetName(fieldIdx), i)
                    |> List.groupBy (fun (nm, i) -> nm) 
                    |> List.map (fun (_, items) -> List.tryItem(entities.Count) items |> Option.defaultWith (fun () -> List.last items))
                    |> dict
                let getOrdinal = fun idx -> dictionary.Item idx
                entities.Add(entity, getOrdinal)
                getOrdinal
            else
                entities.[entity]
        """

        // HydraReader Read Method Body
        "// ReadMethodBodyPlaceholder",
        """
            let hydra = HydraReader(reader)

            let isPrimitive (t: System.Type) = t.IsPrimitive || t = typedefof<string>
            let getPropCount (t: System.Type) = t.GetProperties().Length
            let getTypeInfo (t: System.Type) = 
                let tp, isOpt = 
                    if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>> 
                    then t.GenericTypeArguments.[0], true
                    else t, false
                let isPrim = isPrimitive tp
                let fieldCount = if isPrim then 1 else getPropCount tp
                tp.Name, isOpt, isPrim, fieldCount

            let hydrateEntities typeInfos = 
                typeInfos 
                |> Array.fold (fun (ordinal, values) (tNm, isOpt, isPrim, fieldCount) -> 
                    let o = if isPrim then reader.GetValue(ordinal) else hydra.ReadByName(tNm, isOpt)                            
                    (ordinal + fieldCount, values @ [o])
                ) (0, []) |> snd |> List.toArray

            let t = typeof<'T>
            if t.Name.StartsWith "Tuple" then
                let entityInfos = t.GenericTypeArguments |> Array.map getTypeInfo
                fun () ->
                    let values = hydrateEntities entityInfos
                    let tuple = Microsoft.FSharp.Reflection.FSharpValue.MakeTuple(values, t)
                    tuple :?> 'T
            else
                fun () -> 
                    [| t |> getTypeInfo |] |> hydrateEntities |> Array.head :?> 'T
        """
    ]

/// Formats the generated code using Fantomas.
let toFormattedCode (cfg: Config) (appName: string) (generatedModule: SynModuleOrNamespaceRcd) = 
    let comment = $"// This code was generated by {appName}."
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
    let finalCode = substitutions |> List.fold (fun (code: string) (placeholder, sub) -> code.Replace(placeholder, sub)) formattedCode

    let formattedCodeWithComment =
        [   
            comment
            finalCode
        ]
        |> String.concat System.Environment.NewLine

    formattedCodeWithComment
