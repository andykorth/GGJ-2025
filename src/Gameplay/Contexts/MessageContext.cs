
public class MessageContext : Context {

    public override string Name => "Message";
    
    public override void EnterContext(Player p, GameUpdateService game)
    {
        List(p, game, "");
    }


    [GameCommand("List all messages")]
    public static void List(Player p, GameUpdateService game, string args)
    {
        // Sort messages: most recent sent date first, unread messages first
        List<Message> sortedMessages = SortedMessages(p);

        // string s = Ascii.Header("Messages", 40, "yellow");
        string s = "";
        s += Context.ShowList(sortedMessages.Cast<IShortLine>().ToList(), "Messages", "message", 20, p, 0);
        game.Send(p, s);
    }

    private static List<Message> SortedMessages(Player p)
    {
        return p.messages
            .OrderByDescending(m => m.sent)
            .ThenBy(m => m.read.HasValue).ToList();
    }

    [GameCommand("view 0: View the contents of the message")]
    public static void View(Player p, GameUpdateService game, string args)
    {
        List<Message> sortedMessages = SortedMessages(p);
        var message = PullIndexArg(p, game, ref args, sortedMessages);
        if(message != null){
            message.read = DateTime.Now;

            var from = World.instance.GetPlayer(message.fromPlayerUUID)!;
            // Display the message details
            string s =  "";
            DateTime now = DateTime.Now;
            s += $"[{Ascii.TimeAgo(now - message.sent)}] - {message.type} from {from.name}\n\n`{message.contents}`\n";
            if(message.type == global::Message.MessageType.Invitation) {
                AcceptInvitation(p, game, message, from, s);
            }else 
            if(message.type == global::Message.MessageType.ResearchInvitation) {
                AcceptResearchInvitation(p, game, message, from, s);
            }else{
                p.Send(s);
            }

        }
    }

    private static void AcceptInvitation(Player p, GameUpdateService game, Message message, Player from, string s)
    {
        var site = World.instance.GetSite(message.invitationSiteUUID!);
        s += $"[cyan]The message contains a Site Invitation to join {from.name} on {site!.name}:[/cyan]\n";
        s += ((IShortLine)site).ShortLine(p, -1);
        // s += site.LongLine();
        if (p.GetExploredSites().Contains(site))
        {
            s += $"\n[red]However, you are already on that planet![/red]\n";
            game.Send(p, Ascii.Box(s));
        }
        else
        {
            game.Send(p, Ascii.Box(s));
            p.SetCaptiveYNPrompt($"Do you want to join {from.name} on {site!.name}? (y/n)", (bool response) =>
            {
                if (response)
                {
                    p.exploredSiteUUIDs.Add(site.uuid);
                    game.Send(p, $"{site!.name} added to your site list!");
                }
                else
                    game.Send(p, "You can reconsider later!");
            });
        }
    }

    private static void AcceptResearchInvitation(Player p, GameUpdateService game, Message message, Player from, string s)
    {
        var relicID = message.invitationSiteUUID;
        var relicName = Ascii.relicNames[int.Parse(relicID)];
        s += $"This message contains a Research Invitation from {from.name} to study their [cyan]{relicName}[/cyan].\n \n";
        s += $"You can join this project at no cost and will recieve access to the boost unlocked when this research is complete.\n";
        s += $"You only join one research project with {from.name} as the leader, but you can later lead a research project you propose to {from}.\n";

        game.Send(p, Ascii.Box(s));

        p.SetCaptiveYNPrompt($"Do you want to join {from.name} in their research project? (y/n)", (bool response) =>
        {
            if (response)
            {
                ResearchProject sharedProject = new ResearchProject();
                sharedProject.leaderPlayer = from;
                sharedProject.partnerPlayer = p;
                sharedProject.researchMaterialUUID = null;
                sharedProject.researchStartTime = DateTime.Now;
                sharedProject.researchCost = 0;
                sharedProject.relicName = relicName;

                string s2 = $"A new research project lead by {from.name} has begun!\nThey are assisted by {p.name}. Together they are\nstudying a [cyan]{relicName}[/cyan]!\n \n";

                string[] researchEndings = {
                    "Scientists throughout the galaxy expect wonderous advancements to result!",
                    "The halls of academia buzz with anticipation—what secrets will this relic unveil?",
                    "Hope and curiosity fuel their work, as the galaxy watches with bated breath.",
                    "This collaboration may reshape the future of science as we know it!",
                    "Historians whisper that this could be the greatest discovery of our time."
                };

                s2 += researchEndings[new Random().Next(researchEndings.Length)];
                game.SendAll(Ascii.Box(s2, "hotpink"));
            }
            else
                game.Send(p, "You can reconsider later!");
        });
    }

    [GameCommand("send: Send a text message to someone.")]
    public static void Send(Player p, GameUpdateService game, string args)
    {
        p.SetCaptiveSelectPlayerPrompt( "Who do you want to message? (or [salmon]cancel[/salmon] or [salmon]who[/salmon])", (Player found) => {
            MessageSendReally(p, game, found);
        });
    }

    private static void MessageSendReally(Player p, GameUpdateService game, Player recipient)
    {
        if(p == null){ Log.Error("Missing player sending message."); return; }
        if(recipient == null){ Log.Error("Missing message recipient."); return; }

        game.Send(p, $"You are messaging {recipient.name}.");
        p.SetCaptivePrompt("Enter your message text now: ", InvokeCommand.GetContext<MessageContext>(),
            (string response) => {
                Message m = new Message(p, global::Message.MessageType.TextMail, response);
                recipient.messages.Add(m);

                game.Send(recipient, $"You have a new message from {p.name}!" );
                game.Send(p, $"Message sent to {recipient.name}.");
                return true;
            });

    }
}
