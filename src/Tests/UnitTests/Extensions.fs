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
let private withTempProjectXml (projName: string) (xml: string) (placeDll: bool) (name: string) (run: FileInfo -> unit) =
    let root = Path.Combine(Path.GetTempPath(), $"sqlhydra-ext-{Guid.NewGuid():N}")
    let proj = FileInfo(Path.Combine(root, $"{projName}.fsproj"))
    Directory.CreateDirectory(root) |> ignore
    File.WriteAllText(proj.FullName, xml)
    if placeDll then
        let bin = Directory.CreateDirectory(Path.Combine(root, "bin"))
        let asm = typeof<SqlHydra.Domain.ISqlHydraExtension>.Assembly
        File.Copy(asm.Location, Path.Combine(bin.FullName, $"{name}.dll"))
    try run proj
    finally (try Directory.Delete(root, true) with _ -> ())

let private consumerReferencing (itemType: string) (extName: string) (extraProps: string) =
    $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>{extraProps}</PropertyGroup>
  <ItemGroup><{itemType} Include="{extName}" /></ItemGroup>
</Project>"""

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

[<Test>]
let ``a registered extension that is not referenced raises even when its dll is present`` () =
    // Do not fold the reference check into the missing-dll path: a stale dll would make the
    // lookup succeed and the extension would load unreferenced.
    let noRef = """<Project Sdk="Microsoft.NET.Sdk"></Project>"""
    withTempProjectXml "Consumer" noRef true "Ext" (fun proj ->
        let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed proj [ "Ext" ] |> ignore)
        test <@ ex.Message.Contains "was not found as a PackageReference or ProjectReference" @>)

[<Test>]
let ``a library referencing the extension as a package is told about CopyLocalLockFileAssemblies`` () =
    withTempProjectXml "Consumer" (consumerReferencing "PackageReference" "Ext" "") false "Ext" (fun proj ->
        let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed proj [ "Ext" ] |> ignore)
        test <@ ex.Message.Contains("CopyLocalLockFileAssemblies") @>)

[<Test>]
let ``an executable is not told about CopyLocalLockFileAssemblies`` () =
    let props = "<OutputType>Exe</OutputType>"
    withTempProjectXml "Consumer" (consumerReferencing "PackageReference" "Ext" props) false "Ext" (fun proj ->
        let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed proj [ "Ext" ] |> ignore)
        test <@ not (ex.Message.Contains "CopyLocalLockFileAssemblies") @>)

[<Test>]
let ``a project reference is not told about CopyLocalLockFileAssemblies`` () =
    withTempProjectXml "Consumer" (consumerReferencing "ProjectReference" "Ext" "") false "Ext" (fun proj ->
        let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed proj [ "Ext" ] |> ignore)
        test <@ not (ex.Message.Contains "CopyLocalLockFileAssemblies") @>)

[<Test>]
let ``a project declaring both kinds of reference is not told about CopyLocalLockFileAssemblies`` () =
    // A project reference is copied to the output either way, so the copy-local trap is ruled
    // out even though a PackageReference is also declared.
    // Both orderings: whichever is declared first, the project reference decides.
    let both (first: string) (second: string) =
        $"""<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    {first}
    {second}
  </ItemGroup>
</Project>"""
    let pkg = """<PackageReference Include="Ext" />"""
    let proj = """<ProjectReference Include="../Ext/Ext.fsproj" />"""

    for xml in [ both pkg proj; both proj pkg ] do
        withTempProjectXml "Consumer" xml false "Ext" (fun p ->
            let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed p [ "Ext" ] |> ignore)
            test <@ not (ex.Message.Contains "CopyLocalLockFileAssemblies") @>)

[<Test>]
let ``a library that already set CopyLocalLockFileAssemblies is not told again`` () =
    let props = "<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>"
    withTempProjectXml "Consumer" (consumerReferencing "PackageReference" "Ext" props) false "Ext" (fun proj ->
        let ex = Assert.Throws<Exception>(fun () -> Extensions.loadNamed proj [ "Ext" ] |> ignore)
        test <@ not (ex.Message.Contains "CopyLocalLockFileAssemblies") @>)
