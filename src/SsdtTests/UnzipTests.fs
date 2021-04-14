module UnzipTests
open NUnit.Framework
open System.IO.Compression
open System

let dacPacPath =
    let srcDir = (System.IO.DirectoryInfo(".")).Parent.Parent.Parent.Parent
    IO.Path.Combine(srcDir.FullName, "AdventureWorks/bin/Debug/AdventureWorks.dacpac")

let extractModelXml(path: string) = 
    use stream = new IO.FileStream(path, IO.FileMode.Open)
    use zip = new ZipArchive(stream, ZipArchiveMode.Read, false)
    let modelEntry = zip.GetEntry("model.xml")
    use modelStream = modelEntry.Open()
    use rdr = new IO.StreamReader(modelStream)
    rdr.ReadToEnd()

[<Test>]
let ``Unzip Dacpac Model XML``() =
    let xml = extractModelXml(dacPacPath)
    printfn "XML: %s" xml