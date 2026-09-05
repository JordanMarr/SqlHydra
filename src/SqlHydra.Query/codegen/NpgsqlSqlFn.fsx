// Generates the PostgreSQL `SqlFn` members from `pg_proc`, the catalog the database resolves calls
// against. The allowlist chooses WHICH functions appear; the catalog decides their SHAPE: argument
// and return types, and `proisstrict` (NULL in means NULL out), which gives every parameter of a
// strict function a `'T option` twin. Each member is executed once against the database, so nothing
// that cannot be called as `NAME(args)` is emitted; a keyword-named function such as `position`
// renders schema-qualified instead.
//
//   dotnet fsi NpgsqlSqlFn.fsx                    rewrite the generated region of NpgsqlExtensions.fs
//   dotnet fsi NpgsqlSqlFn.fsx --check            exit 1 if that region is stale (CI)
//   dotnet fsi NpgsqlSqlFn.fsx --conn "..." --allowlist my-fns.txt --schema public \
//       --map vector=Pgvector.Vector --module My.Sql --type PgFn --out src/PgFn.fs
//                                                 emit a standalone type for your own database
//
// Allowlist: one line per overload, `name[=catalog_name] param:type ...`, `#` starts a comment.
// Types are F# (`string`, `int`, `DateTime`, `byte[]`); write ``to`` for an F# keyword. A line
// matches the catalog overload with those parameter types; a variadic function accepts any list.
// `trim=btrim s:string` emits `trim` with `btrim`'s shape.
#r "nuget: Npgsql, 9.0.3"

open System
open System.IO
open Npgsql

// ---------------------------------------------------------------- arguments

let args = fsi.CommandLineArgs |> Array.skip 1 |> List.ofArray

let rec optValues name = function
    | k :: v :: rest when k = name -> v :: optValues name rest
    | _ :: rest -> optValues name rest
    | [] -> []
let optValue name = optValues name >> List.tryHead

let scriptDir = Path.GetDirectoryName fsi.CommandLineArgs.[0]
let check = List.contains "--check" args
let conn = optValue "--conn" args |> Option.defaultValue "Server=localhost;Port=54320;Database=Adventureworks;User Id=postgres;Password=postgres;Timeout=3"
let allowlistPath = optValue "--allowlist" args |> Option.defaultValue (Path.Combine(scriptDir, "NpgsqlSqlFn.allowlist"))
let outPath = optValue "--out" args
let typeName = optValue "--type" args |> Option.defaultValue "SqlFn"
let moduleName = optValue "--module" args
let schemas = "pg_catalog" :: optValues "--schema" args
let regionFile = Path.Combine(scriptDir, "..", "NpgsqlExtensions.fs")

// ---------------------------------------------------------------- types

/// Catalog type (as `format_type` prints it) to the F# type the visitor binds. The column map in
/// src/SqlHydra.Cli/Npgsql/NpgsqlDataTypes.fs is the reference; `date` is DateTime here because
/// the members always were. A variadic parameter is probed as the first catalog type of its F# type.
let builtinTypes =
    [ "text", "string"; "character varying", "string"; "character", "string"; "name", "string"
      "citext", "string"; "json", "string"; "jsonb", "string"; "xml", "string"
      "integer", "int"; "smallint", "int16"; "bigint", "int64"
      "double precision", "float"; "real", "float32"; "numeric", "decimal"; "money", "decimal"
      "boolean", "bool"; "uuid", "Guid"; "bytea", "byte[]"
      "timestamp without time zone", "DateTime"; "timestamp with time zone", "DateTime"; "date", "DateTime"
      "interval", "TimeSpan"; "time without time zone", "TimeSpan" ]

let userTypes =
    optValues "--map" args
    |> List.map (fun kv ->
        match kv.Split('=', 2) with
        | [| pg; clr |] -> pg, clr
        | _ -> failwith $"--map expects pgtype=ClrType, got '{kv}'")

