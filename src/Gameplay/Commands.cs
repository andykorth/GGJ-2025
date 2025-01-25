
using Microsoft.AspNetCore.SignalR;

public class Commands
{
    
    [GameCommand("Chat with all other users.")]
    public static void Say(Player p, GameHub game, string args)
    {
        game.SendAll($"{p.name}: " + args);
    }
    
    [GameCommand("Share an image with all users.")]
    public static void ShareImage(Player p, GameHub game, string args)
    {
        if (Uri.TryCreate(args, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            game.SendImage(uriResult.ToString());
        } else {
            game.Send($"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
    }
    [GameCommand("Share an sound with all users.")]
    public static void PlaySound(Player p, GameHub game, string args)
    {
        if (Uri.TryCreate(args, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            game.SendSound(uriResult.ToString());
        } else {
            game.Send($"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
    }

    [GameCommand("See this help message.")]
    public static void Help(Player p, GameHub game, string args)
    {
        string msg = "These are the commands you can type:\n";
        foreach(var method in InvokeCommand.Commands.Keys){
            msg += $"    [salmon]{method}[/salmon] - {InvokeCommand.HelpTexts[method + ""]} \n";
        }

        game.Send(msg);

    }

    [GameCommand("View some general stats about the world.")]
    public static void WorldStatus(Player p, GameHub game, string args)
    {
        string output = "";

        output += $"========== World Status ==========\n";
        output += " World created: " + World.instance.worldCreationDate.ToString() + "\n";
        output += " Current Tick: " + World.instance.ticks + "\n";
        output += " Players Joined: " + World.instance.allPlayers.Count() + "\n";
        output += " Planets Discovered: " + World.instance.allSites.Count() + "\n";
        output += " Scheduled Tasks: " + World.instance.allScheduledActions.Count() + "\n";

        game.Send(output);
    }


    [GameCommand("View your empire's status.")]
    public static void Status(Player p, GameHub game, string args)
    {
        p.tutorialStep = Math.Min(1, p.tutorialStep);
        string output = "";

        output += $"=========== Empire Status ===========\n";
        
        output += $"   User: {p.name.PadRight(15)} | Cash { (p.cash+"").PadRight(10) }\n";
        output += $"   Completed Research: {0} \n";
        output += ShowShips(4, p, 0);

        output += ShowSites(4, p, 0);
        output += $"===== Unresearched Relics [{p.relicIDs.Count}] =====\n";
        game.Send(output);
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
    public static void Ship(Player p, GameHub game, string args)
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

        game.Send(s);
    }

    private static void ShipExplore(Player p, GameHub game, string args)
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

                game.Send(output);
                game.SetCaptivePrompt(p, "Do you want to start this mission? (y/n)",
                (string response) => {
                    string r = response.ToLower();
                    if(r == "y" || r == "n"){
                        if(r == "y")
                            StartExploration(ticks, p, s);
                        else
                            game.Send("Exploration canceled.");
                        return true;
                    }else{
                        return false;
                    }
                });
            }else{
                output += $" Ship is already on a mission!";
                game.Send(output);
            }
            return;

        }
        game.Send($"Invalid ship exploration args [{args}]");

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

    private static void ShipRename(Player p, GameHub game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            Ship ship = p.ships[index];
            ship.name = args;
            game.Send($"Ship {index} renamed to {args}");
        }else{
            game.Send($"Bad index for rename [{indexS}]");
        }

    }    
    private static void ShipView(Player p, GameHub game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            Ship ship = p.ships[index];

            game.Send(ship.ShortLine());
            game.Send(ship.LongLine());
        }else{
            game.Send($"Bad index for ship viewing [{indexS}]");
        }
    }

    [GameCommand("View and interact with your planetary sites.")]
    public static void Site(Player p, GameHub game, string args)
    {
        if(CheckArg("view", ref args)){
            SiteView(p, game, args);
            return;
        }
        if(CheckArg("invite", ref args)){
            ShipExplore(p, game, args);
            return;
        }
        if(CheckArg("construct", ref args)){
            ShipRename(p, game, args);
            return;
        }

        int start = 0;
        int.TryParse(args, out start);
        string s= ShowSites(20, p, start);
        s += $"===== Site Commands =====\n";
        s += " [salmon]site view 0[/salmon] - View details of your first planetary site.\n";
        s += " [salmon]site invite 0[/salmon] - Invite another player to join you on your site.\n";
        s += " [salmon]site construct 0[/salmon] - View construction options for this site\n";

        game.Send(s);
    }

    private static void SiteView(Player p, GameHub game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            ExploredSite site = p.GetExploredSites()[index];

            game.Send(site.ShortLine());
            game.Send(site.LongLine());

        }else{
            game.Send($"Bad index for site viewing [{indexS}]");
        }

    }

}
