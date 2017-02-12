module TwitterAlerts

// Actors
open Akka
open Akka.Actor
open Akka.FSharp
open System

let schedule (system: ActorSystem) actor message =
    system
        .Scheduler
        .ScheduleTellRepeatedly(TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(5.0), actor, message);

type TwitterFeed = TwitterFeed of string

type ManagerMessages = 
    | WatchFeed of TwitterFeed

let manager system watchers notifier =
    spawn system "manager"
        (fun mailbox ->
            let rec loop() = actor {
                let! message = mailbox.Receive()
                
                match message with
                | WatchFeed(twitterFeed) -> 
                    // Do validation
                    watchers <! message

                return! loop()
            }
            loop())

type FeedMessages = 
    | PollFeed

let feed parent feed =
    spawn parent "feed"
        (fun mailbox ->

            schedule mailbox.Context.System mailbox.Self PollFeed

            let rec loop() = actor {
                let! message = mailbox.Receive()
                
                match message with
                | PollFeed ->
                    // Call out to twitter
                    // Send results back to parent
                    ()

                return! loop()
            }
            loop())

let watchers system =
    spawn system "watchers"
        (fun mailbox ->
            let rec loop() = actor {
                let! message = mailbox.Receive()
                
                match message with
                | WatchFeed(twitterFeed) ->
                    // Do validation
                    feed mailbox twitterFeed |> ignore

                return! loop()
            }
            loop())

let notifier system =
    spawn system "notifier"
        (fun mailbox ->
            let rec loop() = actor {
                let! message = mailbox.Receive()
                
                //

                return! loop()
            }
            loop())

// Twitter

open FSharp.Data.Toolbox.Twitter

let key = "mKQL29XNemjQbLlQ8t0pBg"
let secret = "T27HLDve1lumQykBUgYAbcEkbDrjBe6gwbu0gqi4saM"



// Windows Notifications

[<EntryPoint>]
let main argv =
    
    use system = System.create "my-system" (Configuration.load())

    let twitter = Twitter.AuthenticateAppOnly(key, secret)

    let fsharpTweets = twitter.Search.Tweets("#fsharp", count=100)

    for status in fsharpTweets.Statuses do
        printfn "@%s: %s" status.User.ScreenName status.Text


    0 // return an integer exit code
