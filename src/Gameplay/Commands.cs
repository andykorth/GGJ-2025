
using Microsoft.AspNetCore.SignalR;

public class Commands
{
    
    [GameCommand("Chat with all other users.")]
    public static void Say(Player p, GameUpdateService game, string args)
    {
        game.SendAll($"{p.name}: " + args);
    }
    
    [GameCommand("Share an image with all users.", true)]
    public static void ShareImage(Player p, GameUpdateService game, string args)
    {
        if (Uri.TryCreate(args, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            game.SendImage(uriResult.ToString());
        } else {
            game.Send(p, $"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
    }
    [GameCommand("Share an sound with all users.", true)]
    public static void PlaySound(Player p, GameUpdateService game, string args)
    {
        if (Uri.TryCreate(args, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) {
            game.SendSound(uriResult.ToString());
        } else {
            game.Send(p, $"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
    }

    [GameCommand("See this help message.", true)]
    public static void Help(Player p, GameUpdateService game, string args)
    {
        bool showAll = args.Contains("all");

        string msg = "These are the commands you can type: (To see them all, try [red]help all[/red])\n";
        foreach(var method in InvokeCommand.Commands.Keys){
            var a = InvokeCommand.HelpAttrs[method + ""];
            if(showAll || !a.normallyHidden)
                msg += $"    [salmon]{method}[/salmon] - {a.helpText} \n";
        }

        game.Send(p, msg);

    }

    [GameCommand("View some general stats about the world.")]
    public static void WorldStatus(Player p, GameUpdateService game, string args)
    {
        string output = "";

        output += $"========== World Status ==========\n";
        output += " World created: " + World.instance.worldCreationDate.ToString() + "\n";
        output += $" Current Tick: {World.instance.ticks}        Speed: {World.instance.timescale}\n";
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
        
        output += $"   User: {p.name.PadRight(15)} | Cash: { (p.cash+"").PadRight(10) }\n";
        output += $"   Completed Research: {0}  | Unread Messages: {p.UnreadMessages()} \n";
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
            output += site.ShortLine(p, count);
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
        if(CheckArg("repair", ref args)){
            ShipRepair(p, game, args);
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
            s += ($"{player.name} - {Ascii.TimeAgo(duration)}\n");
            count += 1;
        }

        game.Send(p, s);
    }


    [GameCommand("Check your messages and invitations from other players")]
    public static void Message(Player p, GameUpdateService game, string args)
    {
        if(CheckArg("view", ref args)){
            MessageView(p, game, args);
            return;
        }
        if(CheckArg("send", ref args)){
            MessageSend(p, game, args);
            return;
        }

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
                s += $"===== Message Commands =====\n";
        s += " [salmon]message view 0[/salmon] - View message or respond to invitation.\n";
        s += " [salmon]message send[/salmon] - Start sending a text message to another player.\n";

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

                int ticks = 200 / World.instance.timescale;
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

    private static void ShipRepair(Player p, GameUpdateService game, string args)
    {
        throw new NotImplementedException();
    }

    private static void MessageView(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            Message message = p.messages[index];
            message.read = DateTime.Now;

            string from = World.instance.GetPlayer(message.fromPlayerUUID)!.name;
            // Display the message details
            string s =  "";
            DateTime now = DateTime.Now;
            s += $"[{Ascii.TimeAgo(now - message.sent)}] - {message.type} from {from}\n   {Ascii.Box(message.contents)}\n";
            if(message.type == global::Message.MessageType.Invitation){
                var site = World.instance.GetSite(message.invitationSiteUUID!);
                s += $"[cyan]The message contains an Site Invitation to join {from} on {site!.name}:[/cyan]\n \n";
                s += site.ShortLine(p);
                // s += site.LongLine();
                if(p.GetExploredSites().Contains(site)){
                    s += $"\n[red]But you are already on that planet![/red]\n";
                    game.Send(p, s);
                }else{
                    game.Send(p, s);
                    game.SetCaptiveYNPrompt(p, $"Do you want to join {from} on {site!.name}? (y/n)", (bool response) => {
                    if(response){
                        p.exploredSiteUUIDs.Add(site.uuid);
                        game.Send(p, $"{site!.name} added to your site list!");
                    } else
                        game.Send(p, "You can reconsider later!");
                });
                }
            }

        }else{
            game.Send(p, $"Bad index for message viewing [{indexS}]");
        }
    }

    private static void MessageSend(Player p, GameUpdateService game, string args)
    {
        game.SetCaptivePrompt(p, "Who do you want to message? (or [red]cancel[/red] or [red]who[/red])",
            (string response) => {
                string r = response.ToLower();
                // try to find a player name
                Player? found = World.instance.GetPlayerByName(r);
                if(r == "cancel" || r == "who" || found != null){
                    if(found != null){
                        game.Send(p, $"Message to {found.name}...");
                        MessageSendReally(p, game, found);
                        return false;
                    }
                    if(r == "who"){
                        Who(p, game, args);
                        return false; // keep them in the captive prompt
                    }
                    if(r == "cancel")
                        game.Send(p, "Invite canceled.");
                    return true;
                }else{
                    return false;
                }
            });
    }

    private static void MessageSendReally(Player p, GameUpdateService game, Player recipient)
    {
        if(p == null) Log.Error("Missing player sending message.");
        if(recipient == null) Log.Error("Missing message recipient.");

        game.Send(p, $"You are messaging {recipient.name}.");
        game.SetCaptivePrompt(p, "Enter your message text now.",
            (string response) => {
                Message m = new Message(p, global::Message.MessageType.TextMail, response);
                recipient.messages.Add(m);

                game.Send(recipient, $"You have a new message from {p.name}!" );
                game.Send(p, $"Message sent to {recipient.name}.");
                return true;
            });

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
        if(CheckArg("develop", ref args)){
            SiteDevelop(p, game, args);
            return;
        }

        int start = 0;
        int.TryParse(args, out start);
        string s= ShowSites(20, p, start);
        s += $"===== Site Commands =====\n";
        s += " [salmon]site view 0[/salmon] - View details of your first planetary site.\n";
        s += " [salmon]site invite 0[/salmon] - Invite another player to join you on your site.\n";
        s += " [salmon]site develop 0[/salmon] - Start a development project to increase the population for the entire planet\n";
        s += " [salmon]site construct 0[/salmon] - View building construction options\n";

        game.Send(p, s);
    }

    private static void SiteView(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            ExploredSite site = p.GetExploredSites()[index];

            game.Send(p, site.ShortLine(p));
            game.Send(p, site.LongLine(p));

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
            s+= site.ShortLine(p);
            // s+= site.LongLine();
            s+= "By inviting someone this planet, they will be able to build there too.\n It doesn't use up any of your space.\n";

            game.Send(p, s);


            game.SetCaptivePrompt(p, "Who do you want to invite? (or [red]cancel[/red] or [red]who[/red])",
                (string response) => {
                    string r = response.ToLower();
                    // try to find a player name
                    Player? found = World.instance.GetPlayerByName(r);
                    if(r == "cancel" || r == "who" || found != null){
                        if(found != null){
                            game.Send(p, $"Invite to {found.name}...");
                            Invite(p, game, found, site);
                            return false;
                        }
                        if(r == "who"){
                            Who(p, game, args);
                            return false; // keep them in the captive prompt
                        }
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

    private static void Invite(Player p, GameUpdateService game, Player recipient, ExploredSite site)
    {
        if(p == null) Log.Error("Missing player sending invite.");
        if(recipient == null) Log.Error("Missing invite recipient.");
        if(site == null) Log.Error("Missing invite site.");

        game.Send(p, $"You are inviting {recipient.name} to {site.name}.");
        game.SetCaptivePrompt(p, "If you want to include a message, type it now.",
            (string response) => {
                Message m = new Message(p, global::Message.MessageType.Invitation, response);
                m.invitationSiteUUID = site.uuid;
                recipient.messages.Add(m);

                game.Send(recipient, $"You have a new message from {p.name}!" );
                game.Send(p, $"Invite sent to {recipient.name}.");
                return true;
            });

    }

    private static void SiteDevelop(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            ExploredSite site = p.GetExploredSites()[index];
            int population = site.pendingPopulation;
            int developmentCost = (int) ((500 + population * 30) * site.DevelopmentPriceFactor());
            int nameIndex = (new Random(index + population)).Next(0, Ascii.developmentProjects.Length);
            string projectName = Ascii.developmentProjects[nameIndex];

            string s = "";
            s+= site.ShortLine(p);
            // s+= site.LongLine();
            s+= $"{p.name}, current cash: {p.cash}\n";
            s+= $"A development project will add 10k citizens, and cost {developmentCost}.\n";
            s+= $"Project: [cyan]{projectName}[/cyan]\n";

            if(p.cash >= developmentCost){

                game.Send(p, s);
                game.SetCaptiveYNPrompt(p, "Do you want to start this development project? (y/n)", (bool response) => {
                    if(response)
                        StartDevelopment(p, game, site, developmentCost, projectName);
                    else
                        game.Send(p, "No project started.\n");
                });
            }else{
                s+= $"You can't afford to fund this right now!\n";
                game.Send(p, s);
            }

        }else{
            game.Send(p, $"Bad index for site viewing [{indexS}]");
        }
    }

    private static void StartDevelopment(Player p, GameUpdateService game, ExploredSite site, int developmentCost, string projectName)
    {
        p.cash -= developmentCost;
        int duration = developmentCost / 4 / World.instance.timescale;

        ScheduledTask st = new ScheduledTask(duration, p, site, ScheduledAction.SiteDevelopmentMission);
        st.string1 = projectName;
        World.instance.Schedule(st);

        string output = "";
        if(site.population <= 0){
            if(site.GoldilocksClass()){
                output += $"A river flows undisturbed on the surface of {site.name}.\n";
            }else if(site.StandardClass()){
                output += $"A cold wind blows on the surface of {site.name}.\n";
            }else{
                output += $"The harsh sun rises over the airless surface of {site.name}.\n";
            }
        }else{
            if(site.GoldilocksClass()){
                output += $"The people of {site.name} take to the streets and rejoice.\n";
            }else if(site.StandardClass()){
                output += $"The people of {site.name} rejoice in their homes.\n";
            }else{
                output += $"The people of {site.name} huddle in their bio-domes and hope for their future.\n";
            }
        }

        output += $"Good things are coming to the planet thanks to {p.name}.\n";
        output += $"Development has begun on [cyan]{projectName}[/cyan].\n";
        output += $"The population will grow when the project is complete in {duration} s.\n";
        
        site.pendingPopulation += 10;

        p.Send(Ascii.Box(output));
    
    }

    private static void SiteConstruct(Player p, GameUpdateService game, string args)
    {
        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            ExploredSite site = p.GetExploredSites()[index];
            int totalSlots = site.GetBuildingSlots();
            int usedSlots = site.GetBuildings(p).Count();

            string s = "";
            s+= site.ShortLine(p);
            s+= site.LongLine(p);
            s+= $"Buildings: {usedSlots} / {totalSlots}\n";
            if(usedSlots < totalSlots){
                s+= $"\nConstruction Choices: (you have ${p.cash})\n";
                int choiceCount = Enum.GetValues(typeof(BuildingType)).Length;
                for(int i=0; i < choiceCount; i++){
                    s += GetConstructionLine(i, site, (BuildingType) i);
                    s += "\n";
                }
                game.Send(p, s);
                game.SetCaptivePrompt(p, $"Which do you want to build: [red]0-{choiceCount}[/red] (or [red]cancel[/red])",
                    (string response) => {
                        string r = response.ToLower();
                        int index = -1;
                        if(r == "cancel" || int.TryParse(r, out index) ){
                            if(index >= 0){
                                DoConstruct(p, game, site, (BuildingType) index);
                            }
                            if(r == "cancel")
                                game.Send(p, "Construction canceled.");
                            return true;
                        }else{
                            return false;
                        }
                    });
            }else{
                s+= "[red]Site Full[/red]\n";
                game.Send(p, s);
            }
        }else{
            game.Send(p, $"Bad index for site construction [{indexS}]");
        }   
    }

    private static string GetConstructionLine(int index, ExploredSite site, BuildingType type)
    {
        int cost = GetCost(site, type);

        return $"   {index}) New {type} - ${cost}, {GetTypeDesc(type)}";
    }

    private static string GetTypeDesc(BuildingType type)
    {
        return type switch
        {
            BuildingType.Retail => "Generates revenue by selling goods.",
            BuildingType.Mine => "Extracts raw materials from the earth.",
            BuildingType.Factory => "Produces finished goods from raw materials.",
            _ => "Unknown building type."
        };
    }

    private static int GetCost(ExploredSite site, BuildingType type)
    {
        int cost = 100;
        if (type == BuildingType.Retail)
        {
            cost = (int)site.DevelopmentPriceFactor() * 50 + 100;
        }
        if (type == BuildingType.Mine)
        {
            cost = (int)(site.planetClass + 0.4) * 150 + 50;
        }
        if (type == BuildingType.Factory)
        {
            cost = (int)site.DevelopmentPriceFactor() * 200 + 50;
        }

        return cost;
    }


    private static void DoConstruct(Player p, GameUpdateService game, ExploredSite site, BuildingType type)
    {
        int cost = GetCost(site, type);
        if(p.cash < cost){
            game.Send(p, "You don't have enough cash!");
            return;
        }
        p.cash -= cost;

        int duration = cost / 4 / World.instance.timescale;

        string output = "";
        if (site.population <= 0)
        {
            switch (type)
            {
                case BuildingType.Retail:
                    output += site.GoldilocksClass()
                        ? $"An ambitious venture begins on {site.name}, as the first shops open their doors amidst lush surroundings.\n"
                        : site.StandardClass()
                            ? $"The first trade post on {site.name} is established, braving the chill winds.\n"
                            : $"A lone outpost on the barren surface of {site.name} sparks the beginnings of commerce.\n";
                    break;

                case BuildingType.Mine:
                    output += site.GoldilocksClass()
                        ? $"Drilling begins on {site.name}, uncovering treasures hidden beneath the verdant ground.\n"
                        : site.StandardClass()
                            ? $"Mining rigs pierce the frostbitten crust of {site.name}, eager for precious ores.\n"
                            : $"On the harsh, airless surface of {site.name}, machines dig tirelessly for valuable resources.\n";
                    break;

                case BuildingType.Factory:
                    output += site.GoldilocksClass()
                        ? $"The hum of machinery fills the air on {site.name} as the first factory begins production.\n"
                        : site.StandardClass()
                            ? $"Industrial production sparks life into the cold plains of {site.name}.\n"
                            : $"Sealed within domes, the factory on {site.name} churns in defiance of the lifeless expanse outside.\n";
                    break;
            }
        }
        else
        {
            switch (type)
            {
                case BuildingType.Retail:
                    output += site.GoldilocksClass()
                        ? $"Shoppers flood the bustling markets of {site.name}, eager for new wares.\n"
                        : site.StandardClass()
                            ? $"The citizens of {site.name} enjoy a newfound outlet for trade and commerce.\n"
                            : $"Inside their protective domes, the people of {site.name} experience the joy of trade for the first time.\n";
                    break;

                case BuildingType.Mine:
                    output += site.GoldilocksClass()
                        ? $"Excavators unearth riches on {site.name}, greeted by cheering onlookers.\n"
                        : site.StandardClass()
                            ? $"The citizens of {site.name} watch with awe as the mines yield their first haul.\n"
                            : $"Within their protective shelters, the people of {site.name} hear the rumble of drills tapping into fortune.\n";
                    break;

                case BuildingType.Factory:
                    output += site.GoldilocksClass()
                        ? $"The first assembly lines on {site.name} bring excitement and promise to the population.\n"
                        : site.StandardClass()
                            ? $"In the cool twilight, the people of {site.name} celebrate the hum of progress.\n"
                            : $"Within the safety of domes, the factory on {site.name} signals a step toward self-reliance.\n";
                    break;
            }
        }

        output += $"Thanks to {p.name}'s vision, {type} construction has begun on {site.name}.\n";
        output += $"The project is expected to complete in {duration} s, shaping the planet's future.\n";

        p.Send(Ascii.Box(output));

        ScheduledTask st = new ScheduledTask(duration, p, site, ScheduledAction.SiteConstruction);
        st.buildingType = type;
        World.instance.Schedule(st);
    }

}
