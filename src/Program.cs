using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// This is what runs the update loop for the game
// and eventually calls World.Update
builder.Services.AddHostedService<GameUpdateService>();

var app = builder.Build();
app.Urls.Add("http://*:3000");

// Hook into application shutdown event
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Server is shutting down...");

    // Notify connected clients via SignalR
    var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();
    hubContext.Clients.All.SendAsync("ReceiveShutdownMessage", "Server is shutting down...").Wait();
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

// gameplay launching stuff.
World.CreateOrLoad();
// Kick this off to start the static initializer
InvokeCommand.Load();

// Configure middleware
app.MapHub<GameHub>("/gameHub"); // The client connects to "/gamehub"
app.Run();

