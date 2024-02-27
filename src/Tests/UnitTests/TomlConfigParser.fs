module UnitTests.``TOML Config Parser``

open System
open SqlHydra
open SqlHydra.Domain
open NUnit.Framework
open Swensen.Unquote
open System.Globalization

/// Compare two strings ignoring white space and line breaks
let assertEqual (s1: string, s2: string) = 
    Assert.IsTrue (String.Compare(s1, s2, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) = 0)

[<Test>]
let ``Save: All``() = 
    let cfg = 
        {
            ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
            OutputFile = "AdventureWorks.fs"
            Namespace = "SampleApp.AdventureWorks"
            IsCLIMutable = true
            IsMutableProperties = false
            NullablePropertyType = NullablePropertyType.Option
            ProviderDbTypeAttributes = true
            TableDeclarations = true
            Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" }
            Filters = Filters.Empty
        }

    let toml = TomlConfigParser.save(cfg)

    let expected = 
        """
        [general]
        connection = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
        output = "AdventureWorks.fs"
        namespace = "SampleApp.AdventureWorks"
        cli_mutable = true
        [sqlhydra_query_integration]
        provider_db_type_attributes = true
        table_declarations = true
        [readers]
        reader_type = "Microsoft.Data.SqlClient.SqlDataReader"
        [filters]
        include = []
        exclude = []
        """

    assertEqual(expected, toml)

[<Test>]
let ``Read: with no filters``() = 
    let toml = 
        """
        [general]
        connection = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
        output = "AdventureWorks.fs"
        namespace = "SampleApp.AdventureWorks"
        cli_mutable = true
        [readers]
        reader_type = "Microsoft.Data.SqlClient.SqlDataReader"
        """

    let expected = 
        {
            ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
            OutputFile = "AdventureWorks.fs"
            Namespace = "SampleApp.AdventureWorks"
            IsCLIMutable = true
            IsMutableProperties = false
            NullablePropertyType = NullablePropertyType.Option
            ProviderDbTypeAttributes = true
            TableDeclarations = false
            Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" }
            Filters = Filters.Empty
        }

    let cfg = TomlConfigParser.read(toml)

    cfg =! expected

[<Test>]
let ``Read: when no readers section should be None``() = 
    let toml = 
        """
        [general]
        connection = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
        output = "AdventureWorks.fs"
        namespace = "SampleApp.AdventureWorks"
        cli_mutable = true
        """

    let expected = 
        {
            ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
            OutputFile = "AdventureWorks.fs"
            Namespace = "SampleApp.AdventureWorks"
            IsCLIMutable = true
            IsMutableProperties = false
            NullablePropertyType = NullablePropertyType.Option
            ProviderDbTypeAttributes = true
            TableDeclarations = false
            Readers = None
            Filters = Filters.Empty
        }

    let cfg = TomlConfigParser.read(toml)

    cfg =! expected

[<Test>]
let ``Read: should parse filters``() = 
    let toml = 
        """
        [general]
        connection = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
        output = "AdventureWorks.fs"
        namespace = "SampleApp.AdventureWorks"
        cli_mutable = true
        [filters]
        include = [ "products/*", "dbo/*" ]
        exclude = [ "products/system*" ]                
        """

    let expectedFilters =
        { 
            Includes = [ "products/*"; "dbo/*" ]
            Excludes = [ "products/system*" ] 
            Restrictions = Map.empty
        }

    let cfg = TomlConfigParser.read(toml)

    cfg.Filters =! expectedFilters

[<Test>]
let ``Read: should parse schema restrictions``() = 
    let toml = 
        """
        [general]
        connection = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
        output = "AdventureWorks.fs"
        namespace = "SampleApp.AdventureWorks"
        cli_mutable = true
        [filters]
        include = []
        exclude = []
        restrictions = { "Tables" = [ "products" ], "Columns" = [ "", "Price" ] }
        """

    let expectedFilters =
        { 
            Includes = []
            Excludes = [] 
            Restrictions = 
                Map [ 
                    "Tables", [| "products" |]
                    "Columns", [| null; "Price" |] 
                ]
        }

    let cfg = TomlConfigParser.read(toml)

    cfg.Filters =! expectedFilters
