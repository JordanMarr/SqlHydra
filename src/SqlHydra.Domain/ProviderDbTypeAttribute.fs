namespace SqlHydra

open System

[<AttributeUsage(AttributeTargets.Property
                 ||| AttributeTargets.Field)>]
type ProviderDbTypeAttribute(providerDbTypeName: string) =
    inherit Attribute()

    member this.ProviderDbTypeName = providerDbTypeName
