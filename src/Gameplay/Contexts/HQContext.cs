public class HQContext : Context
{
    public override string Name => "HQ";

    public override void EnterContext(Player p, GameUpdateService game)
    {
         // show the default stats:
        List(p, game, "");
    }

    [GameCommand("See this help message for your current context.", true)]
    public static new void Help(Player p, GameUpdateService game, string args)
    {
        string output = Ascii.Header("How to Research", 50, "cyan");
        output += "You may research at your headquarters by sending one of your relics to a research partner.\nOnly the Research Leader needs to spend their relic. You can only have each player as a research partner once.\n";
        output += "Both the research leader and partner gain the benefit of the research.\n";
        output += $"When a new Material is developed, the leader designs and names it. Both the leader and partner get a {World.RESEARCH_BOOST} bonus to production speed of that material.\n";
        output +=  Ascii.Header("Office Upgrades", 50, "yellow");
        output += $"Your headquarters contains various offices that allow you to expand your empire.\n";
        output += $"The Planetary Administration office allows you to increase the number of buildings you can construct. The Logistics office increases your max storage.\n";
        output += $"The commerce bureau increases the number of orders you can have on the exchange, and the fleet command center increases the number of ships you can control.\n";
        output += $"And the Research Directorate Office increases the maximum number of research projects you can lead.\n";
        p.Send(output);
        Context.Help(p, game, args);
    }

    [GameCommand("List all Headquarters Statistics.")]
    public static void List(Player p, GameUpdateService game, string args)
    {
        string output = Ascii.Header("Headquarters", 50, "yellow");
        output += $"{"Planetary Administration Office:",-34} Level {p.adminOffice}\n";
        output += $"{"Empire Logistics Office:",-34} Level {p.logisticsOffice}\n";
        output += $"{"Fleet Command Center:",-34} Level {p.fleetCommandOffice}\n";
        output += $"{"Research Directorate Office:",-34} Level {p.researchOffice}\n";
        output += $"{"Commerce Bureau:",-34} Level {p.commerceBureauOffice}\n\n";
        // output += $"{"Current Research Points:",-34} {p.researchPoints}\n";
        output += ShowRelics(10, p, 0);
        output += ShowResearchBoosts(10, p, 0);
        output += "Additional HQ instructions are available with [salmon]help[/salmon]";
        game.Send(p, output);
    }

    [GameCommand("research 0 - Propose a research agreement using a relic.")]
    public static void Research(Player p, GameUpdateService game, string args)
    {
        Relic? selectedRelic = PullIndexArg<Relic>(p, game, ref args, p.GetRelics());
        if (selectedRelic == null)
        {
            p.Send("[red]Invalid relic selection.[/red]");
            return;
        }
        
        string message = $"You selected the relic: [cyan]{selectedRelic.GetName()}[/cyan].\nWho do you want to propose a research agreement with? (or [salmon]cancel[/salmon] or [salmon]who[/salmon])";

        p.SetCaptivePrompt(message, (string response) =>
        {
            string r = response.ToLower();
            Player? recipient = World.instance.GetPlayerByName(r);

            if (r == "cancel" || r == "who" || recipient != null)
            {
                if (recipient != null) {
                    SendResearchInvite(p, game, recipient, selectedRelic);
                    // we return false because don't exit the current context, since we will be swapping it.
                    return false;
                }

                if (r == "who") {
                    MainContext.Who(p, game, args);
                    return false;
                }

                if (r == "cancel") {
                    game.Send(p, "Research agreement canceled.");
                }

                return true;
            }
            return false;
        });
    }

    private static void SendResearchInvite(Player p, GameUpdateService game, Player recipient, Relic relic)
    {
        if (p == null || recipient == null || relic == null) {
            Log.Error("Invalid research invitation parameters.");
            return;
        }

        game.Send(p, $"You are inviting {recipient.name} to study the relic [cyan]{relic.GetName()}[/cyan].");
        p.SetCaptivePrompt("Include a one line message with your reseazrch invite:",
            (string response) =>
            {
                Message m = new Message(p, global::Message.MessageType.ResearchInvitation, response);
                m.invitationSiteUUID = relic.id + "";
                recipient.messages.Add(m);

                game.Send(recipient, $"You have a new research invitation from {p.name}!");
                game.Send(p, $"Research request sent to {recipient.name}.");
                return true;
            }, true);
    }

}
