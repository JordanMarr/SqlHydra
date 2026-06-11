module UnitTests.SchemaTemplateTests

open NUnit.Framework
open Swensen.Unquote
open SqlHydra.Domain
open SqlHydra

// A minimal Config suitable for exercising SchemaTemplate.generate without a live database.
let private mkCfg () : Config =
    {
        ConnectionString = ""
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        IsMutableProperties = false
        NullablePropertyType = NullablePropertyType.Option
        ProviderDbTypeAttributes = true
        TableDeclarations = false
        Readers = None
        Filters = Filters.Empty
        TypeMappingExtensions = []
    }

let private mkVersion () : Version.InformationalVersion =
    {
        InformationalVersion = "0.0.0"
        Version = System.Version(0, 0, 0)
        PreReleaseSuffix = None
    }

let private moodEnum () : Enum =
    {
        Schema = "public"
        Name = "mood"
        Labels =
            [
                { Name = "happy"; SortOrder = 0 }
                { Name = "ok"; SortOrder = 1 }
                { Name = "sad"; SortOrder = 2 }
            ]
    }

let private generate (db: Schema) =
    SchemaTemplate.generate (mkCfg ()) SqlHydra.Npgsql.Provider.instance db (mkVersion ()) []

[<Test>]
let ``Generates Enums registration module for Npgsql enum`` () =
    let db: Schema = { Tables = []; Enums = [ moodEnum () ] }
    let output = generate db

    test <@ output.Contains "module Enums" @>
    test <@ output.Contains "let register (builder: Npgsql.NpgsqlDataSourceBuilder)" @>
    test <@ output.Contains "MapEnum<``public``.mood>(\"mood\")" @>

[<Test>]
let ``Does not generate Enums registration module when no enums`` () =
    let db: Schema = { Tables = []; Enums = [] }
    let output = generate db

    test <@ not (output.Contains "module Enums") @>
