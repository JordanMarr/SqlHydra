module SqlHydra.Extensions

open System
open System.IO
open System.Collections.Generic
open System.Reflection
open System.Runtime.Loader
open SqlHydra.Domain
open Microsoft.Build.Construction

/// Filters extensions to only those matching a specific extension interface.
let ofType<'T when 'T :> ISqlHydraExtension> (extensions: ISqlHydraExtension list) : 'T list =
    extensions |> List.choose (function :? 'T as x -> Some x | _ -> None)

let private markerType = typeof<ISqlHydraExtension>

/// An AssemblyLoadContext that resolves shared dependencies (e.g. SqlHydra.Domain, FSharp.Core)
/// back to the host's already-loaded assemblies, ensuring interface type identity is preserved.
type private ExtensionLoadContext(pluginPath: string) =
    inherit AssemblyLoadContext(isCollectible = true)

    let resolver = AssemblyDependencyResolver(pluginPath)

    override this.Load(assemblyName: AssemblyName) =
        // First, check if the host already has this assembly loaded (e.g. SqlHydra.Domain, FSharp.Core).
        // This ensures the extension's IExtendTypeMapping is the same type as the host's.
        let hostAsm =
            AssemblyLoadContext.Default.Assemblies
            |> Seq.tryFind (fun a -> a.GetName().Name = assemblyName.Name)
        match hostAsm with
        | Some asm -> asm
        | None ->
            match resolver.ResolveAssemblyToPath(assemblyName) with
            | null -> null
            | path -> this.LoadFromAssemblyPath(path)

/// Discovers all ISqlHydraExtension implementations in the given assembly.
/// Uses ReflectionTypeLoadException fallback to handle types whose dependencies aren't available.
let private discoverExtensions (asm: Assembly) =
    let types =
        try
            asm.GetTypes()
        with
        | :? ReflectionTypeLoadException as ex ->
            ex.Types |> Array.filter (fun t -> t <> null)

    types
    |> Array.filter (fun t ->
        not t.IsAbstract && not t.IsInterface &&
        markerType.IsAssignableFrom(t))
    |> Array.map (fun t -> Activator.CreateInstance(t) :?> ISqlHydraExtension)
    |> Array.toList

/// Loads an assembly from a DLL path and discovers ISqlHydraExtension implementations.
let private loadFromAssembly (dllPath: string) =
    let fullPath = Path.GetFullPath(dllPath)
    let loadContext = ExtensionLoadContext(fullPath)
    let asm = loadContext.LoadFromAssemblyPath(fullPath)
    discoverExtensions asm

/// Finds a DLL by name in the project's bin/ directory.
let private findDll (project: FileInfo) (dllName: string) =
    let binDir = Path.Combine(project.Directory.FullName, "bin")
    if Directory.Exists(binDir) then
        Directory.EnumerateFiles(binDir, dllName, SearchOption.AllDirectories)
        |> Seq.tryHead
    else
        None

/// Auto-scans the target project's own assembly for ISqlHydraExtension implementations.
let scanProject (project: FileInfo) : ISqlHydraExtension list =
    let projectName = Path.GetFileNameWithoutExtension(project.Name)
    match findDll project $"{projectName}.dll" with
    | Some path -> loadFromAssembly path
    | None -> []

/// Loads an ISqlHydraDbProvider from an assembly found in the project's build output.
/// The assembly must contain exactly one non-abstract class implementing ISqlHydraDbProvider.
let loadProvider (project: FileInfo) (assemblyName: string) : ISqlHydraDbProvider =
    let dllName = $"{assemblyName}.dll"
    let dllPath =
        match findDll project dllName with
        | Some path -> path
        | None -> failwith $"Could not find '{dllName}' in the build output of '{project.Name}'. Ensure the project has been built."

    let fullPath = Path.GetFullPath(dllPath)
    let loadContext = ExtensionLoadContext(fullPath)
    let asm = loadContext.LoadFromAssemblyPath(fullPath)

    let providerType = typeof<ISqlHydraDbProvider>
    let providers =
        let types =
            try asm.GetTypes()
            with :? ReflectionTypeLoadException as ex -> ex.Types |> Array.filter (fun t -> t <> null)
        types
        |> Array.filter (fun t ->
            not t.IsAbstract && not t.IsInterface &&
            providerType.IsAssignableFrom(t))

    match providers with
    | [| t |] -> Activator.CreateInstance(t) :?> ISqlHydraDbProvider
    | [||] -> failwith $"No ISqlHydraDbProvider implementation found in '{dllName}'."
    | _ -> failwith $"Multiple ISqlHydraDbProvider implementations found in '{dllName}'. Expected exactly one."

/// How a project brings an extension in.
type private ExtensionReference =
    | NotReferenced
    | AsPackage
    | AsProject

/// Nothing stops a project declaring both. `AsProject` wins and ends the search, because its
/// output is copied either way, which is what rules out the copy-local trap.
let private referenceKind (root: ProjectRootElement) (extName: string) =
    let rec search (items: IEnumerator<ProjectItemElement>) found =
        if not (items.MoveNext()) then
            found
        else
            match items.Current with
            | i when i.ItemType = "ProjectReference" && Path.GetFileNameWithoutExtension(i.Include) = extName -> AsProject
            | i when i.ItemType = "PackageReference" && i.Include = extName -> search items AsPackage
            | _ -> search items found

    use items = (root.ItemGroups |> Seq.collect _.Items).GetEnumerator()
    search items NotReferenced

/// Loads named extension assemblies (from TOML [extensions] config).
/// Each name must be a PackageReference, ProjectReference, or the target project itself.
let loadNamed (project: FileInfo) (extensionNames: string list) : ISqlHydraExtension list =
    extensionNames
    |> List.collect (fun extName ->
        let projectName = Path.GetFileNameWithoutExtension(project.Name)

        // The target project is its own extension source, so there is no reference to check.
        let root =
            if extName = projectName then None else Some(ProjectRootElement.Open(project.FullName))

        // Has to fire whether or not a dll is sitting in bin/: a stale one would load silently.
        if root |> Option.exists (fun r -> referenceKind r extName = NotReferenced) then
            failwith $"Extension '{extName}' was not found as a PackageReference or ProjectReference in '{project.Name}'."

        let dllName = $"{extName}.dll"
        match findDll project dllName with
        | None ->
            let hint =
                // Imports are not evaluated, so a value set in Directory.Build.props is
                // invisible here. That only costs the hint.
                let declared name =
                    root
                    |> Option.bind (fun r ->
                        r.Properties |> Seq.tryFind (fun p -> p.Name = name) |> Option.map _.Value)

                let referencedAsPackage =
                    root |> Option.exists (fun r -> referenceKind r extName = AsPackage)

                let says name value =
                    declared name |> Option.map (fun v -> v.Trim().Equals(value, StringComparison.OrdinalIgnoreCase))

                // An SDK-style project that says nothing builds a library.
                let buildsLibrary = says "OutputType" "Library" |> Option.defaultValue true
                let copyLocalOn = says "CopyLocalLockFileAssemblies" "true" |> Option.defaultValue false

                if referencedAsPackage && buildsLibrary && not copyLocalOn then
                    $" '{projectName}' builds a library and references '{extName}' as a package: a library does not copy "
                    + "package assemblies to its output directory, so there is nothing here to load. Add "
                    + "<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies> to it."
                else
                    ""

            failwith (
                $"Could not find '{dllName}' in the build output of '{project.Name}'. Ensure the project has been built."
                + hint
            )
        | Some path ->
            // A registered extension that yields no ISqlHydraExtension is always a mistake worth
            // stopping for: otherwise generation silently proceeds without the mapping and exits 0,
            // leaving the affected columns missing from the output with no error. Fail loudly and
            // point at the likely fixes.
            match loadFromAssembly path with
            | [] ->
                failwith (
                    $"Extension '{extName}' (loaded from '{path}') contains no ISqlHydraExtension implementations "
                    + "(e.g. an IExtendTypeMapping). "
                    + $"Check that '{extName}' in the TOML [extensions] section matches the package or assembly that "
                    + "implements the extension, and that it is referenced by the project. If the name is correct, the "
                    + "extension's types may have failed to load — ensure its dependencies are present and that it "
                    + "targets a compatible SqlHydra version."
                )
            | extensions -> extensions
    )
