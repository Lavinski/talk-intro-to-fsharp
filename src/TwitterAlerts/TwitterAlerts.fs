namespace TwitterAlerts
module Main =

    open Akka
    open Akka.Actor
    open Akka.FSharp
    open System
    open Serilog
    open FSharpTalk.Logging
    open FSharp.Data.Toolbox.Twitter

    let key = "mKQL29XNemjQbLlQ8t0pBg"
    let secret = "T27HLDve1lumQykBUgYAbcEkbDrjBe6gwbu0gqi4saM"

    // Types

    type TwitterFeed = 
        TwitterFeed of string

    type WatcherMessages = 
        | WatchFeed of TwitterFeed

    type NewTweet = 
        {
            User: string;
            Text: string;
            Feed: TwitterFeed;
        }

    type FeedMessages = 
        | PollFeed

    type ConsoleMessages = 
        | ReadNextLine

    // Functions

    let schedule (system: ActorSystem) actor message =
        system
            .Scheduler
            .ScheduleTellRepeatedly(TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(5.0), actor, message);

    let logWith (log: ILogger) (propertyName:string) (value:obj) =
        log.ForContext(propertyName, value)

    // Actors

    let console log system watchers =
        let log = logWith log "ActorName" "Console"
        spawn system "console"
            (fun mailbox ->

                mailbox.Self <! ReadNextLine

                let rec loop() = actor {
                    let! message = mailbox.Receive()

                    match message with
                    | ReadNextLine -> 
                        let input = Console.ReadLine().Trim()

                        match input with
                        | "exit" -> 
                            mailbox.Context.System.Terminate() |> ignore
                            return ()
                        | _ ->
                            watchers <! WatchFeed(TwitterFeed(input))
                            mailbox.Self <! ReadNextLine
                            return! loop()
                }
                loop())

    let feed log parent notifier feed =
        let log = logWith log "ActorName" "Feed"
        spawnOpt parent null
            (fun mailbox ->
                schedule mailbox.Context.System mailbox.Self PollFeed
                let twitter = Twitter.AuthenticateAppOnly(key, secret)

                let rec loop state = actor {
                    let! message = mailbox.Receive()

                    let newState = 
                        match message with
                        | PollFeed ->
                            let (TwitterFeed feedName) = feed
                            logInfoFormat log "Polling feed {@FeedName} for new tweets" [ feedName ]

                            let latestTweets = twitter.Search.Tweets(feedName, count = 1)

                            let newTweets = latestTweets.Statuses |> Seq.filter (fun tweet -> not (state |> List.contains tweet.Id))

                            if not (state |> List.isEmpty) then
                                for status in newTweets do
                                    notifier <! { Feed = feed; User = status.User.Name; Text = status.Text }

                            let seenTweets = latestTweets.Statuses |> Seq.map (fun tweet -> tweet.Id) |> Seq.toList
                            seenTweets

                    return! loop newState
                }
                loop []
            )
            [
                SpawnOption.SupervisorStrategy (Strategy.OneForOne (fun error -> Directive.Stop))
            ]

    let watchers log system notifier =
        let log = logWith log "ActorName" "Watchers"
        spawnOpt system "watchers"
            (fun mailbox ->
                let rec loop() = actor {
                    let! message = mailbox.Receive()

                    match message with
                    | WatchFeed(twitterFeed) ->
                        logInfoFormat log "Watching new twitter feed {@TwitterFeed}" [ twitterFeed ]
                        feed log mailbox notifier twitterFeed |> ignore

                    return! loop()
                }
                loop()
            )
            [
                SpawnOption.SupervisorStrategy (Strategy.OneForOne (fun error -> Directive.Restart))
            ]

    let notifier log system =
        let log = logWith log "ActorName" "Notifier"
        spawn system "notifier"
            (fun mailbox ->

                let notifier = WindowsNotifications.NotificationManager("TwitterAlerts")

                let rec loop() = actor {
                    let! (message:NewTweet) = mailbox.Receive()

                    let (TwitterFeed feedName) = message.Feed
                    logInfo log "Notifying of new tweet"
                    notifier.Toast(sprintf "New Tweet: %s" feedName, message.Text, message.User)

                    return! loop()
                }
                loop())

    [<EntryPoint>]
    let main argv =

        let log = 
            LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger()

        use system = System.create "my-system" (Configuration.load())

        let notifier = notifier log system
        let watcher = watchers log system notifier
        let console = console log system watcher

        logInfo log "Ready!"

        //watcher <! WatchFeed(TwitterFeed "FSharp")

        system.WhenTerminated
            |> Async.AwaitTask
            |> Async.RunSynchronously

        0 // return an integer exit code
