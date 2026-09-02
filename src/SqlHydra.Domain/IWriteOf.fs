namespace SqlHydra

/// The write shape of table record 'Table: a record holding only the columns a caller may
/// INSERT or UPDATE. A read-only column has no field here, so writing it cannot be expressed.
type IWriteOf<'Table> = interface end
