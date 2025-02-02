public class InventoryContext : Context
{
    public override string Name => "Inventory";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        p.Send("[yellow]Inventory Menu:[/yellow]");
        string s = ShowInventory(20, p, 0);
        game.Send(p, s);
    }

    private static string ShowInventory(int showMax, Player p, int start)
    {
        var list = p.items;
        return Context.ShowList(list.Cast<IShortLine>().ToList(), "Inventory", "inv", showMax, p, start);
    }

    [GameCommand("List all inventory items again.")]
    public static void List(Player p, GameUpdateService game, string args)
    {
        int start = PullIntArg(p, ref args);
        string s = ShowInventory(20, p, start);
        game.Send(p, s);
    }

    [GameCommand("View item details.")]
    public static void View(Player p, GameUpdateService game, string args)
    {
        Item? item = PullIndexArg<Item>(p, game, ref args, p.items);
        if (item != null) {
            string s = item.LongLine();
            game.Send(p, s);
        } else {
            game.Send(p, "Invalid item.");
        }
    }

    [GameCommand("[salmon]sell 0 100 30[/salmon] - Post an offer to sell 100 units of item 0 at $30/u")]
    public static void Sell(Player p, GameUpdateService game, string args)
    {
        Item? item = PullIndexArg<Item>(p, game, ref args, p.items);
        int quantity = PullIntArg(p, ref args);
        int price = PullIntArg(p, ref args);

        if (item != null && quantity > 0 && quantity <= item.Amount && price > 0)
        {
            p.RemoveMaterial(item.Material, quantity);
            if(World.instance.PostOffer(p, item.Material, quantity, price)){
                game.Send(p, $"Posted sell offer for {quantity} {item.Material.name} at {price} credits each.");
            }
        }
        else
        {
            game.Send(p, "Invalid sale request.");
        }
    }

}
