module FunTools.Tests.ComputedTests

open System
open System.Linq
open NUnit.Framework
open FsUnit
open FunTools.Changed

let [<Test>] ``Given computed with one changed When getting computed value Then value should use changed value`` () =
    let changed = Changed.From("Hello")
    let computed = Computed.From(fun () -> changed.Value + "!")
    computed.Value |> should equal "Hello!"

let [<Test>] ``Given computed with one changed When getting computed value Then the changed should be observed`` () =
    let changed = Changed.From("Hello")
    let computed = Computed.From(fun () -> changed.Value + "!")
    computed.Value |> ignore
    computed.Observed.Count() |> should equal 1

let [<Test>] ``Given computed with two changed When getting computed value Then both changed should be observed`` () =
    let message = Changed.From("Hello #")
    let count = Changed.From(0)
    let computed = Computed.From(fun () -> sprintf "%s #%i" message.Value count.Value)
    computed.Value |> ignore
    computed.Observed.Count() |> should equal 2

let [<Test>] ``Given computed with two changed When one changes value Then computed is notified`` () =
    let message = Changed.From("Hello #")
    let count = Changed.From(0)
    let computed = Computed.From(fun () -> sprintf "%s #%i" message.Value count.Value)
    let isChanged = ref false
    computed.PropertyChanged.Subscribe(fun _ -> isChanged := true) |> ignore
    count.Value <- 3
    !isChanged |> should be True