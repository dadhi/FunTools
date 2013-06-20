module FunTools.Tests.``Download two sites in parallel and handle possible download errors``

open FunTools
open NUnit.Framework
open FsUnit
open System
open System.Net

let downloadAsync uri =
    let url = new Uri(uri)
    Await(fun complete ->
        let webClient = new Net.WebClient()
        let download = 
            Await.Event<DownloadDataCompletedEventArgs, DownloadDataCompletedEventHandler, string>(
                (fun e ->
                    if e.Error <> null then e.Error.ReThrow()
                    Some.Of(Text.Encoding.ASCII.GetString e.Result)),
                (fun h -> webClient.DownloadDataCompleted.AddHandler(h)),
                (fun h -> webClient.DownloadDataCompleted.RemoveHandler(h)),
                (fun a -> DownloadDataCompletedEventHandler(fun o e -> a.Invoke(o, e))))
                .Invoke(complete)
        
        webClient.DownloadDataAsync(url)
        download)

[<Test>]
let ``When one site download fails but another succeeds Then result should contain another one`` () =
    let errors = ref []
    let result = 
        Await.Many(
            (fun (x : Result<_>) _ ->
                if x.IsSuccess then Some.Of(x.Success) 
                else
                    errors := x.Failure :: !errors
                    None.Of<string>()),
            null,
            [|"http://гыгы.com"; "http://www.infoq.com"|] |> Array.map downloadAsync
            ).WaitSuccess()
    
    result |> should contain "infoq"
    !errors |> should haveLength 1