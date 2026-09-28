[<AutoOpen>]
module Util

open NUnit.Framework

/// Sequence length is > 0.
let gt0 (items: 'Item seq) =
    Assert.IsTrue(items |> Seq.length > 0, "Expected more than 0.")


type System.String with
    /// Used to temporarily revert unit tests that were upgraded to test the new v4 `select` behavior.
    member this.RemoveHydraExpr() =
        this.Replace(" AS __hydra_expr_0", "")


/// A minimal Config for exercising SchemaTemplate.generate without a live database.
let testConfig: SqlHydra.Domain.Config =
    {
        ConnectionString = ""
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        IsMutableProperties = false
        NullablePropertyType = SqlHydra.Domain.NullablePropertyType.Option
        ProviderDbTypeAttributes = true
        TableDeclarations = false
        LeftJoinedViews = false
        Readers = None
        Filters = SqlHydra.Domain.Filters.Empty
        TypeMappingExtensions = []
    }

let testVersion: SqlHydra.Version.InformationalVersion =
    {
        InformationalVersion = "0.0.0"
        Version = System.Version(0, 0, 0)
        PreReleaseSuffix = None
    }
