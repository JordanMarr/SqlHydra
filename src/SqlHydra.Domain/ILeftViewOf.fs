namespace SqlHydra

/// The left-view of table record 'Table: the same record with every column in its nullable
/// form. A `leftJoin` over a left-view token binds the ON clause against plain 'Table and the
/// rest of the query against the view, so an unmatched row reads as None per column instead of
/// one Option<'Table> around the whole record. Generated views implement it, so the query
/// layer can map the view back to its physical table.
type ILeftViewOf<'Table> = interface end
