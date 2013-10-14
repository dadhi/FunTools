module FunTools.Tests.ChangedTests

open System
open NUnit.Framework
open FsUnit
open FunTools.Changed

[<Test>] 
let ``Can create Changed from initial value`` () =
    let changed = Changed.From(3)
    changed.Value |> should equal 3

[<Test>]
let ``Can supply change condition while creating Changed object`` () = 
    let mutable changed = Changed.From(3, fun a b -> b > a)
    changed.Value <- 2
    changed.Value |> should equal 3
    changed.Value <- 4
    changed.Value |> should equal 4

[<Test>]
let ``Can subcribe to change event`` () = 
    let mutable changed = Changed.From()
    let isChanged = ref false
    changed.PropertyChanged.Subscribe(fun _ -> isChanged := true) |> ignore
    changed.Value <- "hey"
    !isChanged |> should be True

[<Test>]
let ``Should throw on recursive change attempt`` () = 
    let changed = Changed.From()
    changed.PropertyChanged.Subscribe(fun _ -> changed.Value <- 2) |> ignore
    (fun () -> changed.Value <- 1) |> should throw typeof<ChangedException>
