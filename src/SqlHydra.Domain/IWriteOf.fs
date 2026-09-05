namespace SqlHydra

/// One column a row writes: the value to bind and, when the schema declared one, the provider
/// type to bind it as (what `[<ProviderDbType>]` carries on the field).
type WriteColumn =
    { Name: string
      Value: obj
      ProviderDbType: string option }

/// The columns a record may INSERT or UPDATE, with this row's values. Generated records implement
/// it, so the query layer never reflects over them; a table record with a database-owned column
/// lists only the others, so `entity row` never names what the database refuses.
type IWriteColumns =
    abstract WriteColumns: WriteColumn list

/// The write shape of table record 'Table: a record holding only the columns a caller may
/// INSERT or UPDATE. A read-only column has no field here, so writing it cannot be expressed.
type IWriteOf<'Table> =
    inherit IWriteColumns

/// A table record that projects onto its write shape; `toWrite row` calls it.
type IHasWrite<'Write> =
    abstract ToWrite: unit -> 'Write
