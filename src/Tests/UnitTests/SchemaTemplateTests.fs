module UnitTests.SchemaTemplateTests

open NUnit.Framework
open Swensen.Unquote
open SqlHydra.Domain
open SqlHydra

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

let private generateWith (cfg: Config) (db: Schema) =
    SchemaTemplate.generate cfg SqlHydra.Npgsql.Provider.instance db testVersion []

let private generate (db: Schema) = generateWith testConfig db

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

[<Test>]
let ``Does not generate Enums registration module when ProviderDbTypeAttributes is off`` () =
    let cfg = { testConfig with ProviderDbTypeAttributes = false }
    let db: Schema = { Tables = []; Enums = [ moodEnum () ] }
    let output = generateWith cfg db

    // Without ProviderDbTypeAttributes the generated file must not require an Npgsql package reference.
    test <@ not (output.Contains "module Enums") @>
    test <@ not (output.Contains "Npgsql.NpgsqlDataSourceBuilder") @>

[<Test>]
let ``Factory registers enums when enums exist`` () =
    let db: Schema = { Tables = []; Enums = [ moodEnum () ] }
    let output = generate db

    test <@ output.Contains "(Npgsql.NpgsqlDataSourceBuilder(connectionString) |> Enums.register).Build()" @>

[<Test>]
let ``Factory uses plain data source when no enums`` () =
    let db: Schema = { Tables = []; Enums = [] }
    let output = generate db

    test <@ output.Contains "Npgsql.NpgsqlDataSource.Create(connectionString)" @>
    test <@ not (output.Contains "Enums.register") @>
