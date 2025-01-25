using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    // it is slightly naughty to have a static list here, but Hub instances are transient. This will persist
    // fine until you put your game in a giant distributed datacenter. But then the whole thing won't work anyway.
    public static List<Player> loggedInPlayers = new List<Player>();

    // Called when a player sends a message
    public void SendMessage(string playerName, string message)
    {
        if(string.IsNullOrEmpty(playerName)){
            Send("First, enter your name above.");
            return;
        }

        Player p = RetrievePlayer(playerName, GetContext());

        // check if the player has a captive prompt. If so, run that.
        if(p.captivePrompt != null){
            Log.Info($"[{playerName}] (captiveprompt): [{message}]");
            InvokeCaptivePrompt(p, message);
        }else{
            // run the general command entry.
            var m = message.Split(" ", 2, StringSplitOptions.TrimEntries);
            InvokeCommand.Invoke(p, this, m[0], m.Length > 1 ? m[1] : "");

            Log.Info($"[{playerName}]: [{m[0]}] - [{(m.Length > 1 ? m[1] : "")}]");
        }

    }

    public void SendAll(string line){
        Clients.All.SendAsync("ReceiveLine", line);
    }

    public void Send(string line){
        Clients.Caller.SendAsync("ReceiveLine", line);
    }

    public void SendImage(string url){
        Clients.All.SendAsync("ReceiveImage", url);
    }

    public void SendSound(string url){
        Clients.All.SendAsync("PlaySound", url);
    }

    private HubCallerContext GetContext()
    {
        return Context;
    }

    private Player RetrievePlayer(string playerName, HubCallerContext context)
    {
        var matches = World.instance.allPlayers.FindAll(x => x.name == playerName);
        if(matches.Count > 1){
            Log.Error($"Multiple players exist with name {playerName}");
        }
        Player p;
        if(matches.Count == 0){
            Log.Info($"First log in of player: {playerName}");
            p = new Player(playerName);
            // new client, send them the welcome:
            Send($"Welcome {p}. Clients connected: {ConnectedClients.Count}");
            Send($"Type 'help' for help.");
    
            Clients.Others.SendAsync("ReceiveLine", $"Player [{p.name}] has joined.");
            
            World.instance.allPlayers.Add(p);
        }else{
            p = matches[0];
        }
        p.connectionID = Context.ConnectionId;
        p.client = Clients.Caller;

        return p;
    }

    // Thread-safe dictionary to track connected clients
    private static readonly ConcurrentDictionary<string, string> ConnectedClients = new();

    // Called when a client connects
    public override Task OnConnectedAsync()
    {
        ConnectedClients.TryAdd(Context.ConnectionId, Context.User?.Identity?.Name ?? "Anonymous");
        Log.Info($"Client connected: {Context.ConnectionId}. Total clients: {ConnectedClients.Count}");
        
        Send(
@"
   ___       __             ______                      
  / _ | ___ / /________    /_  __/_ _________  ___  ___ 
 / __ |(_--/ __/ __/ _ \    / / / // / __/ _ \/ _ \/ _ \
/_/ |_/___/\__/_/  \___/   /_/  \_, /\__/\___/\___/_//_/
                               /___/                    

Astro Tycoon is a multiplayer space empire simulation game. Cooperate with other
players to expand your empire, buy and sell your production, explore with spaceships, and
conduct research!

To begin, [cyan]enter your username above[/cyan], then send your first command below.
Try '[red]help[/red]' to get started.
                                                                                                                                                       
");

        return base.OnConnectedAsync();
    }

    // Called when a client disconnects
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override Task OnDisconnectedAsync(Exception exception)
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    {
        ConnectedClients.TryRemove(Context.ConnectionId, out _);
        Log.Info($"Client disconnected: {Context.ConnectionId}. Total clients: {ConnectedClients.Count}");
        return base.OnDisconnectedAsync(exception);
    }

    internal void SetCaptivePrompt(Player p, string message, Func<string, bool> value)
    {
        Send(message);
        p.captivePrompt = value;
        p.captivePromptMsg = message;
    }

    private void InvokeCaptivePrompt(Player p, string playerInput)
    {
        bool b = p.captivePrompt!(playerInput);
        if(b){
            // done with captive prompt!
            p.captivePrompt = null;
            p.captivePromptMsg = null;
        }else{
            // repeat the prompt, they did it wrong.
            Send(p.captivePromptMsg!);
        }
    }

}

