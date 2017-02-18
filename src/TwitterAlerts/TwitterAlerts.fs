namespace TwitterAlerts
module Main =

    // Actors
    open Akka
    open Akka.Actor
    open Akka.FSharp
    open System

    open FSharp.Data.Toolbox.Twitter

    let key = "mKQL29XNemjQbLlQ8t0pBg"
    let secret = "T27HLDve1lumQykBUgYAbcEkbDrjBe6gwbu0gqi4saM"


    let schedule (system: ActorSystem) actor message =
        system
            .Scheduler
            .ScheduleTellRepeatedly(TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(5.0), actor, message);

    type TwitterFeed = TwitterFeed of string

    type WatcherMessages = 
        | WatchFeed of TwitterFeed

    type NewTweet = 
        {
            User: string;
            Text: string;
            Feed: TwitterFeed;
        }

    (*
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
    *)

    type FeedMessages = 
        | PollFeed

    let feed parent notifier feed =
        spawn parent "feed"
            (fun mailbox ->

                schedule mailbox.Context.System mailbox.Self PollFeed
                let twitter = Twitter.AuthenticateAppOnly(key, secret)

                let rec loop state = actor {
                    let! message = mailbox.Receive()

                    match message with
                    | PollFeed ->
                        let (TwitterFeed feedName) = feed
                        let latestTweets = twitter.Search.Tweets("#fsharp", count = 100)

                        for status in latestTweets.Statuses do
                            notifier <! { Feed = feed; User = "???"; Text = status.Text }

                    return! loop state
                }
                loop [])

    let watchers system notifier =
        spawn system "watchers"
            (fun mailbox ->
                let rec loop() = actor {
                    let! message = mailbox.Receive()

                    match message with
                    | WatchFeed(twitterFeed) ->
                        feed mailbox notifier twitterFeed |> ignore

                    return! loop()
                }
                loop())

    let notifier system =
        spawn system "notifier"
            (fun mailbox ->

                let notifier = WindowsNotifications.NotificationManager("TwitterAlerts")

                let rec loop() = actor {
                    let! (message:NewTweet) = mailbox.Receive()

                    let (TwitterFeed feedName) = message.Feed
                    notifier.Toast(sprintf "New Tweet: %s" feedName, message.Text, message.User)

                    return! loop()
                }
                loop())

    [<EntryPoint>]
    let main argv =

        use system = System.create "my-system" (Configuration.load())

        let notifier = notifier system
        let watcher = watchers system notifier

        watcher <! WatchFeed(TwitterFeed "Trump")

        system.WhenTerminated
            |> Async.AwaitTask
            |> Async.Start

        0 // return an integer exit code
