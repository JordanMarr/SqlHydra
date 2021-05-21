namespace SqlHydra

open System.Text.Json
    
type Table = {
    Catalog: string
    Schema: string
    Name: string
    Type: string
}
    
type Column = {
    TableCatalog: string
    TableSchema: string
    TableName: string
    ColumnName: string
    DataType: string
    IsNullable: bool
}

type Schema = {
    Tables: Table array
    Columns: Column array
}

module Utils = 
    let serializeSchema (schemaPath: string) (schema: Schema) =
        let json = JsonSerializer.Serialize(schema)
        System.IO.File.WriteAllText(schemaPath, json)

    let deserializeSchema (schemaPath: string) =
        let json = System.IO.File.ReadAllText(schemaPath)
        JsonSerializer.Deserialize<Schema>(json)
