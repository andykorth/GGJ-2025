using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    // Called when a player sends a message
    public async Task SendMessage(string playerName, string message)
    {
        // Broadcast the message to all connected clients
        await Clients.All.SendAsync("ReceiveMessage", playerName, message);
    }
}