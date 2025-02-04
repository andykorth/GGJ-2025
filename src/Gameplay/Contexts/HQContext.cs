public class HQContext : Context
{
    public override string Name => "HQ";

    public override void EnterContext(Player p, GameUpdateService game)
    {
        p.Send("[yellow]Headquarters:[/yellow]");
        // show the default stats:
        List(p, game, "");
    }


    [GameCommand("List all Headquarters Statistics.")]
    public void List(Player p, GameUpdateService game, string args)
    {
        string output = "";

        output += Ascii.Header("Empire Status", 40, "yellow");
        output += ShowRelics(10, p, 0);
        game.Send(p, output);
    }


    [GameCommand("research 0 - Propose a research agreement using a relic.")]
    public void ResearchAgreement(Player p, GameUpdateService game, string args)
    {
        Relic? selectedRelic = PullIndexArg<Relic>(p, game, ref args, p.GetRelics());
        if (selectedRelic == null)
        {
            p.Send("[red]Invalid relic selection.[/red]");
            return;
        }

        string message = $"You selected the relic: [cyan]{selectedRelic.GetName()}[/cyan].\nWho do you want to propose a research agreement with?";

        p.SetCaptivePrompt(message, (string response) =>
        {
            string r = response.ToLower();
            Player? recipient = World.instance.GetPlayerByName(r);

            if (r == "cancel" || r == "who" || recipient != null)
            {
                if (recipient != null)
                {
                    game.Send(p, $"Sending research agreement request to {recipient.name}...");
                    SendResearchInvite(p, game, recipient, selectedRelic);
                    return false;
                }

                if (r == "who")
                {
                    MainContext.Who(p, game, args);
                    return false;
                }

                if (r == "cancel")
                {
                    game.Send(p, "Research agreement canceled.");
                }

                return true;
            }
            return false;
        });
    }

    private static void SendResearchInvite(Player p, GameUpdateService game, Player recipient, Relic relic)
    {
        if (p == null || recipient == null || relic == null)
        {
            Log.Error("Invalid research invitation parameters.");
            return;
        }

        game.Send(p, $"You are inviting {recipient.name} to study the relic {relic.GetName()}.");
        p.SetCaptivePrompt("If you want to include a message, type it now.",
            (string response) =>
            {
                Message m = new Message(p, global::Message.MessageType.ResearchInvitation, response);
                m.invitationSiteUUID = relic.id + "";
                recipient.messages.Add(m);

                game.Send(recipient, $"You have a new research invitation from {p.name}!");
                game.Send(p, $"Research request sent to {recipient.name}.");
                return true;
            });
    }

}
