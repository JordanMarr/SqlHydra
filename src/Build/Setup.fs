[<RequireQualifiedAccess>]
module Setup

let commandLineArgs = System.Environment.GetCommandLineArgs()

let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" [ ]

/// Sets up the FAKE execution context
let context() = Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)