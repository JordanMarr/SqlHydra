module Sqlite.Generation

open Swensen.Unquote
open SqlHydra.Sqlite
open SqlHydra
open SqlHydra.Domain
open NUnit.Framework

let connectionString =
    let assembly = System.Reflection.Assembly.GetExecutingAssembly().Location |> System.IO.FileInfo
    let thisDir = assembly.Directory.Parent.Parent.Parent.FullName
    let relativeDbPath = System.IO.Path.Combine(thisDir, "TestData", "AdventureWorksLT.db")
    $"Data Source={relativeDbPath}"

let cfg =
    {
        ConnectionString = connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        IsMutableProperties = false
        NullablePropertyType = NullablePropertyType.Option
        ProviderDbTypeAttributes = true
        TableDeclarations = true
        LeftJoinedViews = false
        Readers = Some { ReadersConfig.ReaderType = "System.Data.Common.DbDataReader" }
        Filters = Filters.Empty
        TypeMappingExtensions = []
    }

let getCode (typeMappingExts: IExtendTypeMapping list) (namingExts: IExtendNaming list) cfg =
    let schema = SqliteSchemaProvider.getSchema(cfg, false, typeMappingExts)
    let version = Version.get()
    SchemaTemplate.generate cfg Provider.instance schema version namingExts

//#if NET10_0
//[<Test>]
//let ``TextTypeMapping extension should map text columns to Text`` () =
//    let ext = Sqlite.CustomTypes.TextTypeMapping() :> IExtendTypeMapping
//    let code = getCode [ext] [] cfg
//    // The extension should map "text" and "string" columns to Sqlite.CustomTypes.Text
//    code.Contains "Sqlite.CustomTypes.Text" =! true
//    // Other mappings should still work
//    code.Contains "int64" =! true
//    code.Contains "System.DateTime" =! true

////[<Test>]
////let ``PascalCaseNaming extension should rename rowguid to RowGuid`` () =
////    let ext = Sqlite.CustomNaming.PascalCaseNaming() :> IExtendNaming
////    let code = getCode [] [ext] cfg
////    // The extension should rename rowguid -> RowGuid
////    code.Contains "RowGuid" =! true
////    // Original name should not appear as a field
////    code.Contains "rowguid:" =! false
//#endif
