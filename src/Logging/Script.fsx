#r "..\\..\\packages\\Serilog\\lib\\net45\\Serilog.dll"

#load "Logging.fs"

open FSharpTalk.Logging

let log = createLogger ()

let logInfo = logInfo log
logInfo "Log it!"

let logInfoFormat message arguments =
    printfn "%s" (arguments.GetType().Name)
    logInfoFormat log message arguments


logInfoFormat "Log {Message} and {number} and {char}" ["Everything!"; 1; 'b']


let a (x:seq<obj>) =
    Seq.length x

a [1; 2; 3; 4] |> printfn "%i"