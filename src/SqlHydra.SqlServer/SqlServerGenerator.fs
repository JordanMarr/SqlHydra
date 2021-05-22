module SqlHydra.SqlServer.SqlServerGenerator

open Myriad.Core
open System.IO
open SqlHydra
    
[<MyriadGenerator("sqlserver")>]
type SqlServerGenerator() =
    interface IMyriadGenerator with

        member __.ValidInputExtensions = 
            seq { ".toml" }

        member __.Generate(ctx: GeneratorContext) =

            // Get ssdt config
            let config = ctx.ConfigGetter "sqlserver" |> Map.ofSeq
            
            let connectionString =
                config.TryFind("connection")
                |> Option.map string
                |> Option.defaultWith (fun () -> failwith "Unable to find 'sqlserver' 'connection' in myriad.toml.")

            // Get namespace
            let ns = 
                config.TryFind("namespace")
                |> Option.map string
                |> Option.defaultWith (fun () -> failwith "Unable to find 'sqlserver' 'namespace' in myriad.toml.")

            let inputFile = FileInfo(ctx.InputFilename)
            let schemaOutputPath = Path.Combine(inputFile.DirectoryName, "schema.json")

            let assembly = System.Reflection.Assembly.GetExecutingAssembly()
            let assemblyDir = FileInfo(assembly.Location).DirectoryName
            let exePath = Path.Combine(assemblyDir, "SqlHydra.SqlServer.exe")
            if not (File.Exists exePath) then failwithf "Unable to find provider: '%s'." exePath

            // Call exe to pull schema and save as json
            Schema.callSchemaProvider(exePath, connectionString, schemaOutputPath)
            
            // Read schema
            let schema = Schema.deserialize (schemaOutputPath)

            // Generate records
            SchemaGenerator.generateSchema(ns, schema)
