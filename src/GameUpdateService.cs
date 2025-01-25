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
       _hubContext.Clients.Client(p.connectionID).SendAsync("ReceiveLine", line);
    }

    public void SendImage(string url){
        _hubContext.Clients.All.SendAsync("ReceiveImage", url);
    }

    public void SendSound(string url){
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


    internal void SetCaptivePrompt(Player p, string message, Func<string, bool> promptFunc)
    {
        Send(p, message);
        p.captivePrompt = promptFunc;
        p.captivePromptMsg = message;
    }
    internal void SetCaptiveYNPrompt(Player p, string message, Action<bool> responseFunc)
    {
        Send(p, message);
        p.captivePrompt = (string r) => {
            if(r == "y" || r == "n"){
                responseFunc(r == "y");
                return true;
            }else{
                return false;
            }
        };
        p.captivePromptMsg = message;
    }

    public void InvokeCaptivePrompt(Player p, string playerInput)
    {
        bool b = p.captivePrompt!(playerInput);
        if(b){
            // done with captive prompt!
            p.captivePrompt = null;
            p.captivePromptMsg = null;
        }else{
            // repeat the prompt, they did it wrong.
            Send(p, p.captivePromptMsg!);
        }
    }
}
