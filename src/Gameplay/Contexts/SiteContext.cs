
public class SiteContext : Context {

    public override string Name => "Site";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        p.Send("[yellow]Site Menu:[/yellow]");
        string s= MainContext.ShowSites(20, p, 0);
        game.Send(p, s);
    }

    [GameCommand("list - list the sites again.")]
    public static void List(Player p, GameUpdateService game, string args)
    {
        int start = PullIntArg(p, ref args, true);
        string s= MainContext.ShowSites(20, p, start);
        game.Send(p, s);
    }

    [GameCommand("View 0 - View a planet's details.")]
    public static void View(Player p, GameUpdateService game, string args)
    {
        ExploredSite? site = PullIndexArg<ExploredSite>(p, game, ref args, p.GetExploredSites() );
        if(site != null){
            game.Send(p, Ascii.Header(site.name, 60, site.SiteColor()));
            game.Send(p, ((IShortLine)site).ShortLine(p, -1));
            game.Send(p, site.LongLine(p));
        }
    }

    [GameCommand("invite 0 - Invite another player to join you on your site.")]
    public static void Invite(Player p, GameUpdateService game, string args)
    {
        ExploredSite? site = PullIndexArg(p, game, ref args, p.GetExploredSites());
        if(site != null){
            string s = "";
            s+= ((IShortLine)site).ShortLine(p, -1);
            s+= "By inviting someone this planet, they will be able to build there too.\n It doesn't use up any of your space.\n";

            game.Send(p, s);

            p.SetCaptiveSelectPlayerPrompt( "Who do you want to invite? (or [salmon]cancel[/salmon] or [salmon]who[/salmon])", (Player found) => {
                Invite(p, game, found, site);
            });
        }
    }

    private static void Invite(Player p, GameUpdateService game, Player recipient, ExploredSite site)
    {
        if(p == null){
            Log.Error("Missing player sending invite.");
            return;
        }
        if(recipient == null){
            Log.Error("Missing invite recipient.");
            return;
        } 
        if(site == null){
            Log.Error("Missing invite site.");
            return;
        } 

        game.Send(p, $"You are inviting {recipient.name} to {site.name}.");
        p.SetCaptivePrompt("If you want to include a message, type it now.", InvokeCommand.GetContext<SiteContext>(),
            (string response) => {
                Message m = new Message(p, global::Message.MessageType.Invitation, response);
                m.invitationSiteUUID = site.uuid;
                recipient.messages.Add(m);

                game.Send(recipient, $"You have a new message from {p.name}!" );
                game.Send(p, $"Invite sent to {recipient.name}.");
                return true;
            });

    }

    [GameCommand("construct 0: View building construction options.")]
    public static void Construct(Player p, GameUpdateService game, string args)
    {
        if( p.buildings.Count() >= p.GetMaxBuildings() ){
            game.Send(p, $"[red]Your headquarters only allows for {p.GetMaxBuildings()} buildings. Upgrade your Planetary Administration Office.[/red]");
            return;
        }

        int index;
        string indexS = PullArg(ref args);
        if(int.TryParse(indexS, out index)){
            ExploredSite site = p.GetExploredSites()[index];
            int totalSlots = site.GetBuildingSlots();
            int usedSlots = site.GetBuildingsOnSite(p).Count();

            string s = "";
            s+= ((IShortLine)site).ShortLine(p, -1);
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
                p.SetCaptivePrompt($"Which do you want to build: [salmon]0-{choiceCount-1}[/salmon] (or [salmon]cancel[/salmon])", InvokeCommand.GetContext<SiteContext>(),
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


    [GameCommand("develop 0: Start a development project to increase the population for the entire planet")]
    public static void Develop(Player p, GameUpdateService game, string args)
    {
        ExploredSite? site = PullIndexArg(p, game, ref args, p.GetExploredSites());
        if(site != null){
            int population = site.pendingPopulation;
            int developmentCost = (int) ((500 + population * 30) * site.DevelopmentPriceFactor());
            int nameIndex = new Random(site.name.GetHashCode() + population).Next(0, Ascii.developmentProjects.Length);
            string projectName = Ascii.developmentProjects[nameIndex];

            string s = "";
            s+= ((IShortLine)site).ShortLine(p, -1);
            // s+= site.LongLine();
            s+= $"{p.name}, current cash: {p.cash}\n";
            s+= $"A development project will add 10k citizens, and cost {developmentCost}.\n";
            s+= $"Project: [cyan]{projectName}[/cyan]\n";

            if(p.cash >= developmentCost){

                game.Send(p, s);
                 p.SetCaptiveYNPrompt("Do you want to start this development project?", (bool response) => {
                    if(response)
                        StartDevelopment(p, game, site, developmentCost, projectName);
                    else
                        game.Send(p, "No project started.\n");
                });
            }else{
                s+= $"You can't afford to fund this right now!\n";
                game.Send(p, s);
            }
        }
    }

    private static void StartDevelopment(Player p, GameUpdateService game, ExploredSite site, int developmentCost, string projectName)
    {
        p.cash -= developmentCost;
        int duration = developmentCost / World.instance.timescale;

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
    
    private static string GetTypeDesc(BuildingType type)
    {
        return type switch
        {
            BuildingType.Retail => "Generates revenue by selling goods.",
            BuildingType.Mine => "Extracts raw materials from the earth.",
            BuildingType.Factory => "Produces finished goods from raw materials.",
            BuildingType.Farm => "Grows crops on lush planets or harvests regolith and lichen in a vacuum.",
            _ => "Unknown building type."
        };
    }

    private static int GetCost(ExploredSite site, BuildingType type)
    {
        int cost = 100;
        if (type == BuildingType.Retail)
        {
            cost = (int)(site.DevelopmentPriceFactor() * 50 + 100);
        }
        if (type == BuildingType.Mine)
        {
            cost = (int)((site.planetClass + 0.4) * 150 + 50);
        }
        if (type == BuildingType.Factory)
        {
            cost = (int)(site.DevelopmentPriceFactor() * 200 + 50);
        }
        if (type == BuildingType.Factory)
        {
            cost = (int)(site.planetClass * 30 + 50);
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

        int duration = cost / World.instance.timescale;

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
                case BuildingType.Farm:
                    output += site.GoldilocksClass()
                        ? $"Settlers on {site.name} sow the first fields, hopeful for a thriving harvest in this fertile land.\n"
                        : site.StandardClass()
                            ? $"Greenhouses rise on {site.name}, shielding crops from the biting cold.\n"
                            : $"Out in the cold vacuum, experimental crops grow and agricultural operation begin on {site.name}.\n";
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
                        ? $"The hum of machinery fills the air on {site.name} as construction machines build a factory.\n"
                        : site.StandardClass()
                            ? $"Industrial production sparks life into the cold plains of {site.name}.\n"
                            : $"Sealed within domes, the factory construction on {site.name} churns in defiance of the lifeless expanse outside.\n";
                    break;
            }
        }
        else
        {
            switch (type)
            {
                case BuildingType.Farm:
                    output += site.GoldilocksClass()
                        ? $"The fields of {site.name} sway with golden crops, feeding a growing community.\n"
                        : site.StandardClass()
                            ? $"Under artificial lights, the first harvest is celebrated by the settlers of {site.name}.\n"
                            : $"Out in the barren vacuum, the agricultural machines of {site.name} begin crossing the landscape.\n";
                    break;
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

        p.Send(Ascii.Box(output, "yellow"));

        ScheduledTask st = new ScheduledTask(duration, p, site, ScheduledAction.SiteConstruction);
        st.buildingType = type;
        World.instance.Schedule(st);
    }

}
