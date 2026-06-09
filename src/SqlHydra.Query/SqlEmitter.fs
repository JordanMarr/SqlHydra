namespace SqlHydra.Query

open SqlHydra.Domain

/// Result of compiling a query IR to SQL.
[<NoComparison>]
type CompiledQuery = {
    Sql: string
    /// Parameter name * value pairs (values may be QueryParameter wrappers)
    Parameters: (string * obj) list
}

/// Interface for SQL generation. Implement for custom database providers.
[<Interface>]
type ISqlEmitter =
    /// The database provider type.
    abstract Provider: ProviderType
    /// Emit a SELECT query from IR.
    abstract EmitSelect: SelectQueryIR -> CompiledQuery
    /// Emit an INSERT query from IR.
    abstract EmitInsert: InsertQueryIR -> CompiledQuery
    /// Emit an UPDATE query from IR.
    abstract EmitUpdate: UpdateQueryIR -> CompiledQuery
    /// Emit a DELETE query from IR.
    abstract EmitDelete: DeleteQueryIR -> CompiledQuery
