module TomlConfigParser

open Expecto
open System
open SqlHydra
open SqlHydra.SqlServer
open SqlHydra.Domain
open System.Globalization

/// Compare two strings ignoring white space and line breaks
let assertEqual (s1: string, s2: string) = 
    Expect.isTrue (String.Compare(s1, s2, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) = 0) ""

[<Tests>]
let tests = 

    testList "TOML Config Parser" [
        testCase "Parse: All" <| fun _ ->
            let cfg = 
                {
                    Config.ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
                    Config.OutputFile = "AdventureWorks.fs"
                    Config.Namespace = "SampleApp.AdventureWorks"
                    Config.IsCLIMutable = true
                    Config.Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" }
                }

            let toml = TomlConfigParser.serialize(cfg)
    
            let expected = 
                """
                [general]
                connection = "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI"
                output = "AdventureWorks.fs"
                namespace = "SampleApp.AdventureWorks"
                cli_mutable = true
                [readers]
                reader_type = "Microsoft.Data.SqlClient.SqlDataReader"
                """

            assertEqual(expected, toml)
    
        testCase "Read: All" <| fun _ ->
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
                    Config.Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" }
                }

            let cfg = TomlConfigParser.deserialize(toml)
    
            Expect.equal expected cfg ""

        testCase "Read: when no readers section should be None"  <| fun _ ->
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
                    Config.Readers = None
                }

            let cfg = TomlConfigParser.deserialize(toml)
    
            Expect.equal expected cfg ""
    ]