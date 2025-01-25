
using Microsoft.AspNetCore.SignalR;

public class Commands
{
    
    [GameCommand("Chat with all other users.")]
    public static void Say(Player p, GameUpdateService game, string args)
    {
        game.SendAll($"{p.name}: " + args);
    }
    
    [GameCommand("Share an image with all users.")]
    public static void ShareImage(Player p, GameUpdateService game, string args)
    {
        if (Uri.TryCreate(args, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            game.SendImage(uriResult.ToString());
        } else {
            game.Send(p, $"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
    }
    [GameCommand("Share an sound with all users.")]
    public static void PlaySound(Player p, GameUpdateService game, string args)
    {
        if (Uri.TryCreate(args, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            game.SendSound(uriResult.ToString());
        } else {
            game.Send(p, $"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
    }

    [GameCommand("See this help message.")]
    public static void Help(Player p, GameUpdateService game, string args)
    {
        string msg = "These are the commands you can type:\n";
        foreach(var method in InvokeCommand.Commands.Keys){
            msg += $"    [salmon]{method}[/salmon] - {InvokeCommand.HelpTexts[method + ""]} \n";
        }

        game.Send(p, msg);

    }

    [GameCommand("View some general stats about the world.")]
    public static void WorldStatus(Player p, GameUpdateService game, string args)
    {
        string output = "";

        output += $"========== World Status ==========\n";
        output += " World created: " + World.instance.worldCreationDate.ToString() + "\n";
        output += " Current Tick: " + World.instance.ticks + "\n";
        output += " Players Joined: " + World.instance.allPlayers.Count() + "\n";
        output += " Planets Discovered: " + World.instance.allSites.Count() + "\n";
        output += " Scheduled Tasks: " + World.instance.allScheduledActions.Count() + "\n";

        game.Send(p, output);
    }


    [GameCommand("View your empire's status.")]
    public static void Status(Player p, GameUpdateService game, string args)
    {
        p.tutorialStep = Math.Min(1, p.tutorialStep);
        string output = "";

        output += $"=========== Empire Status ===========\n";
        
        output += $"   User: {p.name.PadRight(15)} | Cash { (p.cash+"").PadRight(10) }\n";
        output += $"   Completed Research: {0} \n";
        output += ShowShips(4, p, 0);

        output += ShowSites(4, p, 0);
        output += $"===== Unresearched Relics [{p.relicIDs.Count}] =====\n";
        game.Send(p, output);
    }

    private static string ShowShips(int showMax, Player p, int start)
    {
        string output = $"===== Ships [{p.ships.Count}] =====\n";
        int count = 0;
        foreach (var ship in p.ships){
            if(count < start) {
                count += 1;
                continue;
            }
            if(count > showMax){
                output += $"     ... {count-showMax} more ships. (type [magenta]ships {count}[/magenta] to start at that item)";
                break;
            }
            output += ship.ShortLine(count);
            count += 1;
        }
        return output;
    }

    private static string ShowSites(int showMax, Player p, int start)
    {
        string output = $"===== Discovered Sites [{p.exploredSiteUUIDs.Count}] =====\n";
        int count = 0;
        
        foreach (var site in p.GetExploredSites()){
            if(count < start) {
                count += 1;
                continue;
            }
            if(count > showMax){
                output += $"     ... {count-showMax} more sites. (type [magenta]sites {count}[/magenta] to start at that item)";
                break;
            }
            output += site.ShortLine(count);
            count += 1;
        }
        return output;
    }

    private static bool CheckArg(string check, ref string args){
        if(args.StartsWith(check + " ")){
            args = args.Split(check, 2, StringSplitOptions.TrimEntries)[1];
            return true;
        }
        return false;
    }

    private static string PullArg(ref string args){
        var split = args.Split(" ", 2, StringSplitOptions.TrimEntries);
        if(split.Length > 1){
            args = split[1];
        }else{
            args = "";
        }
        return split[0];
    }

    [GameCommand("View and interact with your empire's ships.")]
    public static void Ship(Player p, GameUpdateService game, string args)
    {
        if(CheckArg("explore", ref args)){
            ShipExplore(p, game, args);
            return;
        }
        if(CheckArg("rename", ref args)){
            ShipRename(p, game, args);
            return;
        }
        if(CheckArg("view", ref args)){
            ShipView(p, game, args);
            return;
        }
        
        int start = 0;
        int.TryParse(args, out start);
        string s= ShowShips(20, p, start);
        s += $"===== Ship Commands =====\n";
        s += " [salmon]ship view 0[/salmon] - View details of your first ship.\n";
        s += " [salmon]ship explore 0[/salmon] - Send your first ship to explore.\n";
        s += " [salmon]ship rename 0 Big Bertha[/salmon] - Rename your first ship to 'Big Bertha'\n";

        game.Send(p, s);
    }

    [GameCommand("List who has recently been online")]
    public static void Who(Player p, GameUpdateService game, string args)
    {
        var sortedPlayers = World.instance.allPlayers.OrderByDescending(p => p.lastActivity);

        int start = 0;
        int.TryParse(args, out start);

        var now = DateTime.Now;
        string s = $"===== Players ({sortedPlayers.Count()}) =====\n";

        // Display each player's name and how recently they were active
        int count = 0;
        int showMax = 20;
        foreach (var player in sortedPlayers)
        {
            if(count < start) {
                count += 1;
                continue;
            }
            if(count > showMax){
                s += $"     ... {count-showMax} more players. (type [magenta]who {count}[/magenta] to start at that item)";
                break;
            }

            TimeSpan duration = now - player.lastActivity;
            string timeAgo;

            if (duration.TotalMinutes < 60) {
                timeAgo = $"{(int)duration.TotalMinutes} min ago";
            } else if (duration.TotalHours < 24) {
                timeAgo = $"{(int)duration.TotalHours} hr ago";
            } else {
                timeAgo = $"{(int)duration.TotalDays} days ago";
            }
            count += 1;
            s += ($"{player.name} - {timeAgo}\n");
        }

        game.Send(p, s);
    }


    [GameCommand("Check your messages and invitations from other players")]
    public static void Message(Player p, GameUpdateService game, string args)
    {
        // Sort messages: most recent sent date first, unread messages first
        var sortedMessages = p.messages
            .OrderByDescending(m => m.sent)
            .ThenBy(m => m.read.HasValue);

        int.TryParse(args, out int start);

        string s = $"===== Messages ({sortedMessages.Count()}) =====\n";

        // Display each player's name and how recently they were active
        int count = 0;
        int showMax = 20;
        DateTime now = DateTime.Now;
        foreach (var message in sortedMessages)
        {
            if(count < start) {
                count += 1;
                continue;
            }
            if(count > showMax){
                s += $"     ... {count-showMax} more messages. (type [magenta]message {count}[/magenta] to start at that item)";
                break;
            }
            // Determine if the message is read or unread
            string status = message.read.HasValue ? "<read>" : "[cyan]<unread>[/cyan]";
            string from = World.instance.GetPlayer(message.fromPlayerUUID)!.name;
            // Display the message details
            s += $"[{Ascii.TimeAgo(now - message.sent)}] {status} - {message.type} from {from}: {Ascii.Shorten(message.contents, 20)}";
        }

        game.Send(p, s);
    }

    private static void ShipExplore(Player p, GameUpdateService game, string args)
    {
        int index = 0;
        if(int.TryParse(args, out index)){
            string output = "Begin exploration mission with:\n";
            Ship s = p.ships[index];
            output += s.ShortLine(0);
            output += s.LongLine();

            if(s.shipMission == global::Ship.ShipMission.Idle){

                int ticks = 600 / World.instance.timescale;
                output += $" Exploration mission will take {ticks} seconds.";

                game.Send(p, output);
                game.SetCaptiveYNPrompt(p, "Do you want to start this mission? (y/n)", (bool response) => {
                    if(response)
                        StartExploration(ticks, p, s);
                    else
                        game.Send(p, "Exploration canceled.");
                });
            }else{
                output += $" Ship is already on a mission!";
                game.Send(p, output);
            }
            return;

        }
        game.Send(p, $"Invalid ship exploration args [{args}]");

    }

    private static void StartExploration(int duration, Player p, Ship s)
    {
        s.shipMission = global::Ship.ShipMission.Exploring;
        s.arrivalTime = duration + World.instance.ticks;
        ScheduledTask st = new ScheduledTask(duration, p, s, ScheduledAction.ExplorationMission);
        World.instance.Schedule(st);

        string output = $"{s.GetName()} departs for the stars.\n";
        output += $"Hopefully their efforts will be fruitful.\n";
        output += $"We expect to hear from them in {duration}s.\n";

        p.Send(Ascii.Box(output));
    

    }

    private static void ShipRename(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            Ship ship = p.ships[index];
            ship.name = args;
            game.Send(p, $"Ship {index} renamed to {args}");
        }else{
            game.Send(p, $"Bad index for rename [{indexS}]");
        }

    }    
    private static void ShipView(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            Ship ship = p.ships[index];

            game.Send(p, ship.ShortLine());
            game.Send(p, ship.LongLine());
        }else{
            game.Send(p, $"Bad index for ship viewing [{indexS}]");
        }
    }

    [GameCommand("View and interact with your planetary sites.")]
    public static void Site(Player p, GameUpdateService game, string args)
    {
        if(CheckArg("view", ref args)){
            SiteView(p, game, args);
            return;
        }
        if(CheckArg("invite", ref args)){
            SiteInvite(p, game, args);
            return;
        }
        if(CheckArg("construct", ref args)){
            SiteConstruct(p, game, args);
            return;
        }

        int start = 0;
        int.TryParse(args, out start);
        string s= ShowSites(20, p, start);
        s += $"===== Site Commands =====\n";
        s += " [salmon]site view 0[/salmon] - View details of your first planetary site.\n";
        s += " [salmon]site invite 0[/salmon] - Invite another player to join you on your site.\n";
        s += " [salmon]site construct 0[/salmon] - View construction options for this site\n";

        game.Send(p, s);
    }

    private static void SiteView(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            ExploredSite site = p.GetExploredSites()[index];

            game.Send(p, site.ShortLine());
            game.Send(p, site.LongLine());

        }else{
            game.Send(p, $"Bad index for site viewing [{indexS}]");
        }
    }

    
    
    private static void SiteInvite(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            ExploredSite site = p.GetExploredSites()[index];

            string s = "";
            s+= site.ShortLine();
            s+= site.LongLine();
            s+= "By inviting someone this planet, they will be able to build there too.\n It doesn't use up any of your space.\n";

            game.Send(p, s);


            game.SetCaptivePrompt(p, "Who do you want to invite? (or [red]cancel[/red] or [red]who[/red])",
                (string response) => {
                    string r = response.ToLower();
                    // try to find a player name
                    Player? found = World.instance.GetPlayerByName(r);
                    if(r == "cancel" || r == "who" || found != null){
                        if(found != null) 
                            Invite(p, game, found, site);
                        if(r == "who")
                            Who(p, game, args);
                        if(r == "cancel")
                            game.Send(p, "Invite canceled.");
                        return true;
                    }else{
                        return false;
                    }
                });

        }else{
            game.Send(p, $"Bad index for site viewing [{indexS}]");
        }
    }

    private static void Invite(Player p, GameUpdateService game, Player found, ExploredSite site)
    {
        game.Send(p, $"You are inviting {found.name} to {site.name}.");
        game.SetCaptivePrompt(p, "If you want to include a message, type it now.",
            (string response) => {
                Message m = new Message(p, global::Message.MessageType.Invitation, response);
                m.invitationSiteUUID = site.uuid;
                found.messages.Add(m);

                game.SendTo(found.connectionID, $"You have a new message from {p.name}!" );

                return true;
            });

    }


    private static void SiteConstruct(Player p, GameUpdateService game, string args)
    {
        
            game.Send(p, $"ahhhhh");
        
    }
}
