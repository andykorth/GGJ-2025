using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    // it is slightly naughty to have a static list here, but Hub instances are transient. This will persist
    // fine until you put your game in a giant distributed datacenter. But then the whole thing won't work anyway.
    public static List<Player> loggedInPlayers = new List<Player>();

    // Called when a player sends a message
    public async Task SendMessage(string playerName, string message)
    {
        if(string.IsNullOrEmpty(playerName)){
            Send("First, enter your name above.");
            return;
        }

        Player p = RetrievePlayer(playerName);
        var m = message.Split(" ", 2, StringSplitOptions.TrimEntries);
        InvokeCommand.Invoke(p, this, m[0], m.Length > 1 ? m[1] : "");

        Log.Info($"[{playerName}]: [{m[0]}] - [{(m.Length > 1 ? m[1] : "")}]");

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

    private Player RetrievePlayer(string playerName)
    {
        Player? found = loggedInPlayers.FirstOrDefault(x => x.name == playerName);

        if(found == null){
            found = new Player();
            found.name = playerName;
            // new client, send them the welcome:
            Send($"Welcome {playerName}. Clients connected: {ConnectedClients.Count}");
            Send($"Type 'help' for help.");
    
            Clients.Others.SendAsync("ReceiveLine", $"Player [{found.name}] has joined.");
            
            loggedInPlayers.Add(found);
        }

        return found;
    }

    // Thread-safe dictionary to track connected clients
    private static readonly ConcurrentDictionary<string, string> ConnectedClients = new();

    // Called when a client connects
    public override Task OnConnectedAsync()
    {
        ConnectedClients.TryAdd(Context.ConnectionId, Context.User?.Identity?.Name ?? "Anonymous");
        Console.WriteLine($"Client connected: {Context.ConnectionId}. Total clients: {ConnectedClients.Count}");
        
        Send(
@"
          Welcome!
   ____  ____     _   ____   ___ ____  ____  
  / ___|/ ___|   | | |___ \ / _ \___ \| ___| 
 | |  _| |  _ _  | |   __) | | | |__) |___ \ 
 | |_| | |_| | |_| |  / __/| |_| / __/ ___) |
  \____|\____|\___/  |_____|\___/_____|____/ 

GGJ 2025

Enter your username above, then send your first command below. Try 'help' to get started.
                                                                                                                                                       
");

        return base.OnConnectedAsync();
    }

    // Called when a client disconnects
    public override Task OnDisconnectedAsync(Exception exception)
    {
        ConnectedClients.TryRemove(Context.ConnectionId, out _);
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}. Total clients: {ConnectedClients.Count}");
        return base.OnDisconnectedAsync(exception);
    }
}

public class Player {
    public string name;
}