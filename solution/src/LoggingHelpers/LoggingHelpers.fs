module LoggingHelpers

open Serilog

let logInfo (log:ILogger) message =
    log.Information(message)

let logInfoWith (log:ILogger) message (args:seq<obj>) =
    log.Information(message, args |> Seq.toArray)

let logWith (log:ILogger) (propertyName:string) (value:string) =
    log.ForContext(propertyName, value)
