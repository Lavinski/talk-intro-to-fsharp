#r @"..\..\packages\Serilog\lib\net45\Serilog.dll"
#load "LoggingHelpers.fs"
open LoggingHelpers

// Define your library scripting code here


let doThingB log =
    logInfo log "Did it!"

let doThingA log =
    doThingB log



