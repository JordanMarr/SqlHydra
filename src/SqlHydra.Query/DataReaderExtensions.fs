[<AutoOpen>]
module SqlHydra.Query.DataReaderExtensions

open System.Data

type IDataReader with 

    /// Gets a column by its ordinal with a generic return type.
    member this.Get<'Return> (ordinal: int) : 'Return =
        this.[ordinal] :?> 'Return

    /// Gets a column by its name with a generic return type.
    member this.Get<'Return> (column: string) : 'Return =
        column |> this.GetOrdinal |> this.Get

    /// Gets an optional column by its ordinal with a generic return type.
    member this.GetOption<'Return> (ordinal: int) : 'Return option =
        this.[ordinal] 
        |> Option.ofObj
        |> Option.bind (function
            | :? System.DBNull -> None
            | o -> o :?> 'Return |> Some
        )

    /// Gets an optional column by its name with a generic return type.
    member this.GetOption<'Return> (column: string) : 'Return option =
        column |> this.GetOrdinal |> this.GetOption