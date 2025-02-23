using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

public class GameUpdateService : BackgroundService
{
    private readonly IHubContext<GameHub> _hubContext;
    public GameUpdateService(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
        Log.Info("Game Update Service created with GameHub: " + hubContext);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Your update logic here
            World.Update(this);
            
            await Task.Delay(1000, stoppingToken); // Wait for 1 second
        }
    }

    public void Send(Player p, string line){
        if(p.connectionID == null){
            // they aren't logged in anymore! Thanks Joanna for finding this bug :P
            return;
        }
        var v = _hubContext.Clients.Client(p.connectionID);
        if(v != null) // I don't think this can happen
            v.SendAsync("ReceiveLine", line);
    }

    public void SendCommandList(Player p, string[] commandList, string[] commandHelpList, string contextName){
        if(p.connectionID == null){
            // they aren't logged in anymore! Thanks Joanna for finding this bug :P
            return;
        }
        var v = _hubContext.Clients.Client(p.connectionID);
        if(v != null) // I don't think this can happen
            v.SendAsync("ReceiveCommandListAndHelp", commandList, commandHelpList, contextName);
    }

    public void SendImage(string url){
        _hubContext.Clients.All.SendAsync("ReceiveImage", url);
    }

    public void SendImage(Player p, string url){
        if(p.connectionID == null){
            // they aren't logged in anymore!
            return;
        }
        var v = _hubContext.Clients.Client(p.connectionID);
        if(v != null) // I don't think this can happen
            v.SendAsync("ReceiveImage", url);
    }

    public void SendImage(string connectionID, string url){
        if(connectionID == null){
            // they aren't logged in anymore!
            return;
        }
        var v = _hubContext.Clients.Client(connectionID);
        if(v != null) // I don't think this can happen
            v.SendAsync("ReceiveImage", url);
    }

    public void SendSound( string url){
        _hubContext.Clients.All.SendAsync("PlaySound", url);
    }

    public void SendAll(string line){
        _hubContext.Clients.All.SendAsync("ReceiveLine", line);
    }
    public void SendExcept(string except, string line){
        _hubContext.Clients.AllExcept(except).SendAsync("ReceiveLine", line);
    }
    public void SendTo(string connectionID, string line){
        _hubContext.Clients.Client(connectionID).SendAsync("ReceiveLine", line);
    }


}
