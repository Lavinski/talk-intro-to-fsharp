namespace FSharpTalk

module Logging =

    open System
    open Serilog
    //open Microsoft.FSharp.Collections

    let createLogger () =
        Serilog.LoggerConfiguration()
            .WriteTo.Sink({ new Core.ILogEventSink with 
                member this.Emit(logEvent) = 
                    Console.WriteLine(logEvent.RenderMessage())
                })
            .CreateLogger()


    let logInfo (logger:ILogger) message =
        logger.Information(message)

    let logInfoFormat (logger:ILogger) message (arguments:seq<obj>) =
        logger.Information(message, arguments |> Seq.toArray)


    let logError (logger:ILogger) message =
        logger.Error(message)

    let logErrorFormat (logger:ILogger) message (arguments:seq<obj>) =
        logger.Error(message, arguments |> Seq.toArray)

    let logErrorExn (exn:exn) (logger:ILogger) message =
        logger.Error(exn, message)

    let logErrorExnFormat exn (logger:ILogger) message (arguments:seq<obj>) =
        logger.Error(exn, message, arguments |> Seq.toArray)