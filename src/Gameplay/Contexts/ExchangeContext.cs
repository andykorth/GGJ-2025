public class ExchangeContext : Context
{
    public override string Name => "Exchange";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        string s = "";
        s += Ascii.Header("Commodity Exchange", 50, "yellow");
        s += $"Max orders (offers+requests) is determined by your [green]Commerce Bureau Office[/green] (Level {p.commerceBureauOffice})\n";

        int max = p.GetMaxExchangeOrders();
        int now = World.instance.OfferRequestCount(p);

        s += $"You have {now} of {max} active orders. You have ${p.cash}.\n\n";
        game.Send(p, s);
        
        ShowOffers(p, game);
        ShowRequests(p, game);
    }

    private static void ShowOffers(Player p, GameUpdateService game)
    {
        var offers = World.instance.GetOffers();
        if (offers.Count == 0)
        {
            game.Send(p, "[gray]No active sell offers.[/gray]");
            return;
        }

        string s = "[yellow]Sell Offers:[/yellow]\n";
        foreach (var offer in offers)
        {
            s += $"{offer.Material.name, 18}: {offer.Amount} available at {offer.PricePerUnit} each (Seller: {offer.Seller.name})\n";
        }
        game.Send(p, s);
    }

    private static void ShowRequests(Player p, GameUpdateService game)
    {
        var requests = World.instance.GetRequests();
        if (requests.Count == 0)
        {
            game.Send(p, "[gray]No active buy requests.[/gray]");
            return;
        }

        string s = "[yellow]Buy Requests:[/yellow]\n";
        foreach (var request in requests)
        {
            s += $"{request.Material.name, 18}: {request.Amount} wanted at {request.PricePerUnit} each (Buyer: {request.Buyer.name})\n";
        }
        game.Send(p, s);
    }

    [GameCommand("View all offers and requests.")]
    public static void List(Player p, GameUpdateService game, string args)
    {
        ShowOffers(p, game);
        ShowRequests(p, game);
    }

    [GameCommand("[salmon]sell 10 cobalt 5[/salmon] - Posts an offer to sell 10 Cobalt Ore for 5 each. (enter the first couple letters of the mat)")]
    public static void Sell(Player p, GameUpdateService game, string args)
    {
        var (amount, material, price) = ParseTradeArgs(p, game, ref args);
        if (material == null) return;

        if (World.instance.PostOffer(p, material, amount, price))
        {
            game.Send(p, $"[green]You posted an offer: {amount} {material.name} for {price} each.[/green]");
        }
    }

    [GameCommand("[salmon]buy 10 cobalt 5[/salmon] - Attempts to buy 10 Cobalt Ore at 5 each. (enter the first couple letters of the mat)")]
    public static void Buy(Player p, GameUpdateService game, string args)
    {
        var (amount, material, price) = ParseTradeArgs(p, game, ref args);
        if (material == null) return;

        if(price <= 0){
            game.Send(p, "[red]Invalid price.[/red]");
            Help(p, game, "");
            return;
        }
        if(price <= 0){
            game.Send(p, "[red]Invalid price.[/red]");
            Help(p, game, "");
            return;
        }

        List<Offer> matchingOffers = World.instance.GetOffers()
            .Where(o => o.Material.uuid == material.uuid && o.PricePerUnit <= price)
            .OrderBy(o => o.PricePerUnit)
            .ToList();

        int remainingAmount = amount;
        int totalSpent = 0;

        foreach (var offer in matchingOffers)
        {
            int tradeAmount = Math.Min(offer.Amount, remainingAmount);
            int cost = tradeAmount * offer.PricePerUnit;

            if (p.cash < cost)
            {
                game.Send(p, "[red]Not enough cash to complete this trade.[/red]");
                return;
            }

            p.cash -= cost;
            offer.Seller.cash += cost;
            p.AddItem(material, tradeAmount);

            offer.Amount -= tradeAmount;
            remainingAmount -= tradeAmount;
            totalSpent += cost;

            if (remainingAmount == 0) break;
        }

        World.instance.ProcessTrades();

        if (remainingAmount > 0)
        {
            if (World.instance.PostRequest(p, material, remainingAmount, price))
            {
                game.Send(p, $"[green]Bought {amount - remainingAmount} {material.name} for {totalSpent} total. Remaining {remainingAmount} placed as a buy request at {price} each.[/green]");
            }
        }
        else
        {
            game.Send(p, $"[green]Successfully bought {amount} {material.name} for {totalSpent} total.[/green]");
        }
    }

    private static (int, Material?, int) ParseTradeArgs(Player p, GameUpdateService game, ref string args)
    {
        int amount = PullIntArg(p, ref args);
        string materialName = PullArg(ref args);
        int price = PullIntArg(p, ref args);

        Material? material = World.instance.GetMaterialByName(materialName);
        if (material == null)
        {
            game.Send(p, $"[red]Material '{materialName}' not found.[/red]");
            return (0, null, 0);
        }

        return (amount, material, price);
    }
}
