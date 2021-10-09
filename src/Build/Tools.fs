[<RequireQualifiedAccess>]
module Tools

open Nuke.Common.Tooling

let dotnet = ToolPathResolver.GetPathExecutable("dotnet")