let fsharpType pg = Map.ofList (builtinTypes @ userTypes) |> Map.tryFind pg
let catalogType clr = userTypes @ builtinTypes |> List.tryFind (fun (_, c) -> c = clr) |> Option.map fst

// ---------------------------------------------------------------- allowlist

type Entry = { Member: string; Catalog: string; Params: (string * string) list }

let entries =
    File.ReadAllLines allowlistPath
    |> Array.map (fun l -> l.Split('#').[0].Trim())
    |> Array.filter ((<>) "")
    |> Array.map (fun line ->
        match line.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
        | [] -> failwith "unreachable: blank lines are filtered"
        | name :: ps ->
            let memberName, catalogName =
                match name.Split('=', 2) with
                | [| m; c |] -> m, c
                | _ -> name, name
            let ps =
                ps |> List.map (fun p ->
                    match p.Split(':', 2) with
                    | [| n; t |] -> n, t
                    | _ -> failwith $"allowlist '{line}': parameters are written name:type")
            { Member = memberName; Catalog = catalogName; Params = ps })
    |> List.ofArray

// ---------------------------------------------------------------- catalog

type Overload = { Schema: string; Name: string; Strict: bool; Variadic: bool; Args: string []; Ret: string }

let db = new NpgsqlConnection(conn)
db.Open()

let catalog =
    use cmd = db.CreateCommand()
    cmd.CommandText <- """
        SELECT p.pronamespace::regnamespace::text, p.proname, p.proisstrict, p.provariadic <> 0,
               array(SELECT format_type(t, NULL) FROM unnest(p.proargtypes) t),
               format_type(p.prorettype, NULL)
        FROM pg_proc p
        WHERE p.pronamespace = ANY(@schemas::regnamespace[])
          AND p.prokind = 'f' AND p.proname = ANY(@names)
        ORDER BY p.proname, p.pronargs, p.proargtypes::text"""
    cmd.Parameters.AddWithValue("names", entries |> List.map (fun e -> e.Catalog) |> List.distinct |> List.toArray) |> ignore
    cmd.Parameters.AddWithValue("schemas", List.toArray schemas) |> ignore
    use r = cmd.ExecuteReader()
    [ while r.Read() do
        { Schema = r.GetString 0; Name = r.GetString 1; Strict = r.GetBoolean 2; Variadic = r.GetBoolean 3
          Args = r.GetFieldValue<string[]> 4; Ret = r.GetString 5 } ]

let describe (o: Overload) = $"""({String.Join(", ", o.Args)}) -> {o.Ret}"""

/// A member to emit: the allowlist line, its catalog overload, and the catalog types to probe with.
type Member = { Entry: Entry; Overload: Overload; ProbeArgs: string []; Ret: string; SqlName: string option }

let resolve (e: Entry) =
    let candidates = catalog |> List.filter (fun o -> o.Name = e.Catalog)
    let wanted = e.Params |> List.map snd
    let ret (o: Overload) =
        fsharpType o.Ret |> Option.defaultWith (fun () -> failwith $"{e.Member}: return type {o.Ret} has no F# type; add --map {o.Ret}=<Type>")
    let exact = candidates |> List.tryFind (fun o -> not o.Variadic && (o.Args |> Array.map fsharpType |> List.ofArray) = List.map Some wanted)
    match exact, candidates |> List.tryFind (fun o -> o.Variadic) with
    | Some o, _ -> { Entry = e; Overload = o; ProbeArgs = o.Args; Ret = ret o; SqlName = None }
    | None, Some o ->
        let probeArgs =
            wanted |> List.map (fun t -> catalogType t |> Option.defaultWith (fun () -> failwith $"{e.Member}: no catalog type maps to {t}; add --map <pgtype>={t}"))
        { Entry = e; Overload = o; ProbeArgs = List.toArray probeArgs; Ret = ret o; SqlName = None }
    | None, None when candidates.IsEmpty ->
        failwith $"""{e.Catalog}: not a plain function in {String.Join("/", schemas)}. Keyword sugar has a catalog name (trim=btrim); expression nodes (coalesce, nullif) and bare keywords (current_date) are hand-written."""
    | None, None ->
        failwith $"""{e.Member}({String.Join(", ", wanted)}): no such overload. The catalog has {String.Join("; ", candidates |> List.map describe)}"""

