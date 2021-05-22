module SqlHydra.Schema

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

let internal errorSchema = 
    { Tables = [| 
        { Catalog = "Catalog"
          Schema = "dbo"
          Name = "Error"
          Type = "BASE TABLE" } |]
      Columns = [| 
        { TableCatalog = "Catalog" 
          TableSchema = "dbo"
          TableName = "Error"
          ColumnName = "Error"
          DataType = "nvarchar"
          IsNullable = false } |] }

let serialize (schemaPath: string) (schema: Schema) =
    let json = JsonSerializer.Serialize(schema)
    System.IO.File.WriteAllText(schemaPath, json)

let deserialize (schemaPath: string) =
    let json = System.IO.File.ReadAllText(schemaPath)
    JsonSerializer.Deserialize<Schema>(json)
