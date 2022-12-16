module UnitTests.TomlConfigParser

open Expecto
open System
open SqlHydra
open SqlHydra.Domain
open System.Globalization

/// Compare two strings ignoring white space and line breaks
let assertEqual (s1: string, s2: string) = 
    Expect.isTrue (String.Compare(s1, s2, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) = 0) ""

[<Tests>]
let tests = 
    categoryList "Unit Tests" "TOML Config Parser" [
        test "Save: All" {
            let cfg = 
                {
                    Config.ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
                    Config.OutputFile = "AdventureWorks.fs"
                    Config.Namespace = "SampleApp.AdventureWorks"
                    Config.IsCLIMutable = true
                    Config.ProviderDbTypeAttributes = true
                    Config.Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" }
                    Config.Filters = FilterPatterns.Empty
                }

            let toml = TomlConfigParser.save(cfg)
    
            let expected = 
                """
                [general]
                connection = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
                output = "AdventureWorks.fs"
                namespace = "SampleApp.AdventureWorks"
                cli_mutable = true
                [readers]
                reader_type = "Microsoft.Data.SqlClient.SqlDataReader"
                [filters]
                include = []
                exclude = []
                """

            assertEqual(expected, toml)
        }
    
        test "Read: with no filters" {
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
                    Config.ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
                    Config.OutputFile = "AdventureWorks.fs"
                    Config.Namespace = "SampleApp.AdventureWorks"
                    Config.IsCLIMutable = true
                    Config.ProviderDbTypeAttributes = true
                    Config.Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" }
                    Config.Filters = FilterPatterns.Empty
                }

            let cfg = TomlConfigParser.read(toml)
    
            Expect.equal cfg expected ""
        }

        test "Read: when no readers section should be None"  {
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
                    Config.ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
                    Config.OutputFile = "AdventureWorks.fs"
                    Config.Namespace = "SampleApp.AdventureWorks"
                    Config.IsCLIMutable = true
                    Config.ProviderDbTypeAttributes = true
                    Config.Readers = None
                    Config.Filters = FilterPatterns.Empty
                }

            let cfg = TomlConfigParser.read(toml)
    
            Expect.equal cfg expected ""
        }

        test "Read: should parse filters"  {
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
                }

            let cfg = TomlConfigParser.read(toml)
    
            Expect.equal cfg.Filters expectedFilters ""
        }
    ]