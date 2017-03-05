module TwitterAlerts

open Serilog
open LoggingHelpers
open Akka.FSharp

open FSharp.Data.Toolbox.Twitter

type TwitterCreds = 
    {
        Key: string;
        Secret: string;
    }

let loadKeys () =
    let lines = System.IO.File.ReadLines(@"C:\Projects\twitterCreds.txt") |> Seq.toArray
    {
        Key = lines.[0].Trim();
        Secret = lines.[1].Trim();
    }

type WatcherMessages = 
    | StartListening of string

let receive (mailbox:Actor<'m>) : IO<'m> =
    mailbox.Receive()

let feed log creds =
    let log = logWith log "ActorName" "feed"
    (fun mailbox ->

        let twitter = Twitter.AuthenticateAppOnly(creds.Key, creds.Secret)
        let rec loop() = actor {
            let! message = receive mailbox

            match message with
            | StartListening(searchTerm) ->
                
                let results = twitter.Search.Tweets(searchTerm, count = 1)
                let newTweets = results.Statuses



            return! loop()
        }
        loop())


let watcher log creds =
    let log = logWith log "ActorName" "watcher"
    (fun mailbox ->
        let rec loop() = actor {
            let! message = receive mailbox

            match message with
            | StartListening(searchTerm) ->
                let feedRef = spawn mailbox null <| feed log creds
                feedRef <! StartListening(searchTerm)

            
            return! loop()
        }
        loop())

[<EntryPoint>]
let main argv =

    let twitterCreds = loadKeys ()

    let log =
        LoggerConfiguration()
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger()


    let watcher = watcher log


    0 // return an integer exit code
