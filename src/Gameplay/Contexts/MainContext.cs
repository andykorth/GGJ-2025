
public class MainContext : Context {

    public override string Name => "MainMenu";
    public override bool rootContext => true;

    
    public override void EnterContext(Player p, GameUpdateService service)
    {
        p.Send("[yellow]Main Menu:[/yellow]");
    }

    [GameCommand("Menu: View and interact with your empire's ships.")]
    public static void Ship(Player p, GameUpdateService game, string args)
    {
        p.SetContext<ShipContext>();
    }

    [GameCommand("Menu: Check your messages and invitations from other players")]
    public static void Message(Player p, GameUpdateService game, string args)
    {
        p.SetContext<MessageContext>();
    }

    [GameCommand("Menu: Check the production of your buildings")]
    public static void Prod(Player p, GameUpdateService game, string args)
    {
        p.SetContext<ProdContext>();
    }

    [GameCommand("Menu: See the planets you have explored.")]
    public static void Site(Player p, GameUpdateService game, string args)
    {
        p.SetContext<SiteContext>();
    }

    [GameCommand("Inv: See your inventory, sell items on the exchange.")]
    public static void Inv(Player p, GameUpdateService game, string args)
    {
        p.SetContext<InventoryContext>();
    }

    [GameCommand("Exchange: View and Buy materials on the exchange.")]
    public static void Exchange(Player p, GameUpdateService game, string args)
    {
        p.SetContext<ExchangeContext>();
    }

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
            game.SendAll($"{p.name} sends an image:");
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
            game.SendAll($"{p.name} sends a sound.");
            game.SendSound(uriResult.ToString());
        } else {
            game.Send(p, $"Hey {p.name}, the provided input is not a valid URL: {args}");
        }
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
        output += " Materials: " + World.instance.allMats.Count() + "\n";

        game.Send(p, output);
    }


    [GameCommand("View your empire's status.")]
    public static void Status(Player p, GameUpdateService game, string args)
    {
        p.tutorialStep = Math.Min(1, p.tutorialStep);
        string output = "";

        output += Ascii.Header("Empire Status", 40, "yellow");
        
        output += $"   User: {p.name.PadRight(15)} | Cash: { (p.cash+"").PadRight(10) }\n";
        output += $"   Completed Research: {0}  | Unread Messages: {p.UnreadMessages()} \n";
        output += ShowShips(4, p, 0);

        output += ShowSites(4, p, 0);
        output += ShowRelics(4, p, 0);
        game.Send(p, output);
    }

    [GameCommand("List who has recently been online")]
    public static void Who(Player p, GameUpdateService game, string args)
    {
        var sortedPlayers = World.instance.allPlayers.OrderByDescending(p => p.lastActivity);

        int start = PullIntArg(p, ref args);

        string s = ShowList(sortedPlayers.Cast<IShortLine>().ToList(), "Players", "ship", 20, p, start);
        game.Send(p, s);
    }


}
