module UnitTests.Extensions

open System
open System.IO
open NUnit.Framework
open Swensen.Unquote
open SqlHydra

// Lay out a throwaway project the way loadNamed resolves one in production: a `.fsproj` named
// after the extension (so it's treated as the target project and skips the reference check),
// optionally with `{name}.dll` in bin/. For `placeDll`, SqlHydra.Domain is a real, loadable
// assembly that defines the extension interfaces but has no concrete ISqlHydraExtension — i.e.
// the "named but empty" case (the test assembly itself can't be used: it defines TextTypeMapping).
let private withTempProject (name: string) (placeDll: bool) (run: FileInfo -> unit) =
    let root = Path.Combine(Path.GetTempPath(), $"sqlhydra-ext-{Guid.NewGuid():N}")
    let proj = FileInfo(Path.Combine(root, $"{name}.fsproj"))
    Directory.CreateDirectory(root) |> ignore
    File.WriteAllText(proj.FullName, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>")
    if placeDll then
        let bin = Directory.CreateDirectory(Path.Combine(root, "bin"))
        let asm = typeof<SqlHydra.Domain.ISqlHydraExtension>.Assembly
        File.Copy(asm.Location, Path.Combine(bin.FullName, $"{name}.dll"))
    try run proj
    finally (try Directory.Delete(root, true) with _ -> ())

[<Test>]
let ``loadNamed raises when a registered extension yields no implementations`` () =
    let name = typeof<SqlHydra.Domain.ISqlHydraExtension>.Assembly.GetName().Name
    withTempProject name true (fun proj ->
        let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed proj [ name ] |> ignore)
        test <@ ex.Message.Contains(name) @> // names the offending extension
        test <@ ex.Message.Contains("no ISqlHydraExtension") @>)

[<Test>]
let ``loadNamed raises when the named dll is missing from build output`` () =
    withTempProject "Missing" false (fun proj ->
        let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed proj [ "Missing" ] |> ignore)
        test <@ ex.Message.Contains("Missing") @>)
