namespace SqlHydra.Query

open System
open System.Threading
open System.Threading.Tasks

/// Factory for creating QueryContext instances.
/// A QueryContextFactory implementation is generated for each supported database provider.
type IQueryContextFactory =
    inherit IDisposable
#if NETSTANDARD2_1_OR_GREATER
    inherit IAsyncDisposable
#endif
    abstract member OpenContext: unit -> QueryContext
    abstract member OpenContextAsync: unit -> Task<QueryContext>
    abstract member OpenContextAsync: CancellationToken -> Task<QueryContext>
