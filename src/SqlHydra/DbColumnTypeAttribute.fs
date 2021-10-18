module SqlHydra

open System
open SqlHydra.Domain

[<AttributeUsage(AttributeTargets.Property
                 ||| AttributeTargets.Field)>]
type DbColumnTypeAttribute(columnTypeName: string, columnTypeValue: string) =
    inherit Attribute()

    member this.CommandParameterType: CommandParameterType =
        { TypeName = columnTypeName
          TypeValue = columnTypeValue }
