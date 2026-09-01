module SqlHydra.Extensions

open System
open System.IO
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

/// Loads named extension assemblies (from TOML [extensions] config).
/// Each name must be a PackageReference, ProjectReference, or the target project itself.
let loadNamed (project: FileInfo) (extensionNames: string list) : ISqlHydraExtension list =
    extensionNames
    |> List.collect (fun extName ->
        let projectName = Path.GetFileNameWithoutExtension(project.Name)

        // Allow the target project itself as an extension source
        let isTargetProject = extName = projectName

        if not isTargetProject then
            let root = ProjectRootElement.Open(project.FullName)
            let hasRef =
                root.ItemGroups
                |> Seq.collect _.Items
                |> Seq.exists (fun item ->
                    match item.ItemType with
                    | "PackageReference" -> item.Include = extName
                    | "ProjectReference" -> Path.GetFileNameWithoutExtension(item.Include) = extName
                    | _ -> false
                )
            if not hasRef then
                failwith $"Extension '{extName}' was not found as a PackageReference or ProjectReference in '{project.Name}'."

        let dllName = $"{extName}.dll"
        match findDll project dllName with
        | None ->
            failwith $"Could not find '{dllName}' in the build output of '{project.Name}'. Ensure the project has been built."
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

/// Applies the column-contribution extensions to a discovered schema.
///
/// Runs between discovery and emission, once, over the finished schema — not inside a schema
/// provider. A column a catalog does not list is not a provider concern: the same seam serves
/// all five providers, and an extension decides which one it is contributing to by reading
/// `Provider` off the context.
///
/// Extensions compose in registration order, each wrapping the last, which is the shape
/// `IExtendTypeMapping` and `IExtendNaming` already use: an extension can see what the ones
/// before it contributed, and drop from or add to that list.
///
/// A contributed name that a table already has raises. An override would be the more
/// permissive choice and the wrong one — the generated file still compiles, so a shadowed
/// column surfaces as a type error at some unrelated call site, or as nothing at all.
let contributeColumns
    (extensions: IContributeColumns list)
    (provider: ProviderType)
    (schema: Schema)
    : Schema =

    let contribute =
        let baseFn (_: ColumnContributionContext) : Column list = []
        extensions |> List.fold (fun acc (ext: IContributeColumns) -> ext.Contribute(acc)) baseFn

    let contributeTo (table: Table) =
        let contributed = contribute { Table = table; Provider = provider }
        let tableName = $"{table.Schema}.{table.Name}"

        contributed
        |> List.countBy _.Name
        |> List.tryFind (fun (_, count) -> count > 1)
        |> Option.iter (fun (name, count) ->
            failwith (
                $"Column-contribution extensions contributed '{name}' {count} times to '{tableName}'. "
                + "Each contributed column must be named once; an extension meaning to replace an "
                + "earlier contribution should filter it out of the list it is given."
            ))

        let discovered = table.Columns |> List.map _.Name |> Set.ofList

        contributed
        |> List.tryFind (fun col -> discovered.Contains col.Name)
        |> Option.iter (fun col ->
            failwith (
                $"A column-contribution extension contributed '{col.Name}' to '{tableName}', which already "
                + "has a column of that name. Contribution adds columns the provider could not discover; it "
                + "does not override discovered ones. Use an `IExtendTypeMapping` to retype a discovered "
                + "column, or an `IExtendNaming` to rename one."
            ))

        if contributed.IsEmpty
        then table
        else { table with Columns = table.Columns @ contributed }

    if extensions.IsEmpty
    then schema
    else { schema with Tables = schema.Tables |> List.map contributeTo }
