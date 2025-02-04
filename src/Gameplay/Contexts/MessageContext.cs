
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
        var sortedMessages = p.messages
            .OrderByDescending(m => m.sent)
            .ThenBy(m => m.read.HasValue);

        string s = Ascii.Header("Messages", 40, "yellow");
        s += Context.ShowList(sortedMessages.Cast<IShortLine>().ToList(), "Messages", "message", 20, p, 0);
        game.Send(p, s);
    }

    [GameCommand("view 0: View the contents of the message")]
    public static void View(Player p, GameUpdateService game, string args)
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
                s += ((IShortLine)site).ShortLine(p, -1);
                // s += site.LongLine();
                if(p.GetExploredSites().Contains(site)){
                    s += $"\n[red]But you are already on that planet![/red]\n";
                    game.Send(p, s);
                }else{
                    game.Send(p, s);
                    p.SetCaptiveYNPrompt( $"Do you want to join {from} on {site!.name}? (y/n)", (bool response) => {
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

    [GameCommand("send: Send a text message to someone.")]
    public static void Send(Player p, GameUpdateService game, string args)
    {
        p.SetCaptivePrompt( "Who do you want to message? (or [salmon]cancel[/salmon] or [salmon]who[/salmon])",
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
                        MainContext.Who(p, game, args);
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
        if(p == null){ Log.Error("Missing player sending message."); return; }
        if(recipient == null){ Log.Error("Missing message recipient."); return; }

        game.Send(p, $"You are messaging {recipient.name}.");
        p.SetCaptivePrompt("Enter your message text now.",
            (string response) => {
                Message m = new Message(p, global::Message.MessageType.TextMail, response);
                recipient.messages.Add(m);

                game.Send(recipient, $"You have a new message from {p.name}!" );
                game.Send(p, $"Message sent to {recipient.name}.");
                return true;
            });

    }
}
