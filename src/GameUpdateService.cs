using Microsoft.Extensions.Hosting;

public class GameUpdateService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Your update logic here
            World.Update();
            
            await Task.Delay(1000, stoppingToken); // Wait for 1 second
        }
    }
}
