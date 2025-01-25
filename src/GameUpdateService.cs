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