/// Runs `name(NULL::t1, …)` once. A spelling PostgreSQL rejects as syntax (`position(a, b)`) is
/// retried schema-qualified, and the member then renders that spelling.
let probe (m: Member) =
    let nulls = m.ProbeArgs |> Array.map (fun t -> $"NULL::{t}") |> String.concat ", "
    let parses (spelling: string) =
        try
            use cmd = db.CreateCommand()
            cmd.CommandText <- $"SELECT {spelling}({nulls})"
            cmd.ExecuteScalar() |> ignore
            true
        with :? PostgresException as ex when ex.SqlState = "42601" -> false
    let qualified = $"{m.Overload.Schema}.{m.Overload.Name}"
    if parses m.Entry.Member then m
    elif parses qualified then { m with SqlName = Some qualified }
    else failwith $"{m.Entry.Member}({nulls}) cannot be called as a function, even as {qualified}"

let members = entries |> List.map (resolve >> probe)
db.Close()

// ---------------------------------------------------------------- emit

let memberLines (m: Member) =
    let attribute = m.SqlName |> Option.map (fun n -> $"    [<SqlHydraFunction(\"{n}\")>]") |> Option.toList
    let line (ps: (string * string) list) ret =
        let plist = ps |> List.map (fun (n, t) -> $"{n}: {t}") |> String.concat ", "
        attribute @ [ $"    static member {m.Entry.Member}({plist}) : {ret} = sqlFn" ]
    [ yield! line m.Entry.Params m.Ret
      if m.Overload.Strict then
          for i in 0 .. m.Entry.Params.Length - 1 do
              let lifted = fst m.Entry.Params.[i]
              yield $"    /// NULL `{lifted}` is NULL out: hydrates as None, and `= None` renders IS NULL; compare with `= Some x`."
              yield! line (m.Entry.Params |> List.mapi (fun j (n, t) -> if i = j then n, $"{t} option" else n, t)) $"{m.Ret} option" ]

let header = "    // <generated> by codegen/NpgsqlSqlFn.fsx from pg_proc; edit the allowlist, not this block."
let footer = "    // </generated>"
let lines = members |> List.collect memberLines

match outPath with
| Some path ->
    let modLine = moduleName |> Option.map (fun m -> $"module {m}\n\n") |> Option.defaultValue ""
    let body = header :: lines @ [ footer ] |> String.concat "\n"
    File.WriteAllText(path, $"{modLine}open System\nopen SqlHydra.Query\n\n[<SqlHydraFunction>]\ntype {typeName} =\n{body}\n")
    printfn "wrote %s (%d members)" path lines.Length
| None ->
    let existing = File.ReadAllLines regionFile |> List.ofArray
    let marker (text: string) = existing |> List.tryFindIndex (fun l -> l.Trim().StartsWith text)
    match marker "// <generated>", marker "// </generated>" with
    | Some s, Some e when s < e ->
        let updated = existing.[.. s - 1] @ header :: lines @ footer :: existing.[e + 1 ..]
        if updated = existing then
            printfn "%s is up to date (%d generated members)" regionFile lines.Length
        elif check then
            let current = existing.[s + 1 .. e - 1]
            eprintfn "%s is stale: %d generated members on disk, %d from the catalog. Run: dotnet fsi %s" regionFile current.Length lines.Length fsi.CommandLineArgs.[0]
            for l in lines do if not (List.contains l current) then eprintfn "  + %s" (l.Trim())
            for l in current do if not (List.contains l lines) then eprintfn "  - %s" (l.Trim())
            exit 1
        else
            File.WriteAllLines(regionFile, updated)
            printfn "rewrote the generated region of %s (%d members)" regionFile lines.Length
    | _ -> failwith $"{regionFile} has no `// <generated>` ... `// </generated>` region"
