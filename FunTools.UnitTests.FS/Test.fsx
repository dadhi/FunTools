#I "D:\Dev\!2013\FunTools\packages\NUnit.2.6.2\lib"
#r "nunit.framework.dll"

#I "D:\Dev\!2013\FunTools\packages\FsUnit.1.1.1.0\Lib\Net20"
#r "FsUnit.NUnit.dll"

#load "FsUnitSample.fs"

open Tests
open NUnit.Framework
open FsUnit

let lightBulb = new LightBulb(true)
do lightBulb.On |> should be False
