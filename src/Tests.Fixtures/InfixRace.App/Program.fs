/// Runs SqlHydra's first infix-operator scan in a fresh process, loads a DLL while that scan is
/// running, and prints the SQL for a function that DLL declares an operator for.
module InfixRace.App.Program

open System
open System.Reflection
open System.Threading
open SqlHydra.Query

/// SqlHydra creates this attribute when its first scan reads this DLL's attributes. Its
/// constructor holds the scan there until another thread has loaded InfixRace.Late.
type LoadLateDuringScanAttribute() =
    inherit SqlHydraInfixOperatorAttribute("race_gate", "<?>")
    do
        let otherThread = Thread(fun () -> Assembly.Load("InfixRace.Late") |> ignore)
        otherThread.Start()
        otherThread.Join()

[<assembly: LoadLateDuringScan>]
do ()

type Point = { x: float; y: float }

[<SqlHydraFunction>]
let fixture_fn (a: float, b: float) : float = sqlFn

[<EntryPoint>]
let main _ =
    if AppDomain.CurrentDomain.GetAssemblies() |> Array.exists (fun a -> a.GetName().Name = "InfixRace.Late") then
        failwith "InfixRace.Late loaded before the first scan, so this run can't show the race."
    let query = select { for p in table<Point> do select (fixture_fn (p.x, p.y)) }
    printfn "%s" (query.CompileWith(PostgresEmitter()).Sql)
    0
