/// Stands in for an extension package. InfixRace.App loads it during SqlHydra's first scan.
module InfixRace.Late

[<assembly: SqlHydra.Query.SqlHydraInfixOperator("fixture_fn", "<~>")>]
do ()
