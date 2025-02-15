
public class ProdContext : Context {

    public override string Name => "Prod";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        p.Send("[yellow]Production Menu:[/yellow]");
        string s= ShowProdBuildings(20, p, 0);
        game.Send(p, s);
    }

    private static string ShowProdBuildings(int showMax, Player p, int start)
    {
        var list = p.buildings;
        return Context.ShowList(list.Cast<IShortLine>().ToList(), "Production Buildings", "prod", showMax, p, start, 60);
    }

    [GameCommand("List all buildings again.")]
    public static void List(Player p, GameUpdateService game, string args)
    {
        int start = PullIntArg(p, ref args);
        string s = ShowProdBuildings(20, p, start);
        game.Send(p, s);
    }   

    [GameCommand("View building details, and see what can be produced there.")]
    public static void View(Player p, GameUpdateService game, string args)
    {
        Building? b = PullIndexArg<Building>(p, game, ref args, p.buildings);
        if(b != null){
            string s = "";
            s += b.LongLine(p);
            game.Send(p, s);
        }
    }    

    [GameCommand("Edit a building's production. (start/stop prod, rename, upgrade, remove)")]
    public static void Edit(Player p, GameUpdateService game, string args)
    {
        Building? b = PullIndexArg<Building>(p, game, ref args, p.buildings);
        if(b != null)
        {
            p.ContextData = b;
            p.SetContext<BuildingContext>();
        }else{
            game.Send(p, "Bad arg");
        }
    }
}

public class BuildingContext : Context {

    public override string Name => $"Building";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        Building building = (Building) p.ContextData;
        p.Send($"[yellow]{Name} Menu:[/yellow]");
        string s= building.LongLine(p);

        game.Send(p, s);
    }

    [GameCommand("[salmon]rename Potato Factory[/salmon] - Renames this building to 'Potato Factory'.")]
    public static void Rename(Player p, GameUpdateService game, string args)
    {
        Building b = (Building) p.ContextData;

        string oldName = b.GetName();
        b.name = args;
        game.Send(p, $"Building '{oldName}' renamed to '{args}'.");
    }   

    [GameCommand("Destroy this building (you can reuse the slot).")]
    public static void Destroy(Player p, GameUpdateService game, string args)
    {
        Building b = (Building) p.ContextData;
        p.buildings.Remove(b);
        game.Send(p, $"Building {b.GetName()} destroyed!");
    }   

    [GameCommand("[salmon]start 0[/salmon] - Start production on product 0.")]
    public static void Start(Player p, GameUpdateService game, string args)
    {
        Building b = (Building) p.ContextData;
        int index = PullIntArg(p, ref args);
        if(index >= 0){
            b.StartProd(p, game, b, index);
        }
    }

    [GameCommand("Stop production on current product")]
    public static void Stop(Player p, GameUpdateService game, string args)
    {
        Building b = (Building) p.ContextData;
        b.StopProd(p, game, b);
    }

    [GameCommand("Go back to the Prod menu.")]
    public static void Back(Player p, GameUpdateService game, string args)
    {
        p.SetContext<ProdContext>();
    }
}
