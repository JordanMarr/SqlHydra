module SqlHydra.DbColumnTypeAttribute

open System
open SqlHydra.Domain

[<AttributeUsage(AttributeTargets.Property
                 ||| AttributeTargets.Field)>]
type DbColumnTypeAttribute(columnTypeName: string, columnTypeValue: string) =
    inherit Attribute()

    member this.ColumnType: DbColumnType =
        { TypeName = columnTypeName
          TypeValue = columnTypeValue }
