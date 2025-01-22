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
        if(playerName == "System") playerName = "NaughtyPlayer";

        Player p = RetrievePlayer(playerName);
        InvokeCommand.Invoke(p, message);

        // Broadcast the message to all connected clients
        await Clients.All.SendAsync("ReceiveLine", playerName, message);
    }

    public void SendAll(string line){
        Clients.All.SendAsync("ReceiveLine", line);
    }

    private Player RetrievePlayer(string playerName)
    {
        Player? found = loggedInPlayers.FirstOrDefault(x => x.name == playerName);

        if(found == null){
            found = new Player();
            found.name = playerName;
            // new client, send them the welcome:
            Clients.Caller.SendAsync("ReceiveLine", $"Welcome {playerName}. Clients connected: {ConnectedClients.Count}");
            Clients.Caller.SendAsync("ReceiveLine", $"Type 'help' for help.");
        }

        foreach (Player player in loggedInPlayers){
            Clients.All.SendAsync("ReceiveLine", "System", $"Player [{found.name}] has joined.");
        }
        loggedInPlayers.Add(found);

        return found;
    }

    // Thread-safe dictionary to track connected clients
    private static readonly ConcurrentDictionary<string, string> ConnectedClients = new();

    // Called when a client connects
    public override Task OnConnectedAsync()
    {
        ConnectedClients.TryAdd(Context.ConnectionId, Context.User?.Identity?.Name ?? "Anonymous");
        Console.WriteLine($"Client connected: {Context.ConnectionId}. Total clients: {ConnectedClients.Count}");
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