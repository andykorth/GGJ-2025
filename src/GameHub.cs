using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{

    // Called when a player types something.
    public void PlayerSendCommand(string playerName, string message)
    {
        if(string.IsNullOrEmpty(playerName)){
            Send("First, enter your name above.");
            return;
        }

        Player p = RetrievePlayer(playerName, GetContext());
        p.lastActivity = DateTime.Now;
        var service = World.instance.GetService();
        
        // inside it finds their appropriate context and method.
        p.Invoke(service, message);

    }
    public void Send(string line){
        Clients.Caller.SendAsync("ReceiveLine", line);
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

        // verify some key fields on a player each time
        p.connectionID = Context.ConnectionId;
        p.client = Clients.Caller;
        if(p.currentContext == null){
            p.currentContext = InvokeCommand.allContexts[nameof(MainContext)];
        }

        return p;
    }

    // Thread-safe dictionary to track connected clients
    private static readonly ConcurrentDictionary<string, string> ConnectedClients = new();

    // Called when a client connects
    public override Task OnConnectedAsync()
    {
        ConnectedClients.TryAdd(Context.ConnectionId, Context.User?.Identity?.Name ?? "Anonymous");
        Log.Info($"Client connected: {Context.ConnectionId}. Total clients: {ConnectedClients.Count}");
        var service = World.instance.GetService();
        service.SendImage(Context.ConnectionId, "AstroTycoon.png");
        Send(
@"

Astro Tycoon is a multiplayer space empire simulation game. Cooperate with other
players to expand your empire, buy and sell your production, explore with spaceships, and
conduct research!

To begin, [cyan]enter your username above[/cyan], then send your first command below.
Try '[salmon]help[/salmon]' to get started.
                                                                                                                                                       
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
        // find the player and mark them disconnected???
        foreach(var p in World.instance.allPlayers){
            if(p.connectionID == Context.ConnectionId){
                p.connectionID = null;
            }
        }
        
        return base.OnDisconnectedAsync(exception);
    }

}

