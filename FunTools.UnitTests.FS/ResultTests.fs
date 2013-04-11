module ``Result tests``

open NUnit.Framework
open FsUnit
(*

Hurray!!! managed to run NUnit tests on module level
DON'T forget to use empty () tuple as let function parameter, to be compatible with c# NUnit
*)
[<Test>]
let ``Simple test that 1 is equal to 1`` () =
    1 |> should equal 1
