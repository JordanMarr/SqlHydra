module SqlHydra.Version

open System
open System.Reflection

type InformationalVersion = 
    { 
        /// Ex: 1.0.0-beta.1
        InformationalVersion: string
        /// Ex: 1.0.0
        Version: Version
        /// Ex: "beta.1"
        PreReleaseSuffix: string option 
    }
    member this.IsPreRelease = this.PreReleaseSuffix.IsSome
    override this.ToString() = this.InformationalVersion

/// Ex: 1.0.0-alpha.1
/// Ex: 1.0.0-beta.1
/// Ex: 1.0.0
let getInformationalVersion (info: AssemblyInformationalVersionAttribute) = 
    match info.InformationalVersion.Split([|'-'; '+'|], StringSplitOptions.RemoveEmptyEntries) with
    | [| root; suffix; _ |] -> 
        { InformationalVersion = $"{root}-{suffix}"; Version = Version(root); PreReleaseSuffix = Some suffix }
    | [| root; _ |] -> 
        { InformationalVersion = root; Version = Version(root); PreReleaseSuffix = None }
    | _ -> 
        failwith "Invalid version format"

let getInformationalVersionAttribute assemblyPath = 
    Reflection.Assembly.LoadFrom(assemblyPath).GetCustomAttributes(typeof<AssemblyInformationalVersionAttribute>, false)
    |> Seq.cast<AssemblyInformationalVersionAttribute>
    |> Seq.head

let get () = 
    Reflection.Assembly.GetAssembly(typeof<Console.Args>).Location
    |> getInformationalVersionAttribute
    |> getInformationalVersion
    |> _.InformationalVersion