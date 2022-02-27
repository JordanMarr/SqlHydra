/// Utility functions for working with query tasks results.
module SqlHydra.Query.TaskResult

open System.Threading.Tasks

/// Maps the result of Task<'a> to Task<'b>
let map<'a, 'b> (f : 'a -> 'b) (tsk: Task<'a>) =
    async {
        let! result = tsk |> Async.AwaitTask
        return f result
    }
    |> Async.StartImmediateAsTask

/// Maps each individual item in Task<'a seq> and returns as a Task<'b seq>.
let mapSeq<'a, 'b> (f : 'a -> 'b) (itemsTask: Task<'a seq>) =
    itemsTask |> map (Seq.map f)

/// Maps each individual item in Task<'a seq> and returns as a Task<'b array>.
let mapArray<'a, 'b> (f : 'a -> 'b) (itemsTask: Task<'a seq>) =
    itemsTask |> map (Seq.map f >> Seq.toArray)

/// Maps each individual item in Task<'a seq> and returns as a Task<'b list>.
let mapList<'a, 'b> (f : 'a -> 'b) (itemsTask: Task<'a seq>) =
    itemsTask |> map (Seq.map f >> Seq.toList)

/// Awaits a Task and ignores the result.
let awaitIgnore (tsk: Task<'T>) = 
    map ignore tsk :> Task

/// An alias to `Task.FromResult`.
let fromResult value = 
    System.Threading.Tasks.Task.FromResult value
    