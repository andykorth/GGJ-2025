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
        string output = Ascii.Header("How to Research", 60, "cyan");
        output += "You may research at your headquarters by sending one of your relics to a research partner.\nOnly the Research Leader needs to spend their relic. You can only have each player as a research partner once.\n";
        output += "Both the research leader and partner gain the benefit of the research.\n";
        output += $"When a new Material is developed, the leader designs and names it. Both the leader and partner get a {World.RESEARCH_BOOST} bonus to production speed of that material.\n";
        output +=  Ascii.Header("Office Upgrades", 60, "yellow");
        output += $"Your headquarters contains various offices that allow you to expand your empire.\n";
        output += $"The [green]Planetary Administration Office[/green] allows you to increase the number of buildings you can construct. The [green]Empire Logistics Office[/green] increases your max storage.\n";
        output += $"The [green]Commerce Bureau[/green] increases the number of orders you can have on the exchange, and the [green]Fleet Command Center[/green] increases the number of ships you can control.\n";
        output += $"And the [green]Research Directorate Office[/green] increases the maximum number of research projects you can lead.\n";
        p.Send(output);
        Context.Help(p, game, args);
    }

    [GameCommand("List all Headquarters Statistics.")]
    public static void List(Player p, GameUpdateService game, string args)
    {
        string output = Ascii.Header("Headquarters", 60, "yellow");
        output += $"   0) {"Planetary Administration Office:",-34} Level {p.adminOffice}\n";
        output += $"   1) {"Empire Logistics Office:",-34} Level {p.logisticsOffice}\n";
        output += $"   2) {"Fleet Command Center:",-34} Level {p.fleetCommandOffice}\n";
        output += $"   3) {"Research Directorate Office:",-34} Level {p.researchOffice}\n";
        output += $"   4) {"Commerce Bureau:",-34} Level {p.commerceBureauOffice}\n\n";
        // output += $"{"Current Research Points:",-34} {p.researchPoints}\n";
        output += ShowRelics(10, p, 0);
        output += ShowResearchBoosts(10, p, 0);
        output += "Additional HQ instructions are available with [salmon]help[/salmon]";
        game.Send(p, output);
    }

    public static string[] OFFICE_NAMES = [
        "Planetary Administration Office",
        "Empire Logistics Office",
        "Fleet Command Center",
        "Research Directorate Office",
        "Commerce Bureau",
    ];

    [GameCommand("upgrade 0 - Upgrade the specified headquarters office.")]
    public static void Upgrade(Player p, GameUpdateService game, string args)
    {
        var names = new List<string>(OFFICE_NAMES);
        string? name = PullIndexArg<string>(p, game, ref args, names);
        if (name == null) return;

        int index = names.IndexOf(name);
        name = $"[green]{name}[/green]";
        int currentLevel = 0;
        if (index == 0) currentLevel = p.adminOffice;
        if (index == 1) currentLevel = p.logisticsOffice;
        if (index == 2) currentLevel = p.fleetCommandOffice;
        if (index == 3) currentLevel = p.researchOffice;
        if (index == 4) currentLevel = p.commerceBureauOffice;

        if (currentLevel > 6)
        {
            p.Send($"You have maxed out your [green]{name}[/green]! Nice job!");
            return;
        }

        int padRightAmt = 30;

        int cashCost = 100 * currentLevel * Math.Max(1, currentLevel - 1);
        string s = $"Upgrading your {name} from level {currentLevel} to {currentLevel + 1} will cost:\n";
        s += $"   Cash {cashCost}".PadRight(padRightAmt) + $"(you have {p.cash})\n";

        Material? upgradeMat1 = currentLevel switch
        {
            1 => World.metalFurniture,
            2 => World.metalFurniture,
            3 => World.workwear,
            4 => World.workwear,
            5 => World.luxuryFurniture,
            6 => World.luxuryFurniture,
            _ => null
        };

        Material? upgradeMat2 = currentLevel switch
        {
            1 => null,
            2 => World.cobalt,
            3 => World.polyethelene,
            4 => World.veggies,
            5 => World.prepackagedMeals,
            6 => World.deluxeMeals,
            _ => null
        };

        Material? upgradeMat3 = currentLevel switch
        {
            1 => null,
            2 => null,
            3 => World.regolith,
            4 => World.vacuumLichen,
            5 => World.platinum,
            6 => World.hullPlates,
            _ => null
        };

        int ListMat( ref string s, Material? mat)
        {
            if (mat != null)
            {
                float a = mat.baseCost > 0 ? mat.baseCost : 5f / mat.rarity;
                int amt = (int)((100 * currentLevel) / a);
                s += $"   {mat.name} x{amt}".PadRight(padRightAmt) + $"(you have {p.GetMaterialQuantity(mat)})\n";
                return amt;
            }
            return 0;
        }

        int upgradeAmt1 = ListMat(ref s, upgradeMat1);
        int upgradeAmt2 = ListMat(ref s, upgradeMat2);
        int upgradeAmt3 = ListMat(ref s, upgradeMat3);

        s += " \n";

        //test for cash:
        if (p.cash < cashCost)
        {
            s += "[red]You don't have enough cash![/red]\n";
            p.Send(s);
            return;
        }

        // test for these numbers:
        if (upgradeMat1 != null && p.GetMaterialQuantity(upgradeMat1) < upgradeAmt1)
        {
            s += $"[red]You don't have enough {upgradeMat1.name}![/red]\n";
            p.Send(s);
            return;
        }
        // test for these numbers:
        if (upgradeMat2 != null && p.GetMaterialQuantity(upgradeMat2) < upgradeAmt2)
        {
            s += $"[red]You don't have enough {upgradeMat2.name}![/red]\n";
            p.Send(s);
            return;
        }
        // test for these numbers:
        if (upgradeMat3 != null && p.GetMaterialQuantity(upgradeMat3) < upgradeAmt3)
        {
            s += $"[red]You don't have enough {upgradeMat3.name}![/red]\n";
            p.Send(s);
            return;
        }
        p.Send(s);

        p.SetCaptiveYNPrompt($"You have the required materials. Do you want to proceed with construction?", (bool response) =>
        {
            if (response)
            {
                // they have enough, subtract!
                if (upgradeMat1 != null) p.RemoveMaterial(upgradeMat1, upgradeAmt1);
                if (upgradeMat2 != null) p.RemoveMaterial(upgradeMat2, upgradeAmt1);
                if (upgradeMat3 != null) p.RemoveMaterial(upgradeMat3, upgradeAmt1);
                p.cash -= cashCost;

                switch (index)
                {
                    case 0: p.adminOffice += 1; break;
                    case 1: p.logisticsOffice += 1; break;
                    case 2: p.fleetCommandOffice += 1; break;
                    case 3: p.researchOffice += 1; break;
                    case 4: p.commerceBureauOffice += 1; break;
                }
                ;
                p.Send($"{name} upgraded to {currentLevel + 1}!\n");

                string toAll = $"{p.name} upgrades {name} to Level {currentLevel + 1}!\n \n" + GetUpgradeMessage(p, index, currentLevel);
                game.SendAll(Ascii.Box(toAll, "white"));
            }
            else
                game.Send(p, "You can reconsider later!");
        });


    }

    private static string GetUpgradeMessage(Player p, int index, int currentLevel)
    {
        string upgradeMessage = "";

        switch (index)
        {
            case 0: // Planetary Administration Office
                upgradeMessage = currentLevel switch
                {
                    1 => $"{p.name} has established a modest planetary administration office, laying the foundation for governance.",
                    2 => $"{p.name}'s administration expands, increasing bureaucratic efficiency and enabling further development.",
                    3 => $"City planners in {p.name}'s administration office are drafting ambitious expansion plans.",
                    4 => $"{p.name} integrates real-time data processing, accelerating colony growth strategies.",
                    5 => $"With automated systems in place, {p.name} ensures seamless planetary oversight.",
                    6 => $"{p.name}'s administration is now a galactic powerhouse, coordinating development across multiple worlds.",
                    _ => ""
                };
                break;

            case 1: // Empire Logistics Office
                upgradeMessage = currentLevel switch
                {
                    1 => $"{p.name} establishes a logistics office, improving resource management and supply chain efficiency.",
                    2 => $"Trade routes and supply lines under {p.name}'s control are now operating at peak efficiency.",
                    3 => $"{p.name} implements automated inventory systems, optimizing resource distribution.",
                    4 => $"{p.name} constructs interplanetary transport hubs, expanding logistical capabilities.",
                    5 => $"Advanced AI logistics enable {p.name} to predict and fine-tune resource distribution.",
                    6 => $"{p.name} now controls an empire-wide logistical network rivaling ancient trade dynasties.",
                    _ => ""
                };
                break;

            case 2: // Fleet Command Center
                upgradeMessage = currentLevel switch
                {
                    1 => $"{p.name} establishes a fleet command center, organizing their growing naval forces.",
                    2 => $"Tactical simulations at {p.name}'s command center refine fleet maneuvers and combat strategies.",
                    3 => $"{p.name} expands their shipyards, bolstering fleet production and operational capacity.",
                    4 => $"Veteran admirals trained under {p.name} now lead formidable battle fleets.",
                    5 => $"With strategic AI assistance, {p.name} can coordinate fleet movements with unparalleled precision.",
                    6 => $"{p.name} now commands one of the most powerful naval forces in the galaxy.",
                    _ => ""
                };
                break;

            case 3: // Research Directorate Office
                upgradeMessage = currentLevel switch
                {
                    1 => $"{p.name} gathers a team of researchers, embarking on the pursuit of scientific discovery.",
                    2 => $"New laboratories established by {p.name} accelerate technological progress.",
                    3 => $"Top scientific minds are drawn to {p.name}'s research office, pushing the boundaries of knowledge.",
                    4 => $"Breakthroughs emerge as interdisciplinary teams under {p.name} collaborate on cutting-edge research.",
                    5 => $"AI-driven research accelerates innovation, ensuring {p.name} stays at the forefront of science.",
                    6 => $"{p.name}'s research directorate stands as the leading center of technological advancement in the galaxy.",
                    _ => ""
                };
                break;

            case 4: // Commerce Bureau
                upgradeMessage = currentLevel switch
                {
                    1 => $"{p.name} establishes a commerce bureau, expanding their influence in galactic trade.",
                    2 => $"Financial systems under {p.name} are optimized, making trade more efficient and profitable.",
                    3 => $"Automated trading platforms boost market transactions, increasing {p.name}'s economic power.",
                    4 => $"Galactic investors recognize {p.name}'s economic prowess, bringing in waves of capital.",
                    5 => $"Dynamic economic models predict market trends, ensuring {p.name} dominates interstellar trade.",
                    6 => $"{p.name} now controls a vast economic empire, shaping markets across entire star systems.",
                    _ => ""
                };
                break;
        }
        return upgradeMessage;
    }


    [GameCommand("research 0 - Propose a research agreement using a relic.")]
    public static void Research(Player p, GameUpdateService game, string args)
    {
        Relic? selectedRelic = PullIndexArg<Relic>(p, game, ref args, p.GetRelics());
        if (selectedRelic == null)
        {
            p.Send("[red]Invalid relic selection.[/red]");
            return;
        }else{
            p.Send($"You selected the relic [cyan]{selectedRelic.GetName()}[/cyan] for research.");
        }
        
        string message = $"Who do you want to propose a research agreement with? (or [salmon]cancel[/salmon] or [salmon]who[/salmon])";

        p.SetCaptivePrompt(message, InvokeCommand.GetContext<HQContext>(),
            (string response) =>
        {
            string r = response.ToLower();
            Player? recipient = World.instance.GetPlayerByName(r);

            if (r == "cancel" || r == "who" || recipient != null)
            {
                if (recipient != null) {
                    SendResearchInvite(p, game, recipient, selectedRelic);
                    // we return true to use the exit message to pop to our new context.
                    return true;
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
        p.SetCaptivePrompt("Include a one line message with your research invite:", InvokeCommand.GetContext<HQContext>(),
            (string response) =>
            {
                Message m = new Message(p, global::Message.MessageType.ResearchInvitation, response);
                m.invitationSiteUUID = relic.id + "";
                recipient.messages.Add(m);

                game.Send(recipient, $"Hey {recipient.name}! You have a new research invitation from {p.name}!");
                game.Send(p, $"Research request sent to {recipient.name} from {p.name}.");
                return true;
            } );
    }

}
